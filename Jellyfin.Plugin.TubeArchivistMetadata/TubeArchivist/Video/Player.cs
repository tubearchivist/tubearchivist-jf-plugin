using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing TubeArchivist API video player data.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class.
        /// </summary>
        /// <param name="duration">Duration of the video.</param>
        /// <param name="isWatched">Whether the video is marked as watched.</param>
        /// <param name="position">Video watched seconds.</param>
        public Player(long duration, bool isWatched, double position)
        {
            Duration = duration;
            IsWatched = isWatched;
            Position = position;
        }

        /// <summary>
        /// Gets the duration of the video.
        /// </summary>
        [JsonProperty(PropertyName = "duration")]
        public long Duration { get; }

        /// <summary>
        /// Gets a value indicating whether the video is marked as watched.
        /// </summary>
        [JsonProperty(PropertyName = "watched")]
        public bool IsWatched { get; }

        /// <summary>
        /// Gets the video watched seconds.
        /// </summary>
        [JsonProperty(PropertyName = "position")]
        public double Position { get; }
    }
}
