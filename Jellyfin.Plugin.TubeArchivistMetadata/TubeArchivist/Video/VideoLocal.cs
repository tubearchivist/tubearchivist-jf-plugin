using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.TubeArchivistMetadata.Configuration;
using Jellyfin.Plugin.TubeArchivistMetadata.Utilities;
using MediaBrowser.Controller.Entities.TV;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing local embedded TubeArchivist video data.
    /// </summary>
    public class VideoLocal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoLocal"/> class.
        /// </summary>
        /// <param name="channel">Channel the video belongs to.</param>
        /// <param name="tags">Video tags.</param>
        /// <param name="title">Video title.</param>
        /// <param name="description">Video description.</param>
        /// <param name="published">Video published date.</param>
        /// <param name="vidThumbUrl">Video thumb image URL.</param>
        /// <param name="youtubeId">Video YouTube id.</param>
        /// <param name="player">Player info.</param>
        public VideoLocal(
            ChannelLocal channel,
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
            this.Player = player;
        }

        /// <summary>
        /// Gets or sets the channel info.
        /// </summary>
        [JsonProperty(PropertyName = "channel")]
        public ChannelLocal Channel { get; set; }

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
        /// Gets or sets video published date (deserialized from Unix epoch integer).
        /// </summary>
        [JsonProperty(PropertyName = "published")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
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
        /// Gets the player related info.
        /// </summary>
        [JsonProperty(PropertyName = "player")]
        public Player Player { get; }

        /// <summary>
        /// Converts the local TubeArchivist video to a Jellyfin <see cref="Episode"/> object.
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
                Tags = this.Tags?.ToArray() ?? Array.Empty<string>()
            };
        }
    }
}
