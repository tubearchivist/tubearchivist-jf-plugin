using ICU4N.Text;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing TubeArchivist API playlist entries data.
    /// </summary>
    public class PlaylistEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistEntry"/> class.
        /// </summary>
        /// <param name="youtubeId">Video YouTube id.</param>
        /// <param name="title">Video title.</param>
        /// <param name="uploader">Video uploader.</param>
        /// <param name="index">Video index in the playlist.</param>
        /// <param name="isDownloaded">A value indicating whether the video has been already downloaded or not.</param>
        public PlaylistEntry(
            string youtubeId,
            string title,
            string uploader,
            int index,
            bool isDownloaded)
        {
            this.YoutubeId = youtubeId;
            this.Title = title;
            this.Uploader = uploader;
            this.Index = index;
            this.IsDownloaded = isDownloaded;
        }

        /// <summary>
        /// Gets or sets the video YouTube id.
        /// </summary>
        [JsonProperty(PropertyName = "youtube_id")]
        public string YoutubeId { get; set; }

        /// <summary>
        /// Gets or sets the video title.
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the video uploader.
        /// </summary>
        [JsonProperty(PropertyName = "uploader")]
        public string Uploader { get; set; }

        /// <summary>
        /// Gets or sets the video index in the playlist.
        /// </summary>
        [JsonProperty(PropertyName = "idx")]
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the video has been already downloaded or not.
        /// </summary>
        [JsonProperty(PropertyName = "downloaded")]
        public bool IsDownloaded { get; set; }
    }
}
