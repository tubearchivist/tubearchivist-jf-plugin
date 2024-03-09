namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// Class representing the base common structure of TubeArchivist API responses.
    /// </summary>
    /// <typeparam name="T">The contained data type.</typeparam>
    public class ResponseContainer<T>
    {
        /// <summary>
        /// Gets or sets the contained data object.
        /// </summary>
        public T? Data { get; set; }
    }
}
