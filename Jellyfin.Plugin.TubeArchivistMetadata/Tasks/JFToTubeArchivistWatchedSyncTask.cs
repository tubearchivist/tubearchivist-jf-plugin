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
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Tasks
{
    /// <summary>
    /// Task to sync Jellyfin playback progresses to TubeArchivist.
    /// </summary>
    public class JFToTubearchivistWatchedSyncTask : IScheduledTask
    {
        private readonly ILogger<Plugin> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="JFToTubearchivistWatchedSyncTask"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="libraryManager">Library manager.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="userDataManager">User data manager.</param>
        public JFToTubearchivistWatchedSyncTask(ILogger<Plugin> logger, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
        }

        /// <inheritdoc/>
        public string Name => "JFToTubeArchivistWatchedSyncTask";

        /// <inheritdoc/>
        public string Description => "This tasks syncs TubeArchivist watched statuses to Jellyfin";

        /// <inheritdoc/>
        public string Category => "TubeArchivistMetadata";

        /// <inheritdoc/>
        public string Key => "JFToTubeArchivistWatchedSyncTask";

        /// <inheritdoc/>
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (Plugin.Instance!.Configuration.JFTASync)
            {
                var start = DateTime.Now;
                _logger.LogInformation("Starting Jellyfin->TubeArchivist watched statuses synchronization.");
                var taApi = TubeArchivistApi.GetInstance();
                foreach (var jfUsername in Plugin.Instance!.Configuration.GetJFUsernamesToArray())
                {
                    var user = _userManager.GetUserByName(jfUsername);
                    if (user == null)
                    {
                        _logger.LogInformation("{Message}", $"Jellyfin user with username {jfUsername} not found");
                        continue;
                    }

                    var collectionItem = _libraryManager.GetItemList(new InternalItemsQuery
                    {
                        Name = Plugin.Instance?.Configuration.CollectionTitle,
                        IncludeItemTypes = new[] { BaseItemKind.CollectionFolder }
                    }).FirstOrDefault();

                    if (collectionItem == null)
                    {
                        var message = $"Collection '{Plugin.Instance?.Configuration.CollectionTitle}' not found.";
                        _logger.LogCritical("{Message}", message);
                    }
                    else
                    {
                        var collection = (CollectionFolder)collectionItem;
                        var channels = collection.GetChildren(user, false, new InternalItemsQuery
                        {
                            IncludeItemTypes = new[] { BaseItemKind.Series }
                        });
                        _logger.LogInformation("Analyzing collection {Id} with name {Name}", collectionItem.Id, collectionItem.Name);
                        _logger.LogInformation("Found {Message} channels", channels.Count);

                        foreach (Series channel in channels)
                        {
                            var channelYTId = Utils.GetChannelNameFromPath(channel.Path);
                            var isChannelWatched = false;
                            var isChannelCheckedForWatched = false;
                            var years = channel.GetChildren(user, false, new InternalItemsQuery
                            {
                                IncludeItemTypes = new[] { BaseItemKind.Season }
                            });
                            _logger.LogInformation("Found {Years} years in channel {ChannelName}", years.Count, channel.Name);

                            foreach (Season year in years)
                            {
                                var videos = year.GetChildren(user, false, new InternalItemsQuery
                                {
                                    IncludeItemTypes = new[] { BaseItemKind.Episode }
                                });
                                _logger.LogInformation("Found {Videos} videos in year {YearName} of the channel {ChannelName}", videos.Count, year.Name, channel.Name);

                                foreach (Episode video in videos)
                                {
                                    var videoYTId = Utils.GetVideoNameFromPath(video.Path);

                                    if (!isChannelCheckedForWatched && channel.IsPlayed(user))
                                    {
                                        var isChannelPlayed = channel.IsPlayed(user);
                                        var statusCode = await taApi.SetWatchedStatus(channelYTId, isChannelPlayed).ConfigureAwait(true);
                                        if (statusCode != System.Net.HttpStatusCode.OK)
                                        {
                                            _logger.LogInformation("{Message}", $"POST /watched returned {statusCode} for channel {channel.Name} ({channelYTId}) with wacthed status {isChannelPlayed}");
                                        }
                                        else
                                        {
                                            isChannelWatched = true;
                                        }

                                        isChannelCheckedForWatched = true;
                                    }

                                    if (!isChannelWatched)
                                    {
                                        var isVideoPlayed = video.IsPlayed(user);
                                        var statusCode = await taApi.SetWatchedStatus(videoYTId, isVideoPlayed).ConfigureAwait(true);
                                        if (statusCode != System.Net.HttpStatusCode.OK)
                                        {
                                            _logger.LogInformation("{Message}", $"POST /watched returned {statusCode} for video {video.Name} ({videoYTId}) with wacthed status {isVideoPlayed}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation("Time elapsed: {Time}", DateTime.Now - start);
            }
            else
            {
                _logger.LogInformation("Jellyfin->TubeArchivist watched status is currently disabled.");
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return
            [
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerInterval,
                    IntervalTicks = TimeSpan.FromSeconds(Plugin.Instance!.Configuration.JFTAWatchedTaskInterval).Ticks
                },
            ];
        }
    }
}
