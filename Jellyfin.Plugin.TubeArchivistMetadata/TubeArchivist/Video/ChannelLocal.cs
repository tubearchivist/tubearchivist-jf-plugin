using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing local embedded TubeArchivist channel data.
    /// </summary>
    public class ChannelLocal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelLocal"/> class.
        /// </summary>
        /// <param name="name">Channel name.</param>
        [JsonConstructor]
        public ChannelLocal(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets or sets the channel name.
        /// </summary>
        [JsonProperty(PropertyName = "channel_name")]
        public string Name { get; set; }
    }
}
