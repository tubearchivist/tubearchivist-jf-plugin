using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using J2N.Collections.Generic.Extensions;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist;
using Jellyfin.Plugin.TubeArchivistMetadata.Utilities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Tasks
{
    /// <summary>
    /// Task to sync TubeArchivist playlists to Jellyfin.
    /// </summary>
    public class JFToTubeArchivistPlaylistsSyncTask : IScheduledTask
    {
        private const string TAPlaylistIdRegex = @"^(.*)\((.*)\)$";
        private const string YTTAPlaylistNameFormatRegex = @"^(.*)\s\-\s(.*)\s\((.*)\)$";
        private const string TAPlaylistNameFormatRegex = @"^(.*)\s\((.*)\)$";
        private readonly ILogger<Plugin> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IPlaylistManager _playlistManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="JFToTubeArchivistPlaylistsSyncTask"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="libraryManager">Library manager.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="playlistManager">Playlists manager.</param>
        public JFToTubeArchivistPlaylistsSyncTask(ILogger<Plugin> logger, ILibraryManager libraryManager, IUserManager userManager, IPlaylistManager playlistManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _playlistManager = playlistManager;
        }

        /// <inheritdoc/>
        public string Name => "JFToTubeArchivistPlaylistsSyncTask";

        /// <inheritdoc/>
        public string Description => "This tasks syncs Jellyfin playlists to TubeArchivist";

        /// <inheritdoc/>
        public string Category => "TubeArchivistMetadata";

        /// <inheritdoc/>
        public string Key => "JFToTubeArchivistPlaylistsSyncTask";

        private Dictionary<Guid, List<BaseItem>> GetAllVideos(IEnumerable<MediaBrowser.Controller.Playlists.Playlist> playlists, User user)
        {
            var items = new Dictionary<Guid, List<BaseItem>>();
            foreach (var playlist in playlists)
            {
                items[playlist.Id] = playlist.GetChildren(user, true, new InternalItemsQuery());
            }

            return items;
        }

        private int CountTotalVideos(Dictionary<Guid, List<BaseItem>> playlistsItems)
        {
            var totalVideosCount = 0;
            foreach (var playlistId in playlistsItems.Keys)
            {
                totalVideosCount += playlistsItems[playlistId].Count;
            }

            return totalVideosCount;
        }

        private string? GetTAPlaylistIdFromName(string playlistName)
        {
            var regex = new Regex(TAPlaylistIdRegex);
            return regex.Match(playlistName).Groups[2].ToString();
        }

        private string? GetTAPlaylistNameFromName(string playlistName)
        {
            var ytRegex = new Regex(YTTAPlaylistNameFormatRegex);
            var regex = new Regex(TAPlaylistNameFormatRegex);

            var name = ytRegex.Match(playlistName).Groups[1].ToString();
            if (string.IsNullOrEmpty(name))
            {
                name = regex.Match(playlistName).Groups[1].ToString();
            }

            return name;
        }

        private string GetPlaylistUpdatedName(string playlistName, string newId)
        {
            var index = playlistName.LastIndexOf(" (", StringComparison.CurrentCulture);
            if (index < 0)
            {
                return $"{playlistName} ({newId})";
            }
            else
            {
                var authorAndName = playlistName.Substring(0, index);
                return $"{authorAndName} ({newId})";
            }
        }

        private void MoveElementToPosition(Collection<PlaylistEntry> playlistEntries, int oldPosition, int newPosition)
        {
            var oldItem = playlistEntries[oldPosition];
            playlistEntries.RemoveAt(oldPosition);
            playlistEntries.Insert(newPosition, oldItem);
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(0);
            if (Plugin.Instance!.Configuration.JFTASync)
            {
                var start = DateTime.Now;
                _logger.LogInformation("Starting Jellyfin->TubeArchivist playlists synchronization.");
                var taApi = TubeArchivistApi.GetInstance();
                var taPlaylists = await taApi.GetPlaylists().ConfigureAwait(true);

                var jfUsername = Plugin.Instance!.Configuration.JFUsernameFrom;

                var user = _userManager.GetUserByName(jfUsername);
                if (user == null)
                {
                    _logger.LogInformation("{Message}", $"Jellyfin user with username {jfUsername} not found");
                    return;
                }

                var userPlaylists = _playlistManager.GetPlaylists(user.Id).ToList();
                var jfItemsToAnalyze = GetAllVideos(userPlaylists, user);
                var totalVideosCount = CountTotalVideos(jfItemsToAnalyze);
                var processedVideosCount = 0;

                _logger.LogInformation("Found a total of {PlaylistsCount} playlists to analyze with a total of {VideosCount} videos", userPlaylists.Count, totalVideosCount);
                foreach (var jfPlaylist in userPlaylists)
                {
                    _logger.LogInformation("Analyzing playlist {PlaylistName}...", jfPlaylist.Name);
                    var taPlaylistId = GetTAPlaylistIdFromName(jfPlaylist.Name);
                    if (taPlaylistId == null)
                    {
                        _logger.LogDebug("The playlist {PlaylistName} was not a TA playlist: could not find TA playlist id at the end", jfPlaylist.Name);
                        continue;
                    }

                    // Try to find a TA playlist with matching id
                    // N.B.: only videos with TubeArchivist provider can be synced to TA, if there is a non matching video log a warn
                    var taPlaylist = taPlaylists?.Where(tp => tp.Id == taPlaylistId).FirstOrDefault();

                    if (taPlaylist != null)
                    {
                        // If it exists and is custom add new videos and update videos order
                        if (taPlaylist.Type == PlaylistType.Custom)
                        {
                            var jfItems = jfItemsToAnalyze[jfPlaylist.Id];
                            _logger.LogInformation("Found {PlaylistVideosCount} videos in playlist {PlaylistName}", jfItems.Count, jfPlaylist.Name);

                            var itemsToProcess = new List<PlaylistItemAction>();

                            // Add videos to move/add to the TA playlist
                            for (var i = 0; i < jfItems.Count; i++)
                            {
                                if (!jfItems[i].ProviderIds.ContainsKey(Constants.ProviderName))
                                {
                                    _logger.LogError("Could not sync {JFItem} video from playlist {JFPlaylist} because it doesn't belong to TubeArchivist", jfItems[i].Name, jfPlaylist.Name);
                                    continue;
                                }

                                var position = taPlaylist.Entries.FindIndex(e => e.YoutubeId == jfItems[i].ProviderIds[Constants.ProviderName]);
                                PlaylistItemAction action;
                                if (position < 0)
                                {
                                    _logger.LogDebug("Video {VideoName} from playlist {PlaylistName} not found in TA playlist", jfItems[i].Name, jfPlaylist.Name);
                                    var positionsToMove = i - position;
                                    action = new PlaylistItemAction(i, jfItems[i].ProviderIds[Constants.ProviderName], CustomPlaylistAction.Create, positionsToMove, jfItems[i].Name);
                                }
                                else
                                {
                                    _logger.LogDebug("Found video {VideoName} at position {JFPosition} in playlist {PlaylistName} at position {TAPosition} in TA playlist", jfItems[i].Name, i, jfPlaylist.Name, position);
                                    if (i == 0)
                                    {
                                        action = new PlaylistItemAction(i, jfItems[i].ProviderIds[Constants.ProviderName], CustomPlaylistAction.Top, jfItems[i].Name);
                                    }
                                    else if (i == jfItems.Count)
                                    {
                                        action = new PlaylistItemAction(i, jfItems[i].ProviderIds[Constants.ProviderName], CustomPlaylistAction.Bottom, jfItems[i].Name);
                                    }
                                    else
                                    {
                                        var positionsToMove = i - position;
                                        action = new PlaylistItemAction(i, jfItems[i].ProviderIds[Constants.ProviderName], positionsToMove > 0 ? CustomPlaylistAction.Up : CustomPlaylistAction.Down, Math.Abs(positionsToMove), jfItems[i].Name);
                                    }
                                }

                                itemsToProcess.Add(action);
                            }

                            // Add videos to delete from the TA playlist
                            itemsToProcess.AddRange(taPlaylist.Entries
                            .Where(e => !itemsToProcess
                                .Select(i => i.YoutubeId)
                                .Contains(e.YoutubeId))
                            .Select(e => new PlaylistItemAction(e.YoutubeId, e.Title)));

                            foreach (var item in itemsToProcess)
                            {
                                _logger.LogDebug("Analyzing video {VideoName} from playlist {PlaylistName} at position {Position}", item.Name, jfPlaylist.Name, item.Index);
                                switch (item.Action)
                                {
                                    case CustomPlaylistAction.Create:
                                        var success = await AddVideoToTAPlaylist(taApi, jfPlaylist, taPlaylistId, item).ConfigureAwait(true);
                                        if (!success)
                                        {
                                            continue;
                                        }

                                        break;

                                    case CustomPlaylistAction.Top:
                                    case CustomPlaylistAction.Bottom:
                                        _logger.LogDebug("Moving the video {VideoName} to the {Side} of the playlist {PlaylistName}", item.Name, item.Action, jfPlaylist.Name);
                                        await MoveOrDeleteVideo(taApi, jfPlaylist, taPlaylistId, item).ConfigureAwait(true);
                                        break;

                                    case CustomPlaylistAction.Remove:
                                        _logger.LogDebug("Removing the video {VideoName} in playlist {PlaylistName}", item.Name, jfPlaylist.Name);
                                        await MoveOrDeleteVideo(taApi, jfPlaylist, taPlaylistId, item).ConfigureAwait(true);
                                        break;

                                    default:
                                        _logger.LogDebug("Moving the video {VideoName} {Direction} in playlist {PlaylistName}", item.Name, item.Action, jfPlaylist.Name);
                                        await MoveOrDeleteVideo(taApi, jfPlaylist, taPlaylistId, item).ConfigureAwait(true);
                                        break;
                                }

                                processedVideosCount++;
                                progress.Report(processedVideosCount * 100 / totalVideosCount);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Could not sync modifications on a YouTube downloaded TA playlist: {TAPlaylist}", $"{taPlaylist.Name} - {taPlaylist.Channel} ({taPlaylist.Id})");
                            continue;
                        }
                    }
                    else
                    {
                        // If it doesn't, create a new custom playlist and add videos
                        _logger.LogInformation("Playlist {PlaylistName} was not found on TubeArchivist. Creating a new playlist...", jfPlaylist.Name);

                        var playlistName = GetTAPlaylistNameFromName(jfPlaylist.Name);
                        if (string.IsNullOrEmpty(playlistName))
                        {
                            playlistName = jfPlaylist.Name;
                        }

                        _logger.LogDebug("Creating a new TubeArchivist playlist with name: {PlaylistName}", playlistName);

                        var creationRequest = new CustomPlaylistCreation(playlistName);
                        var createdTAPlaylist = await taApi.CreateCustomPlaylist(creationRequest).ConfigureAwait(true);
                        if (createdTAPlaylist == null)
                        {
                            _logger.LogError("Failed to create the playlist {JFPlaylist}", playlistName);
                            continue;
                        }

                        // Update the JF playlist name with the new TA playlist id
                        var newPlaylistName = GetPlaylistUpdatedName(jfPlaylist.Name, createdTAPlaylist.Id);
                        var updateRequest = new PlaylistUpdateRequest
                        {
                            Id = jfPlaylist.Id,
                            Name = newPlaylistName,
                            UserId = user.Id
                        };
                        await _playlistManager.UpdatePlaylist(updateRequest).ConfigureAwait(true);
                        _logger.LogDebug("Updated the playlist name from {NewPlaylistName} to {NewPlaylistName}", playlistName, newPlaylistName);

                        foreach (var jfItem in jfPlaylist.GetItemList(new InternalItemsQuery()))
                        {
                            _logger.LogDebug("Adding the video {VideoName} to the playlist {PlaylistName}", jfItem.Name, jfPlaylist.Name);
                            var action = new CustomPlaylistEntryAction(CustomPlaylistAction.Create, jfItem.ProviderIds[Constants.ProviderName]);
                            var response = await taApi.CustomPlaylistEntryAction(taPlaylistId, action).ConfigureAwait(true);
                            if (response != System.Net.HttpStatusCode.OK)
                            {
                                _logger.LogError("Failed to add the video {JFItem} to playlist {JFPlaylist}", $"{jfItem.Name} ({jfItem.ProviderIds[Constants.ProviderName]})", jfPlaylist.Name);
                                continue;
                            }

                            processedVideosCount++;
                            progress.Report(processedVideosCount * 100 / totalVideosCount);
                        }
                    }
                }

                _logger.LogInformation("Time elapsed: {Time}", DateTime.Now - start);
            }
            else
            {
                _logger.LogInformation("Jellyfin->TubeArchivist playlists synchronization is currently disabled.");
            }

            progress.Report(100);
        }

        private async Task MoveOrDeleteVideo(TubeArchivistApi taApi, MediaBrowser.Controller.Playlists.Playlist jfPlaylist, string taPlaylistId, PlaylistItemAction item)
        {
            var response = HttpStatusCode.OK;
            for (var i = 0; i < item.PositionsToMove; i++)
            {
                var action = new CustomPlaylistEntryAction(item.Action, item.YoutubeId);
                response = await taApi.CustomPlaylistEntryAction(taPlaylistId, action).ConfigureAwait(true);
                if (response != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError("Failed to execute the operation {Operation} for the video {JFItem} in playlist {PlaylistName}", item.Action, $"{item.Name} ({item.YoutubeId})", jfPlaylist.Name);
                }
            }
        }

        private async Task<bool> AddVideoToTAPlaylist(TubeArchivistApi taApi, MediaBrowser.Controller.Playlists.Playlist jfPlaylist, string taPlaylistId, PlaylistItemAction item)
        {
            // TA playlist doesn't contain the JF video
            _logger.LogDebug("Adding the video {VideoName} to the playlist {PlaylistName}", item.Name, jfPlaylist.Name);
            var action = new CustomPlaylistEntryAction(CustomPlaylistAction.Create, item.YoutubeId);
            var response = await taApi.CustomPlaylistEntryAction(taPlaylistId, action).ConfigureAwait(true);
            if (response != System.Net.HttpStatusCode.OK)
            {
                _logger.LogError("Failed to add the video {JFItem} to playlist {JFPlaylist}", $"{item.Name} ({item.YoutubeId})", jfPlaylist.Name);
                return false;
            }

            // Since now we have the video added at the bottom of the TA playlist (previouse entries count + 1),
            // we need to move the video to the correct position
            var positionsToMove = item.PositionsToMove;
            action = new CustomPlaylistEntryAction(CustomPlaylistAction.Up, item.YoutubeId);
            if (item.PositionsToMove < 0)
            {
                positionsToMove = (int)Math.Abs((decimal)positionsToMove!);
                action = new CustomPlaylistEntryAction(CustomPlaylistAction.Up, item.YoutubeId);
            }

            _logger.LogDebug("Moving the video {VideoName} of the playlist {PlaylistName} {Positions} up", item.Name, jfPlaylist.Name, positionsToMove);
            for (var i = 0; i < positionsToMove; i++)
            {
                response = await taApi.CustomPlaylistEntryAction(taPlaylistId, action).ConfigureAwait(true);
                if (response != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError("Failed to move {Direction} the video {JFItem} in playlist {JFPlaylist}", item.Action, $"{item.Name} ({item.YoutubeId})", jfPlaylist.Name);
                    continue;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return
            [
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerInterval,
                    IntervalTicks = TimeSpan.FromSeconds(Plugin.Instance!.Configuration.TAJFTaskInterval).Ticks
                },
            ];
        }

        private sealed class PlaylistItemAction
        {
            public PlaylistItemAction(
                int index,
                string youtubeId,
                CustomPlaylistAction action,
                int positionsToMove,
                string name)
            {
                Index = index;
                YoutubeId = youtubeId;
                Action = action;
                PositionsToMove = positionsToMove;
                Name = name;
            }

            public PlaylistItemAction(
                int index,
                string youtubeId,
                CustomPlaylistAction action,
                string name)
            {
                Index = index;
                YoutubeId = youtubeId;
                Action = action;
                Name = name;
                PositionsToMove = 1;
            }

            public PlaylistItemAction(
                string youtubeId,
                string name)
            {
                YoutubeId = youtubeId;
                Action = CustomPlaylistAction.Remove;
                Name = name;
                PositionsToMove = 1;
            }

            public int? Index { get; set; }

            public string YoutubeId { get; set; }

            public string Name { get; set; }

            public CustomPlaylistAction Action { get; set; }

            public int? PositionsToMove { get; set; }
        }
    }
}
