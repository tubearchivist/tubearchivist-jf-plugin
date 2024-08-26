using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing TubeArchivist API video progress data.
    /// </summary>
    public class Progress
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Progress"/> class.
        /// </summary>
        /// <param name="position">Playback progress in seconds.</param>
        /// <param name="youtubeId">Video YouTube id.</param>
        /// <param name="userId">Playback progress user id.</param>
        public Progress(
            long position,
            string? youtubeId,
            long? userId)
        {
            YoutubeId = youtubeId;
            UserId = userId;
            Position = position;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Progress"/> class.
        /// </summary>
        /// <param name="position">Playback progress in seconds.</param>
        public Progress(long position)
        {
            Position = position;
        }

        /// <summary>
        /// Gets or sets the video YouTube id.
        /// </summary>
        [JsonProperty(PropertyName = "youtube_id")]
        public string? YoutubeId { get; set; }

        /// <summary>
        /// Gets or sets playback progress user id.
        /// </summary>
        [JsonProperty(PropertyName = "user_id")]
        public long? UserId { get; set; }

        /// <summary>
        /// Gets or sets video playback progress (in seconds).
        /// </summary>
        [JsonProperty(PropertyName = "position")]
        public long Position { get; set; }
    }
}
