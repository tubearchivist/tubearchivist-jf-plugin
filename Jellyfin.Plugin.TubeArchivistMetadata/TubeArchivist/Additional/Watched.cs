using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing TubeArchivist API video watched status.
    /// </summary>
    public class Watched
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Watched"/> class.
        /// </summary>
        /// <param name="id">Id of the video/channel/playlist.</param>
        /// <param name="isWatched">Watched status.</param>
        public Watched(string id, bool isWatched)
        {
            Id = id;
            IsWatched = isWatched;
        }

        /// <summary>
        /// Gets the id of the video/channel/playlist.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; }

        /// <summary>
        /// Gets a value indicating whether the item has been watched or not.
        /// </summary>
        [JsonProperty(PropertyName = "is_watched")]
        public bool IsWatched { get; }
    }
}
