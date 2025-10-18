using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.TubeArchivistMetadata.Configuration;
using Jellyfin.Plugin.TubeArchivistMetadata.Tasks;
using Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist;
using Jellyfin.Plugin.TubeArchivistMetadata.Utilities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeArchivistMetadata
{
    /// <summary>
    /// The main plugin.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private Guid? _tubeArchivistCollectionId;

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="taskManager">Instance of the <see cref="ITaskManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        public Plugin(
            IApplicationPaths applicationPaths,
            IXmlSerializer xmlSerializer,
            ILogger<Plugin> logger,
            ISessionManager sessionManager,
            ILibraryManager libraryManager,
            ITaskManager taskManager,
            IUserManager userManager,
            IUserDataManager userDataManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
            Logger = logger;
            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            handler.CheckCertificateRevocationList = true;
            HttpClient = new HttpClient(handler);
            UpdateAuthorizationHeader(Configuration.TubeArchivistApiKey);

            SessionManager = sessionManager;
            sessionManager.PlaybackProgress += OnPlaybackProgress;
            LibraryManager = libraryManager;
            _userManager = userManager;
            _userDataManager = userDataManager;
            userDataManager.UserDataSaved += OnWatchedStatusChange;

            // Cache the TubeArchivist collection ID for efficient lookups
            CacheTubeArchivistCollectionId();

            var taToJellyfinProgressSyncTask = new TAToJellyfinProgressSyncTask(logger, libraryManager, userManager, userDataManager);
            var jfToTubearchivistProgressSyncTask = new JFToTubearchivistProgressSyncTask(logger, libraryManager, userManager, userDataManager);
            var isTAJFTaskPresent = taskManager.ScheduledTasks.Any(t => t.Name.Equals(taToJellyfinProgressSyncTask.Name, StringComparison.Ordinal));
            if (Instance!.Configuration.TAJFSync && !isTAJFTaskPresent)
            {
                logger.LogInformation("Queueing task {TaskName}.", taToJellyfinProgressSyncTask.Name);
                taskManager.AddTasks([taToJellyfinProgressSyncTask]);
                taskManager.Execute<TAToJellyfinProgressSyncTask>();
            }

            var isJFTATaskPresent = taskManager.ScheduledTasks.Any(t => t.Name.Equals(jfToTubearchivistProgressSyncTask.Name, StringComparison.Ordinal));
            if (Instance!.Configuration.JFTASync && !isJFTATaskPresent)
            {
                logger.LogInformation("Queueing task {TaskName}.", jfToTubearchivistProgressSyncTask.Name);
                taskManager.AddTasks([jfToTubearchivistProgressSyncTask]);
                taskManager.Execute<JFToTubearchivistProgressSyncTask>();
            }

            logger.LogInformation("{Message}", "Collection display name: " + Instance?.Configuration.CollectionTitle);
            logger.LogInformation("{Message}", "TubeArchivist API URL: " + Instance?.Configuration.TubeArchivistUrl);
            logger.LogInformation("{Message}", "Pinging TubeArchivist API...");
        }

        /// <inheritdoc />
        public override string Name => Constants.PluginName;

        /// <inheritdoc />
        public override Guid Id => Guid.Parse(Constants.PluginGuid);

        /// <summary>
        /// Gets the current plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <summary>
        /// Gets the logger instance used globally by the plugin.
        /// </summary>
        public ILogger<Plugin> Logger { get; }

        /// <summary>
        /// Gets the HTTP client used globally by the plugin.
        /// </summary>
        public HttpClient HttpClient { get; }

        /// <summary>
        /// Gets the SessionManager used globally by the plugin.
        /// </summary>
        public ISessionManager SessionManager { get; }

        /// <summary>
        /// Gets the LibraryManager used globally by the plugin.
        /// </summary>
        public ILibraryManager LibraryManager { get; }

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages() => new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };

        /// <summary>
        /// Updates the HTTP client's Authorization header with the current API key.
        /// </summary>
        /// <param name="apiKey">TubeArchivist API key.</param>
        public void UpdateAuthorizationHeader(string apiKey)
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", apiKey);
                Logger.LogInformation("{Message}", "Updated Authorization header with API key");
            }
            else
            {
                Logger.LogWarning("{Message}", "No TubeArchivist API key configured");
            }
        }

        /// <summary>
        /// Logs the TubeArchivist API connection status.
        /// </summary>
        public void LogTAApiConnectionStatus()
        {
            Logger.LogInformation("{Message}", "Ping...");

            var api = TubeArchivistApi.GetInstance();
            Task.Run(() => api.Ping()).ContinueWith(
                task =>
                {
                    if (task.Result != null)
                    {
                        Logger.LogInformation("{Message}", "TubeArchivist API said: " + task.Result.Response + "!");
                    }
                    else
                    {
                        Logger.LogCritical("{Message}", "TubeArchivist API was unreachable!");
                    }
                },
                TaskScheduler.Default);
        }

        private void CacheTubeArchivistCollectionId(string? collectionTitle = null)
        {
            try
            {
                string titleToSearch = collectionTitle ?? Configuration.CollectionTitle;

                if (string.IsNullOrEmpty(titleToSearch))
                {
                    Logger.LogWarning("TubeArchivist collection title not configured");
                    _tubeArchivistCollectionId = null;
                    return;
                }

                // Find the collection by name
                var collections = LibraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { Jellyfin.Data.Enums.BaseItemKind.CollectionFolder, Jellyfin.Data.Enums.BaseItemKind.Folder },
                    Recursive = false
                });

                var tubeArchivistCollection = collections.FirstOrDefault(c =>
                    c.Name.Equals(titleToSearch, StringComparison.OrdinalIgnoreCase));

                if (tubeArchivistCollection != null)
                {
                    _tubeArchivistCollectionId = tubeArchivistCollection.Id;
                    Logger.LogInformation("Found TubeArchivist collection with ID: {CollectionId}", _tubeArchivistCollectionId);
                }
                else
                {
                    Logger.LogWarning("Could not find TubeArchivist collection with name: {CollectionName}", titleToSearch);
                    _tubeArchivistCollectionId = null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error caching TubeArchivist collection ID");
                _tubeArchivistCollectionId = null;
            }
        }

        /// <summary>
        /// Refreshes the cached TubeArchivist collection ID.
        /// Called when the collection title changes in configuration.
        /// </summary>
        /// <param name="collectionTitle">Optional collection title to use. If null, reads from Configuration.</param>
        public void RefreshTubeArchivistCollectionId(string? collectionTitle = null)
        {
            Logger.LogInformation("Refreshing TubeArchivist collection cache due to configuration change");
            CacheTubeArchivistCollectionId(collectionTitle ?? Configuration.CollectionTitle);
        }

        /// <summary>
        /// Validates if an item belongs to the TubeArchivist collection.
        /// Uses cached collection ID for efficient lookups without traversing hierarchy.
        /// </summary>
        /// <param name="item">The item to validate.</param>
        /// <returns>True if the item is a valid Episode in the TubeArchivist collection; otherwise, false.</returns>
        private bool IsItemInTubeArchivistCollection(BaseItem item)
        {
            // Type check first (cheapest operation)
            if (item is not Episode)
            {
                return false;
            }

            // If we couldn't cache the collection ID, fall back to name-based check
            if (_tubeArchivistCollectionId == null)
            {
                // Fallback: traverse hierarchy to find collection
                return IsItemInTubeArchivistCollectionByHierarchy(item);
            }

            // Efficient check: Walk up the parent chain looking for our cached collection ID
            // This is much faster than multiple GetItemById calls
            var currentItem = item;
            for (int depth = 0; depth < 10; depth++) // Limit depth to prevent infinite loops
            {
                if (currentItem.Id == _tubeArchivistCollectionId)
                {
                    return true;
                }

                if (currentItem.ParentId == Guid.Empty)
                {
                    break;
                }

                var parent = LibraryManager.GetItemById(currentItem.ParentId);
                if (parent == null)
                {
                    break;
                }

                // Check if this parent is our target collection
                if (parent.Id == _tubeArchivistCollectionId)
                {
                    return true;
                }

                currentItem = parent;
            }

            return false;
        }

        /// <summary>
        /// Fallback method for validating collection membership when collection ID is not cached.
        /// </summary>
        private bool IsItemInTubeArchivistCollectionByHierarchy(BaseItem item)
        {
            // Early exit if parent ID is empty or null
            if (item.ParentId == Guid.Empty)
            {
                return false;
            }

            // Traverse hierarchy safely with null checks
            BaseItem? season = LibraryManager.GetItemById(item.ParentId);
            if (season?.ParentId == null || season.ParentId == Guid.Empty)
            {
                return false;
            }

            BaseItem? channel = LibraryManager.GetItemById(season.ParentId);
            if (channel?.ParentId == null || channel.ParentId == Guid.Empty)
            {
                return false;
            }

            BaseItem? collection = LibraryManager.GetItemById(channel.ParentId);

            // Check if this is the configured TubeArchivist collection
            return collection?.Name.Equals(Instance?.Configuration.CollectionTitle, StringComparison.OrdinalIgnoreCase) ?? false;
        }

        private async void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs eventArgs)
        {
            // Early exit checks - ordered from cheapest to most expensive
            // 1. Configuration check (memory access)
            if (!Instance!.Configuration.JFTASync)
            {
                return;
            }

            // 2. User check (list iteration)
            if (!eventArgs.Users.Any(u => Instance!.Configuration.JFUsernameFrom.Equals(u.Username, StringComparison.Ordinal)))
            {
                return;
            }

            // 3. Playback position check (null check)
            if (eventArgs.PlaybackPositionTicks == null)
            {
                return;
            }

            // 4. Type and collection check (type check + cached ID lookup)
            // This is now much more efficient - single type check + ID comparison vs 3 database calls
            if (!IsItemInTubeArchivistCollection(eventArgs.Item))
            {
                return;
            }

            // At this point, we know it's a valid TubeArchivist Episode - safe to proceed
            try
            {
                long progress = (long)eventArgs.PlaybackPositionTicks / TimeSpan.TicksPerSecond;
                var videoId = Utils.GetVideoNameFromPath(eventArgs.Item.Path);
                var statusCode = await TubeArchivistApi.GetInstance().SetProgress(videoId, progress).ConfigureAwait(true);

                if (statusCode != System.Net.HttpStatusCode.OK)
                {
                    Logger.LogCritical("{Message}", $"POST /video/{videoId}/progress returned {statusCode} for video {eventArgs.Item.Name} with progress {progress} seconds");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error syncing playback progress for item {ItemName}", eventArgs.Item.Name);
            }
        }

        private async void OnWatchedStatusChange(object? sender, UserDataSaveEventArgs eventArgs)
        {
            // Early exit checks
            var user = _userManager.GetUserById(eventArgs.UserId);
            if (!Configuration.JFTASync || user == null || !Configuration.GetJFUsernamesToArray().Contains(user.Username))
            {
                return;
            }

            var isPlayed = eventArgs.Item.IsPlayed(user);
            Logger.LogDebug("User {UserId} changed watched status to {Status} for the item {ItemName}", eventArgs.UserId, isPlayed, eventArgs.Item.Name);

            string itemYTId;
            try
            {
                // Handle Series (YouTube Channels)
                if (eventArgs.Item is Series)
                {
                    // For Series, we still need to validate collection membership
                    // but can use the efficient cached ID method
                    if (_tubeArchivistCollectionId != null && eventArgs.Item.Id != _tubeArchivistCollectionId)
                    {
                        // Check if this Series is a child of the TubeArchivist collection
                        var parent = eventArgs.Item.ParentId != Guid.Empty
                            ? LibraryManager.GetItemById(eventArgs.Item.ParentId)
                            : null;

                        if (parent?.Id != _tubeArchivistCollectionId)
                        {
                            return;
                        }
                    }

                    itemYTId = Utils.GetChannelNameFromPath(eventArgs.Item.Path);
                }

                // Handle Episodes (YouTube Videos) - with collection validation
                else if (eventArgs.Item is Episode)
                {
                    // Validate this Episode belongs to TubeArchivist collection
                    if (!IsItemInTubeArchivistCollection(eventArgs.Item))
                    {
                        return;
                    }

                    itemYTId = Utils.GetVideoNameFromPath(eventArgs.Item.Path);
                }
                else
                {
                    // Not a relevant item type for TubeArchivist
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while processing item path: {ItemPath}", eventArgs.Item.Path ?? "null");
                return;
            }

            try
            {
                var statusCode = await TubeArchivistApi.GetInstance().SetWatchedStatus(itemYTId, isPlayed).ConfigureAwait(true);
                if (statusCode != System.Net.HttpStatusCode.OK)
                {
                    Logger.LogCritical("POST /watched returned {StatusCode} for item {ItemName} ({VideoYTId}) with watched status {IsPlayed}", statusCode, eventArgs.Item.Name, itemYTId, isPlayed);
                }

                // Sync progress for Episodes only
                if (eventArgs.Item is Episode)
                {
                    var progress = _userDataManager.GetUserData(user, eventArgs.Item).PlaybackPositionTicks / TimeSpan.TicksPerSecond;
                    var videoId = Utils.GetVideoNameFromPath(eventArgs.Item.Path);
                    statusCode = await TubeArchivistApi.GetInstance().SetProgress(videoId, progress).ConfigureAwait(true);
                    if (statusCode != System.Net.HttpStatusCode.OK)
                    {
                        Logger.LogCritical("{Message}", $"POST /video/{videoId}/progress returned {statusCode} for video {eventArgs.Item.Name} with progress {progress} seconds");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogCritical("An exception occurred while calling POST /watched for item {ItemName} ({VideoYTId}) with watched status {IsPlayed}: {ExceptionMessage}", eventArgs.Item.Name, itemYTId, isPlayed, ex.Message);
            }
        }
    }
}
