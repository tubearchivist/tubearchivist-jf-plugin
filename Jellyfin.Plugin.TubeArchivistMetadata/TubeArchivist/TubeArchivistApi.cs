using System;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeArchivistMetadata.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// Class to interact with TubeArchivist API.
    /// </summary>
    public class TubeArchivistApi
    {
        private ILogger _logger;
        private HttpClient client;
        private static TubeArchivistApi _taApiInstance = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="TubeArchivistApi"/> class.
        /// </summary>
        /// <param name="httpClient">HTTP client to make requests to TubeArchivist API.</param>
        private TubeArchivistApi(HttpClient httpClient)
        {
            if (Plugin.Instance == null)
            {
                throw new DataException("Uninitialized plugin!");
            }
            else
            {
                _logger = Plugin.Instance.Logger;
            }

            client = httpClient;
        }

        /// <summary>
        /// Gets the instance of the <see cref="TubeArchivistApi"/> class.
        /// </summary>
        /// <returns>The TubeArchivistApi instance.</returns>
        public static TubeArchivistApi GetInstance()
        {
            if (_taApiInstance == null)
            {
                if (Plugin.Instance != null)
                {
                    _taApiInstance = new TubeArchivistApi(Plugin.Instance.HttpClient);
                }
                else
                {
                    throw new DataException("Uninitialized plugin!");
                }
            }

            return _taApiInstance;
        }

        /// <summary>
        /// Retrieves the given channel information from TubeArchivist.
        /// </summary>
        /// <param name="channelId">YouTube channel id.</param>
        /// <returns>A task.</returns>
        public async Task<Channel?> GetChannel(string channelId)
        {
            ResponseContainer<Channel>? channel = null;

            var channelsEndpoint = "/api/channel/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance?.Configuration.TubeArchivistUrl + channelsEndpoint + channelId));
            var response = await client.GetAsync(url).ConfigureAwait(true);
            while (response.StatusCode == HttpStatusCode.Moved)
            {
                _logger.LogInformation("{Message}", "Received redirect to: " + response.Headers.Location);
                response = await client.GetAsync(response.Headers.Location).ConfigureAwait(true);
            }

            _logger.LogInformation("{Message}", url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                channel = JsonConvert.DeserializeObject<ResponseContainer<Channel>>(rawData);
            }

            return channel?.Data;
        }

        /// <summary>
        /// Retrieves the given video information from TubeArchivist.
        /// </summary>
        /// <param name="videoId">YouTube video id.</param>
        /// <returns>A task.</returns>
        public async Task<Video?> GetVideo(string videoId)
        {
            ResponseContainer<Video>? video = null;

            var videosEndpoint = "/api/video/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance?.Configuration.TubeArchivistUrl + videosEndpoint + videoId));
            var response = await client.GetAsync(url).ConfigureAwait(true);
            while (response.StatusCode == HttpStatusCode.Moved)
            {
                _logger.LogInformation("{Message}", "Received redirect to: " + response.Headers.Location);
                response = await client.GetAsync(response.Headers.Location).ConfigureAwait(true);
            }

            _logger.LogInformation("{Message}", url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                video = JsonConvert.DeserializeObject<ResponseContainer<Video>>(rawData);
            }

            return video?.Data;
        }
    }
}
