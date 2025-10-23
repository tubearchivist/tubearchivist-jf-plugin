using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.TubeArchivistMetadata.Configuration;
using Jellyfin.Plugin.TubeArchivistMetadata.Utilities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing TubeArchivist API video data.
    /// </summary>
    public class Video
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Video"/> class.
        /// </summary>
        /// <param name="channel">Channel the video belongs to.</param>
        /// <param name="tags">Video tags.</param>
        /// <param name="title">Video title.</param>
        /// <param name="description">Video description.</param>
        /// <param name="published">Video published date.</param>
        /// <param name="vidThumbUrl">Video thuumb image URL.</param>
        /// <param name="youtubeId">Video YouTube id.</param>
        /// <param name="player">Player info.</param>
        public Video(
            Channel channel,
            Collection<string> tags,
            string title,
            string description,
            DateTime published,
            string vidThumbUrl,
            string youtubeId,
            Player player)
        {
            this.Channel = channel;
            this.Tags = tags;
            this.Title = title;
            this.Description = description;
            this.Published = published;
            this.VidThumbUrl = vidThumbUrl;
            this.YoutubeId = youtubeId;
            Player = player;
        }

        /// <summary>
        /// Gets or sets the channel info.
        /// </summary>
        [JsonProperty(PropertyName = "channel")]
        public Channel Channel { get; set; }

        /// <summary>
        /// Gets video tags.
        /// </summary>
        [JsonProperty(PropertyName = "tags")]
        [JsonConverter(typeof(TagsJsonConverter))]
        public Collection<string> Tags { get; }

        /// <summary>
        /// Gets or sets video title.
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets video description.
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets video published date.
        /// </summary>
        [JsonProperty(PropertyName = "published")]
        public DateTime Published { get; set; }

        /// <summary>
        /// Gets or sets video thumb image URL.
        /// </summary>
        [JsonProperty(PropertyName = "vid_thumb_url")]
        public string VidThumbUrl { get; set; }

        /// <summary>
        /// Gets or sets video YouTube id.
        /// </summary>
        [JsonProperty(PropertyName = "youtube_id")]
        public string YoutubeId { get; set; }

        /// <summary>
        /// Getsthe player related info.
        /// </summary>
        [JsonProperty(PropertyName = "player")]
        public Player Player { get; }

        /// <summary>
        /// Converts the TubeArchivist API video to a Jellyfin <see cref="RemoteSearchResult"/> object.
        /// </summary>
        /// <returns>The video equivalent Jellyfin <see cref="RemoteSearchResult"/> object.</returns>
        public RemoteSearchResult ToSearchResult()
        {
            return new RemoteSearchResult
            {
                Name = Title,
                SearchProviderName = Constants.ProviderName,
                ProductionYear = Published.Year,
                ImageUrl = VidThumbUrl,
                PremiereDate = Published,
                ProviderIds = new Dictionary<string, string>() { { Constants.ProviderName, YoutubeId } }
            };
        }

        /// <summary>
        /// Converts the TubeArchivist API video to a Jellyfin <see cref="Episode"/> object.
        /// </summary>
        /// <returns>The video equivalent Jellyfin <see cref="Episode"/> object.</returns>
        public Episode ToEpisode()
        {
            return new Episode
            {
                Name = Title,
                Overview = Utils.FormatDescription(Description),
                SeasonName = Published.Year.ToString(CultureInfo.CurrentCulture),
                ParentIndexNumber = Published.Year,
                IndexNumber = Plugin.Instance?.Configuration?.EpisodeNumberingScheme switch
                {
                    NumberingScheme.YYYYMMDD => (Published.Year * 10000) + (Published.Month * 100) + Published.Day,
                    _ => null
                },
                SeriesName = Channel.Name,
                ProductionYear = Published.Year,
                PremiereDate = Published,
                Studios = new[] { Channel.Name },
                ProviderIds = new Dictionary<string, string>()
                {
                    {
                        Constants.ProviderName, YoutubeId
                    }
                },
                ImageInfos = new[]
                {
                    new ItemImageInfo
                    {
                        Path = VidThumbUrl,
                        Type = ImageType.Primary
                    }
                },
                Tags = this.Tags.ToArray<string>()
            };
        }
    }
}
