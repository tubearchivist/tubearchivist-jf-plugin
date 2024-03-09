using System;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// Class to interact with TubeArchivist API.
    /// </summary>
    public class TubeArchivistApi
    {
        private HttpClient client;
        private static TubeArchivistApi _taApiInstance = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="TubeArchivistApi"/> class.
        /// </summary>
        /// <param name="httpClient">HTTP client to make requests to TubeArchivist API.</param>
        private TubeArchivistApi(HttpClient httpClient)
        {
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
            Console.WriteLine(Plugin.Instance?.Configuration.TubeArchivistUrl);
            Console.WriteLine(Plugin.Instance?.Configuration.TubeArchivistApiKey);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", Plugin.Instance?.Configuration.TubeArchivistApiKey);

            var channelsEndpoint = "/api/channel/";
            var url = new Uri(Plugin.Instance?.Configuration.TubeArchivistUrl + channelsEndpoint + channelId);
            var response = await client.GetAsync(url).ConfigureAwait(true);
            Console.WriteLine(response.StatusCode);
            while (response.StatusCode == HttpStatusCode.Moved)
            {
                Console.WriteLine("Received redirect to: " + response.Headers.Location);
                response = await client.GetAsync(response.Headers.Location).ConfigureAwait(true);
            }

            Console.WriteLine(url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                channel = JsonConvert.DeserializeObject<ResponseContainer<Channel>>(rawData);

                Console.WriteLine("Channel null? " + channel == null);
                Console.WriteLine("Channel: " + channel?.Data?.Name);
                if (channel != null && channel.Data != null)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(channel));
                }
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
            Console.WriteLine(Plugin.Instance?.Configuration.TubeArchivistUrl);
            Console.WriteLine(Plugin.Instance?.Configuration.TubeArchivistApiKey);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", Plugin.Instance?.Configuration.TubeArchivistApiKey);

            var videosEndpoint = "/api/video/";
            var url = new Uri(Plugin.Instance?.Configuration.TubeArchivistUrl + videosEndpoint + videoId);
            var response = await client.GetAsync(url).ConfigureAwait(true);
            Console.WriteLine(response.StatusCode);
            while (response.StatusCode == HttpStatusCode.Moved)
            {
                Console.WriteLine("Received redirect to: " + response.Headers.Location);
                response = await client.GetAsync(response.Headers.Location).ConfigureAwait(true);
            }

            Console.WriteLine(url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                video = JsonConvert.DeserializeObject<ResponseContainer<Video>>(rawData);
            }

            return video?.Data;
        }
    }
}
