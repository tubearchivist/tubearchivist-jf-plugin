using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing TubeArchivist API channel data.
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Channel"/> class.
        /// </summary>
        /// <param name="bannerUrl">URL of the channel banner image.</param>
        /// <param name="description">Channel description.</param>
        /// <param name="id">Channel YouTube id.</param>
        /// <param name="name">Channel name.</param>
        /// <param name="thumbUrl">URL of the channel thumb image.</param>
        /// <param name="tvartUrl">URL of the channel tvart image.</param>
        public Channel(
            string bannerUrl,
            string description,
            string id,
            string name,
            string thumbUrl,
            string tvartUrl)
        {
            this.BannerUrl = bannerUrl;
            this.Description = description;
            this.Id = id;
            this.Name = name;
            this.ThumbUrl = thumbUrl;
            this.TvartUrl = tvartUrl;
        }

        /// <summary>
        /// Gets or sets the URL of the channel banner image.
        /// </summary>
        [JsonProperty(PropertyName = "channel_banner_url")]
        public string BannerUrl { get; set; }

        /// <summary>
        /// Gets or sets the channel description.
        /// </summary>
        [JsonProperty(PropertyName = "channel_description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the channel YouTube id.
        /// </summary>
        [JsonProperty(PropertyName = "channel_id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the channel name.
        /// </summary>
        [JsonProperty(PropertyName = "channel_name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URL of the channel thumb image.
        /// </summary>
        [JsonProperty(PropertyName = "channel_thumb_url")]
        public string ThumbUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL of the channel tvart image.
        /// </summary>
        [JsonProperty(PropertyName = "channel_tvart_url")]
        public string TvartUrl { get; set; }
    }
}
