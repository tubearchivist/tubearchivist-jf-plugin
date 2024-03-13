using System;
using System.Data;
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

            CollectionTitle = "YouTube";
            _tubeArchivistUrl = string.Empty;
            TubeArchivistApiKey = string.Empty;
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
            }
        }

        /// <summary>
        /// Gets or sets TubeArchivist API key.
        /// </summary>
        public string TubeArchivistApiKey { get; set; }
    }
}
