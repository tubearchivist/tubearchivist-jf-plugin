using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Jellyfin.Plugin.TubeArchivistMetadata.Utilities;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Configuration
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        private ILogger _logger;
        private string _tubeArchivistUrl;
        private string _tubeArchivistApiKey;
        private HashSet<string> _jfUsernamesTo;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            if (Plugin.Instance == null)
            {
                throw new DataException("Uninitialized plugin!");
            }
            else
            {
                _logger = Plugin.Instance.Logger;
            }

            CollectionTitle = string.Empty;
            _tubeArchivistUrl = string.Empty;
            _tubeArchivistApiKey = string.Empty;
            MaxDescriptionLength = 500;
            JFTAProgressSync = false;
            JFUsernameFrom = string.Empty;
            TAJFProgressSync = false;
            JFTAPlaylistsSync = false;
            JFTAPlaylistsDelete = false;
            TAJFPlaylistsSync = false;
            TAJFPlaylistsDelete = false;
            _jfUsernamesTo = new HashSet<string>();
            TAJFProgressTaskInterval = 60;
            JFTAPlaylistsSyncTaskInterval = 60;
            TAJFPlaylistsSyncTaskInterval = 60;
        }

        /// <summary>
        /// Gets or sets TubeArchivist collection display name.
        /// </summary>
        public string CollectionTitle { get; set; }

        /// <summary>
        /// Gets or sets TubeArchivist URL.
        /// </summary>
        public string TubeArchivistUrl
        {
            get
            {
                return _tubeArchivistUrl;
            }

            set
            {
                if (value.StartsWith("http://", StringComparison.CurrentCulture) || value.StartsWith("https://", StringComparison.CurrentCulture))
                {
                    _tubeArchivistUrl = Utils.SanitizeUrl(value);
                }
                else
                {
                    _logger.LogInformation("{Message}", "Given TubeArchivist URL contains no schema. Adding http://...");
                    _tubeArchivistUrl = Utils.SanitizeUrl("http://" + value);
                }

                Plugin.Instance?.LogTAApiConnectionStatus();
            }
        }

        /// <summary>
        /// Gets or sets TubeArchivist API key.
        /// </summary>
        public string TubeArchivistApiKey
        {
            get
            {
                return _tubeArchivistApiKey;
            }

            set
            {
                _tubeArchivistApiKey = value;
                Plugin.Instance?.LogTAApiConnectionStatus();
                Plugin.Instance?.UpdateAuthorizationHeader(value);
            }
        }

        /// <summary>
        /// Gets or sets maximum series and episodes overviews length.
        /// </summary>
        public int MaxDescriptionLength { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable TA->JF playback progress synchronization.
        /// </summary>
        public bool TAJFProgressSync { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable JF->TA playback progress synchronization.
        /// </summary>
        public bool JFTAProgressSync { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable JF->TA playlists synchronization.
        /// </summary>
        public bool JFTAPlaylistsSync { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to delete playlists from TA when not found on JF.
        /// </summary>
        public bool JFTAPlaylistsDelete { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable TA->JF playlists synchronization.
        /// </summary>
        public bool TAJFPlaylistsSync { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to delete playlists from JF when not found on TA.
        /// </summary>
        public bool TAJFPlaylistsDelete { get; set; }

        /// <summary>
        /// Gets or sets the playback progress owner Jellyfin username to synchronize data to TubeArchivist.
        /// </summary>
        public string JFUsernameFrom { get; set; }

        /// <summary>
        /// Gets or sets the playback progress owners Jellyfin usernames to synchronize data from TubeArchivist.
        /// </summary>
        public string JFUsernamesTo
        {
            get
            {
                _logger.LogInformation("JFUsernamesTo configured: {Message}", string.Join(", ", _jfUsernamesTo));
                return string.Join(", ", _jfUsernamesTo);
            }

            set
            {
                // Clear existing usernames
                _jfUsernamesTo.Clear();

                // Split by comma, then trim each part to remove leading/trailing spaces
                foreach (var username in value.Split(','))
                {
                    var trimmedUsername = username.Trim();
                    if (!string.IsNullOrEmpty(trimmedUsername))
                    {
                        _jfUsernamesTo.Add(trimmedUsername);
                    }
                }

                _logger.LogInformation("Set JFUsernamesTo to: {Message}", string.Join(", ", _jfUsernamesTo));
            }
        }

        /// <summary>
        /// Gets or sets the interval in seconds at which the TubeArchivist to Jellyfin playback progress synchronization task should run.
        /// It requires Jellyfin server restart to take effect.
        /// </summary>
        public int TAJFProgressTaskInterval { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds at which the Jellyfin to TubeArchivist playlists synchronization task should run.
        /// It requires Jellyfin server restart to take effect.
        /// </summary>
        public int JFTAPlaylistsSyncTaskInterval { get; set; }

        /// <summary>
        /// Gets or sets the interval in seconds at which the TubeArchivist to Jellyfin playlists synchronization task should run.
        /// It requires Jellyfin server restart to take effect.
        /// </summary>
        public int TAJFPlaylistsSyncTaskInterval { get; set; }

        /// <summary>
        /// Gets or sets the preferred numbering scheme for episodes (index number) in Jellyfin.
        /// </summary>
        public NumberingScheme EpisodeNumberingScheme { get; set; } = NumberingScheme.Default;

        /// <summary>
        /// Gets the playback progress owners Jellyfin usernames to synchronize data from TubeArchivist.
        /// </summary>
        /// <returns>An array of usernames.</returns>
        public HashSet<string> GetJFUsernamesToArray()
        {
            return _jfUsernamesTo;
        }
    }
}
