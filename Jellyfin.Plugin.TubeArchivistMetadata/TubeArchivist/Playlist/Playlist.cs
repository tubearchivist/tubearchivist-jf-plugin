using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// Enum representing whether the playlist on TubeArchivist belongs from YouTube or is created by the user.
    /// </summary>
    public enum PlaylistType
    {
        /// <summary>
        /// Playlist belongs from YouTube.
        /// </summary>
        Regular,

        /// <summary>
        /// Playlist created by the user.
        /// </summary>
        Custom
    }

    /// <summary>
    /// A class representing TubeArchivist API playlist data.
    /// </summary>
    public class Playlist
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Playlist"/> class.
        /// </summary>
        /// <param name="isActive">URL of the channel banner image.</param>
        /// <param name="channel">Playlist channel name.</param>
        /// <param name="channelId">Playlist channel YouTube id.</param>
        /// <param name="description">Playlist description.</param>
        /// <param name="entries">Playlist videos.</param>
        /// <param name="id">Playlist YouTube id.</param>
        /// <param name="name">Playlist name.</param>
        /// <param name="thumbnailUrl">URL of the playlist thumbnail.</param>
        /// <param name="type">Type of the playlist. See <see cref="PlaylistType"/>.</param>
        public Playlist(
            bool isActive,
            string channel,
            string channelId,
            string description,
            Collection<PlaylistEntry> entries,
            string id,
            string name,
            string thumbnailUrl, // TODO: Check if settable on Jellyfin
            PlaylistType type)
        {
            this.IsActive = isActive;
            this.Channel = channel;
            this.ChannelId = channelId;
            this.Description = description;
            this.Entries = entries;
            this.Id = id;
            this.Name = name;
            this.ThumbnailUrl = thumbnailUrl;
            this.Type = type;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the playlist is active or not.
        /// </summary>
        [JsonProperty(PropertyName = "playlist_active")]
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the playlist channel name.
        /// </summary>
        [JsonProperty(PropertyName = "playlist_channel")]
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the playlist channel YouTube id.
        /// </summary>
        [JsonProperty(PropertyName = "playlist_channel_id")]
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the playlist description.
        /// </summary>
        [JsonProperty(PropertyName = "playlist_description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets the playlist videos.
        /// </summary>
        [JsonProperty(PropertyName = "playlist_entries")]
        public Collection<PlaylistEntry> Entries { get; }

        /// <summary>
        /// Gets or sets the playlist YouTube id.
        /// </summary>
        [JsonProperty(PropertyName = "playlist_id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the playlist name.
        /// </summary>
        [JsonProperty(PropertyName = "playlist_name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the playlist thumbnail URL.
        /// </summary>
        [JsonProperty(PropertyName = "playlist_thumbnail")]
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// Gets or sets the playlist type.
        /// </summary>
        /// <value>One of the <see cref="PlaylistType"/> values indicating the playlist's type.</value>
        [JsonProperty(PropertyName = "playlist_type")]
        public PlaylistType Type { get; set; }
    }
}
