using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing TubeArchivist API custom playlist creation request.
    /// </summary>
    public class CustomPlaylistCreation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomPlaylistCreation"/> class.
        /// </summary>
        /// <param name="playlistName">The name of the new custom playlist to create.</param>
        public CustomPlaylistCreation(string playlistName)
        {
            this.PlaylistName = playlistName;
        }

        /// <summary>
        /// Gets or sets the name of the new custom playlist to create.
        /// </summary>
        [JsonProperty(PropertyName = "playlist_name")]
        public string PlaylistName { get; set; }
    }
}
