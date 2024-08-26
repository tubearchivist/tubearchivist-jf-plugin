using System;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Text;
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
                url = response.Headers.Location;
                _logger.LogInformation("{Message}", "Received redirect to: " + url);
                response = await client.GetAsync(url).ConfigureAwait(true);
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
                url = response.Headers.Location;
                _logger.LogInformation("{Message}", "Received redirect to: " + url);
                response = await client.GetAsync(url).ConfigureAwait(true);
            }

            _logger.LogInformation("{Message}", url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                video = JsonConvert.DeserializeObject<ResponseContainer<Video>>(rawData);
            }

            return video?.Data;
        }

        /// <summary>
        /// Validates connection and authentication to TubeArchivist.
        /// </summary>
        /// <returns>The ping response.</returns>
        public async Task<PingResponse?> Ping()
        {
            PingResponse? pong = null;

            var pingEndpoint = "/api/ping/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance!.Configuration.TubeArchivistUrl + pingEndpoint));
            var response = await client.GetAsync(url).ConfigureAwait(true);

            while (response.StatusCode == HttpStatusCode.Moved)
            {
                url = response.Headers.Location;
                _logger.LogInformation("{Message}", "Received redirect to: " + url);
                response = await client.GetAsync(url).ConfigureAwait(true);
            }

            _logger.LogInformation("{Message}", url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                pong = JsonConvert.DeserializeObject<PingResponse>(rawData);
            }

            return pong;
        }

        /// <summary>
        /// Send video playback progress to TubeArchivist.
        /// </summary>
        /// <param name="videoId">Video id.</param>
        /// <param name="progress">Progress in seconds.</param>
        /// <returns>Nothing if successful.</returns>
        public async Task<HttpStatusCode> SetProgress(string videoId, long progress)
        {
            var progressEndpoint = $"/api/video/{videoId}/progress/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance!.Configuration.TubeArchivistUrl + progressEndpoint));
            var body = JsonConvert.SerializeObject(new Progress(progress));

            var response = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json")).ConfigureAwait(true);

            return response.StatusCode;
        }

        /// <summary>
        /// Send video playback progress to TubeArchivist.
        /// </summary>
        /// <param name="videoId">Video id.</param>
        /// <returns>Video playback progress data.</returns>
        public async Task<Progress?> GetProgress(string videoId)
        {
            Progress? progress = null;

            var progressEndpoint = $"/api/video/{videoId}/progress/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance!.Configuration.TubeArchivistUrl + progressEndpoint));

            var response = await client.GetAsync(url).ConfigureAwait(true);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                progress = JsonConvert.DeserializeObject<Progress>(rawData);
            }

            return progress;
        }
    }
}
