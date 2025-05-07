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
            Channel? channel = null;

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
                _logger.LogInformation("{Message}", url + ": " + rawData);
                channel = JsonConvert.DeserializeObject<Channel>(rawData);
            }

            return channel;
        }

        /// <summary>
        /// Retrieves the given video information from TubeArchivist.
        /// </summary>
        /// <param name="videoId">YouTube video id.</param>
        /// <returns>A task.</returns>
        public async Task<Video?> GetVideo(string videoId)
        {
            Video? video = null;

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
                _logger.LogInformation("{Message}", rawData);
                video = JsonConvert.DeserializeObject<Video>(rawData);
            }

            return video;
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
        /// <returns>The response <see cref="HttpStatusCode"/>.</returns>
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

            var video = await GetVideo(videoId).ConfigureAwait(true);
            if (video != null)
            {
                progress = new Progress((long)video.Player.Position);
                _logger.LogInformation("{Message}", $"Retrieved progress {video.Player.Position}");
            }

            return progress;
        }

        /// <summary>
        /// Set video/channel/playlist as watched on TubeArchivist.
        /// </summary>
        /// <param name="itemId">Video/channel/playlist id.</param>
        /// <param name="isWatched">Whether the item has been watched or not.</param>
        /// <returns>The response <see cref="HttpStatusCode"/>.</returns>
        public async Task<HttpStatusCode> SetWatchedStatus(string itemId, bool isWatched)
        {
            var watchedEndpoint = $"/api/watched/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance!.Configuration.TubeArchivistUrl + watchedEndpoint));
            var body = JsonConvert.SerializeObject(new Watched(itemId, isWatched));

            var response = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json")).ConfigureAwait(true);

            return response.StatusCode;
        }
    }
}
