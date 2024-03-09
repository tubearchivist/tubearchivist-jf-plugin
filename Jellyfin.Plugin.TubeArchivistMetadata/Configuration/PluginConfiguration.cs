using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Configuration
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            CollectionTitle = "YouTube";
            TubeArchivistUrl = string.Empty;
            TubeArchivistApiKey = string.Empty;
        }

        /// <summary>
        /// Gets or sets TubeArchivist collection display name.
        /// </summary>
        public string CollectionTitle { get; set; }

        /// <summary>
        /// Gets or sets TubeArchivist URL.
        /// </summary>
        public string TubeArchivistUrl { get; set; }

        /// <summary>
        /// Gets or sets TubeArchivist API key.
        /// </summary>
        public string TubeArchivistApiKey { get; set; }
    }
}
