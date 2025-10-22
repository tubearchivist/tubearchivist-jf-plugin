using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class JFToTubeArchivistProgressSyncTask : IScheduledTask
    {
        private readonly ILogger<Plugin> _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="JFToTubeArchivistProgressSyncTask"/> class.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="libraryManager">Library manager.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="userDataManager">User data manager.</param>
        public JFToTubeArchivistProgressSyncTask(ILogger<Plugin> logger, ILibraryManager libraryManager, IUserManager userManager, IUserDataManager userDataManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
        }

        /// <inheritdoc/>
        public string Name => "JFToTubeArchivistProgressSyncTask";

        /// <inheritdoc/>
        public string Description => "This tasks syncs Jellyfin playback progresses to TubeArchivist";

        /// <inheritdoc/>
        public string Category => "TubeArchivistMetadata";

        /// <inheritdoc/>
        public string Key => "JFToTubeArchivistProgressSyncTask";

        /// <inheritdoc/>
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            progress.Report(0);
            if (Plugin.Instance!.Configuration.JFTAProgressSync)
            {
                var start = DateTime.Now;
                _logger.LogInformation("Starting Jellyfin->TubeArchivist playback progresses synchronization.");
                var taApi = TubeArchivistApi.GetInstance();
                var videosCount = 0;
                var jfUsername = Plugin.Instance!.Configuration.JFUsernameFrom;
                var user = _userManager.GetUserByName(jfUsername);
                if (user == null)
                {
                    _logger.LogInformation("{Message}", $"Jellyfin user with username {jfUsername} not found");
                    return;
                }

                var items = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    Name = Plugin.Instance?.Configuration.CollectionTitle,
                    IncludeItemTypes = new[] { BaseItemKind.CollectionFolder }
                });

                var collectionItem = items.Count > 0 ? items[0] : null;

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
                    _logger.LogDebug("Analyzing collection {Id} with name {Name}", collectionItem.Id, collectionItem.Name);
                    _logger.LogDebug("Found {Message} channels", channels.Count);

                    foreach (Series channel in channels)
                    {
                        var channelYTId = Utils.GetChannelNameFromPath(channel.Path);
                        var years = channel.GetChildren(user, false, new InternalItemsQuery
                        {
                            IncludeItemTypes = new[] { BaseItemKind.Season }
                        });
                        _logger.LogDebug("Found {Years} years in channel {ChannelName}", years.Count, channel.Name);

                        foreach (Season year in years)
                        {
                            var videos = year.GetChildren(user, false, new InternalItemsQuery
                            {
                                IncludeItemTypes = new[] { BaseItemKind.Episode }
                            });
                            _logger.LogDebug("Found {Videos} videos in year {YearName} of the channel {ChannelName}", videos.Count, year.Name, channel.Name);
                            videosCount += videos.Count;
                        }
                    }
                }

                _logger.LogDebug("Found a total of {VideosCount} videos", videosCount);

                var processedVideosCount = 0;
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

                    foreach (Series channel in channels)
                    {
                        var channelYTId = Utils.GetChannelNameFromPath(channel.Path);
                        var isChannelWatched = false;
                        var isChannelCheckedForWatched = false;
                        var years = channel.GetChildren(user, false, new InternalItemsQuery
                        {
                            IncludeItemTypes = new[] { BaseItemKind.Season }
                        });

                        foreach (Season year in years)
                        {
                            var videos = year.GetChildren(user, false, new InternalItemsQuery
                            {
                                IncludeItemTypes = new[] { BaseItemKind.Episode }
                            });
                            videosCount += videos.Count;

                            foreach (Episode video in videos)
                            {
                                var videoYTId = Utils.GetVideoNameFromPath(video.Path);
                                _logger.LogDebug("Current video extracted YouTube id: {VideoYtId}", videoYTId);
                                HttpStatusCode statusCode;
                                var userItemData = _userDataManager.GetUserData(user, channel);

                                if (!isChannelCheckedForWatched && channel.IsPlayed(user, userItemData))
                                {
                                    var isChannelPlayed = channel.IsPlayed(user, userItemData);
                                    statusCode = await taApi.SetWatchedStatus(channelYTId, isChannelPlayed).ConfigureAwait(true);
                                    if (statusCode != System.Net.HttpStatusCode.OK)
                                    {
                                        _logger.LogCritical("{Message}", $"POST /watched returned {statusCode} for channel {channel.Name} ({channelYTId}) with wacthed status {isChannelPlayed}");
                                    }
                                    else
                                    {
                                        isChannelWatched = true;
                                    }

                                    isChannelCheckedForWatched = true;
                                }

                                if (!isChannelWatched)
                                {
                                    var isVideoPlayed = video.IsPlayed(user, userItemData);
                                    statusCode = await taApi.SetWatchedStatus(videoYTId, isVideoPlayed).ConfigureAwait(true);
                                    if (statusCode != System.Net.HttpStatusCode.OK)
                                    {
                                        _logger.LogCritical("{Message}", $"POST /watched returned {statusCode} for video {video.Name} ({videoYTId}) with wacthed status {isVideoPlayed}");
                                    }

                                    _logger.LogDebug("{Message}", isVideoPlayed);
                                    if (!isVideoPlayed)
                                    {
                                        var playbackProgress = _userDataManager.GetUserData(user, video)?.PlaybackPositionTicks / TimeSpan.TicksPerSecond;
                                        if (playbackProgress != null)
                                        {
                                            statusCode = await taApi.SetProgress(videoYTId, playbackProgress.Value).ConfigureAwait(true);
                                            if (statusCode != System.Net.HttpStatusCode.OK)
                                            {
                                                _logger.LogCritical("{Message}", $"POST /video/{videoYTId}/progress returned {statusCode} for video {video.Name} with progress {progress} seconds");
                                            }
                                        }
                                    }
                                }

                                processedVideosCount++;
                                progress.Report(processedVideosCount * 100 / videosCount);
                            }
                        }
                    }
                }

                _logger.LogInformation("Time elapsed: {Time}", DateTime.Now - start);
            }
            else
            {
                _logger.LogInformation("Jellyfin->TubeArchivist playback synchronization is currently disabled.");
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
                    Type = TaskTriggerInfoType.StartupTrigger,
                },
            ];
        }
    }
}
