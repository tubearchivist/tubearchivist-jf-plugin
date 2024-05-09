using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing TubeArchivist API ping response data.
    /// </summary>
    public class PingResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PingResponse"/> class.
        /// </summary>
        /// <param name="response">Response from the API ("pong").</param>
        /// <param name="user">TubeArchivist user id.</param>
        /// <param name="version">TubeArchivist version.</param>
        public PingResponse(
            string response,
            int user,
            string version)
        {
            this.Response = response;
            this.User = user;
            this.Version = version;
        }

        /// <summary>
        /// Gets response to the ping ("pong").
        /// </summary>
        [JsonProperty(PropertyName = "response")]
        public string Response { get; }

        /// <summary>
        /// Gets user id.
        /// </summary>
        [JsonProperty(PropertyName = "user")]
        public int User { get; }

        /// <summary>
        /// Gets TubeArchivist version.
        /// </summary>
        [JsonProperty(PropertyName = "version")]
        public string Version { get; }
    }
}
