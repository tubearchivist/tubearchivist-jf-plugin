using System;
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
        /// <param name="watchedUnixTime">Unix time of when the video has been marked watched.</param>
        public Player(long duration, bool isWatched, long watchedUnixTime)
        {
            Duration = duration;
            IsWatched = isWatched;
            WatchedUnixTime = watchedUnixTime;
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
        /// Gets the Unix date and time when the video has been marked as watched.
        /// </summary>
        [JsonProperty(PropertyName = "watched_date")]
        public long WatchedUnixTime { get; }

        /// <summary>
        /// Gets the date and time when the video has been marked as watched.
        /// </summary>
        [JsonIgnore]
        public DateTime WatchedTime
        {
            get => DateTimeOffset.FromUnixTimeSeconds(WatchedUnixTime).DateTime;
        }
    }
}
