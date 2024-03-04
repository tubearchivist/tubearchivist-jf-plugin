using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Configuration
{
    /// <summary>
    /// The configuration options.
    /// </summary>
    public enum SomeOptions
    {
        /// <summary>
        /// Option one.
        /// </summary>
        OneOption,

        /// <summary>
        /// Second option.
        /// </summary>
        AnotherOption
    }

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
        }

        /// <summary>
        /// Gets or sets TubeArchivist collection dispay name.
        /// </summary>
        public string CollectionTitle { get; set; }
    }
}
