using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
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
        /// <param name="tags">Channel tags.</param>
        /// <param name="thumbUrl">URL of the channel thumb image.</param>
        /// <param name="tvartUrl">URL of the channel tvart image.</param>
        public Channel(
            string bannerUrl,
            string description,
            string id,
            string name,
            Collection<string> tags,
            string thumbUrl,
            string tvartUrl)
        {
            this.BannerUrl = bannerUrl;
            this.Description = description;
            this.Id = id;
            this.Name = name;
            this.Tags = tags;
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
        /// Gets channel tags.
        /// </summary>
        [JsonProperty(PropertyName = "channel_tags")]
        [JsonConverter(typeof(TagsJsonConverter))]
        public Collection<string> Tags { get; }

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

        /// <summary>
        /// Converts the TubeArchivist API channel to a Jellyfin <see cref="RemoteSearchResult"/> object.
        /// </summary>
        /// <returns>The channel equivalent Jellyfin <see cref="RemoteSearchResult"/> object.</returns>
        public RemoteSearchResult ToSearchResult()
        {
            return new RemoteSearchResult
            {
                Name = Name,
                SearchProviderName = Constants.ProviderName,
                ImageUrl = ThumbUrl,
                ProviderIds = new Dictionary<string, string>() { { Constants.ProviderName, Id } }
            };
        }

        /// <summary>
        /// Converts the TubeArchivist API channel to a Jellyfin <see cref="Series"/> object.
        /// </summary>
        /// <returns>The channel equivalent Jellyfin <see cref="Series"/> object.</returns>
        public Series ToSeries()
        {
            return new Series
            {
                Name = Name,
                Studios = new[] { Name },
                ProviderIds = new Dictionary<string, string>()
                {
                    {
                        Constants.ProviderName, Id
                    }
                },
                ImageInfos = new[]
                {
                    new ItemImageInfo
                    {
                        Path = ThumbUrl,
                        Type = ImageType.Primary
                    }
                },
                Tags = this.Tags.ToArray<string>()
            };
        }
    }
}
