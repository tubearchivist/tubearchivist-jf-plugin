using System.Globalization;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// Enum representing the possible actions to execute on a TubeArchivist custom playlist video.
    /// </summary>
    public enum CustomPlaylistAction
    {
        /// <summary>
        /// Create a new custom playlist video.
        /// </summary>
        Create,

        /// <summary>
        /// Removea custom playlist video.
        /// </summary>
        Remove,

        /// <summary>
        /// Move a custom playlist video to the top of the list.
        /// </summary>
        Top,

        /// <summary>
        /// Move a custom playlist video to the bottom of the list.
        /// </summary>
        Bottom,

        /// <summary>
        /// Move a custom playlist video up in the list.
        /// </summary>
        Up,

        /// <summary>
        /// Move a custom playlist video down in the list.
        /// </summary>
        Down
    }

    /// <summary>
    /// A class representing TubeArchivist API custom playlist entry operation request.
    /// </summary>
    public class CustomPlaylistEntryAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomPlaylistEntryAction"/> class.
        /// </summary>
        /// <param name="action">The standard action to create a new entry.</param>
        /// <param name="videoId">The entry YoutubeId.</param>
        public CustomPlaylistEntryAction(
            CustomPlaylistAction action,
            string videoId)
        {
            this.Action = action.ToString().ToLower(CultureInfo.CurrentCulture);
            this.VideoId = videoId;
        }

        /// <summary>
        /// Gets or sets the standard action to create a new entry.
        /// </summary>
        [JsonProperty(PropertyName = "action")]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the entry YoutubeId.
        /// </summary>
        [JsonProperty(PropertyName = "video_id")]
        public string VideoId { get; set; }
    }
}
