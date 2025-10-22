using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
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
    public class TAToJellyfinPlaylistsSyncTask : IScheduledTask
    {
        private readonly ILogger<Plugin> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IPlaylistManager _playlistManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TAToJellyfinPlaylistsSyncTask"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="libraryManager">Library manager.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="playlistManager">Playlists manager.</param>
        public TAToJellyfinPlaylistsSyncTask(ILogger<Plugin> logger, ILibraryManager libraryManager, IUserManager userManager, IPlaylistManager playlistManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _playlistManager = playlistManager;
        }

        /// <inheritdoc/>
        public string Name => "TAToJellyfinPlaylistsSyncTask";

        /// <inheritdoc/>
        public string Description => "This tasks syncs TubeArchivist playlists to Jellyfin";

        /// <inheritdoc/>
        public string Category => "TubeArchivistMetadata";

        /// <inheritdoc/>
        public string Key => "TAToJellyfinPlaylistsSyncTask";

        private int CountTotalVideos(ISet<TubeArchivist.Playlist> taPlaylists)
        {
            var totalEntries = 0;
            foreach (var taPlaylist in taPlaylists)
            {
                totalEntries += taPlaylist.Entries.Count;
            }

            return totalEntries;
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(0);
            if (Plugin.Instance!.Configuration.TAJFPlaylistsSync)
            {
                var start = DateTime.Now;
                _logger.LogInformation("Starting TubeArchivist->Jellyfin playlists synchronization.");
                var taApi = TubeArchivistApi.GetInstance();
                _logger.LogInformation("Getting TubeArchivist playlists");
                var taPlaylists = await taApi.GetPlaylists().ConfigureAwait(true);
                _logger.LogInformation("Received playlists:\n{Playlists}", taPlaylists);
                if (taPlaylists != null)
                {
                    var totalVideosCount = CountTotalVideos(taPlaylists);
                    var processedVideosCount = 0;
                    foreach (var jfUsername in Plugin.Instance!.Configuration.GetJFUsernamesToArray())
                    {
                        var user = _userManager.GetUserByName(jfUsername);
                        if (user == null)
                        {
                            _logger.LogInformation("{Message}", $"Jellyfin user with username {jfUsername} not found");
                            continue;
                        }

                        var userPlaylists = _playlistManager.GetPlaylists(user.Id).ToList();

                        if (Plugin.Instance!.Configuration.TAJFPlaylistsDelete)
                        {
                            var jfPlaylistsToDelete = userPlaylists.Where(up => !taPlaylists.Select(tp => tp.Id).Contains(Utils.GetTAPlaylistIdFromName(up.Name)));
                            foreach (var jfPlaylistToDelete in jfPlaylistsToDelete)
                            {
                                _logger.LogInformation("Deleting Jellyfin playlist {PlaylistName}", jfPlaylistToDelete.Name);
                                _libraryManager.DeleteItem(jfPlaylistToDelete, new DeleteOptions());
                            }
                        }

                        foreach (var taPlaylist in taPlaylists)
                        {
                            var playlistName = taPlaylist.Type == PlaylistType.Regular ? $"{taPlaylist.Name} - {taPlaylist.Channel} ({taPlaylist.Id})" : $"{taPlaylist.Name} ({taPlaylist.Id})";
                            var userPlaylist = userPlaylists.Where(up => up.Name == playlistName).FirstOrDefault();

                            var jfEntryIds = new List<Guid>();
                            var currentPlaylistVideos = 0;
                            foreach (var taEntry in taPlaylist.Entries)
                            {
                                currentPlaylistVideos++;
                                var taEntryStr = $"{taEntry.Uploader} - {taEntry.Title} ({taEntry.YoutubeId}) in playlist {playlistName}";
                                if (!taEntry.IsDownloaded)
                                {
                                    _logger.LogInformation("The entry {TAEntry} was skipped because has not been downloaded by TubeArchivist", taEntryStr);
                                    continue;
                                }

                                var entries = _libraryManager.GetItemList(new InternalItemsQuery
                                {
                                    HasAnyProviderId = new Dictionary<string, string>()
                                        {
                                            {
                                                Constants.ProviderName, taEntry.YoutubeId
                                            }
                                        }
                                });

                                var jfEntry = entries.Count > 0 ? entries[0] : null;

                                if (jfEntry != null)
                                {
                                    jfEntryIds.Add(jfEntry.Id);
                                }
                                else
                                {
                                    _logger.LogWarning("The relative video for {TAEntry} was not found on Jellyfin", taEntryStr);
                                }
                            }

                            if (userPlaylist != null)
                            {
                                var updateRequest = new PlaylistUpdateRequest
                                {
                                    Id = userPlaylist.Id,
                                    UserId = user.Id,
                                    Name = playlistName,
                                    Ids = jfEntryIds
                                };

                                var result = _playlistManager.UpdatePlaylist(updateRequest);
                                _logger.LogInformation("Updated playlist {PlaylistName} with id {Id}", playlistName, result.Id);
                            }
                            else
                            {
                                var creationRequest = new PlaylistCreationRequest
                                {
                                    Name = playlistName,
                                    ItemIdList = jfEntryIds,
                                    MediaType = MediaType.Video,
                                    UserId = user.Id
                                };

                                var result = await _playlistManager.CreatePlaylist(creationRequest).ConfigureAwait(true);
                                _logger.LogInformation("Created playlist {PlaylistName} with id {Id}", playlistName, result.Id);
                            }

                            processedVideosCount += currentPlaylistVideos;
                            progress.Report(processedVideosCount * 100 / totalVideosCount);
                        }
                    }
                }

                _logger.LogInformation("Time elapsed: {Time}", DateTime.Now - start);
            }
            else
            {
                _logger.LogInformation("TubeArchivist->Jellyfin playlists synchronization is currently disabled.");
            }

            progress.Report(100);
        }

        /// <inheritdoc/>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return
            [
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.IntervalTrigger,
                    IntervalTicks = TimeSpan.FromSeconds(Plugin.Instance!.Configuration.TAJFProgressTaskInterval).Ticks
                },
            ];
        }
    }
}
