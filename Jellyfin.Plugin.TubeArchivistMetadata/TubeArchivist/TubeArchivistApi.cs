using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeArchivistMetadata.Utilities;
using MediaBrowser.Model.Playlists;
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
                _logger.LogDebug("{Message}", "Received redirect to: " + url);
                response = await client.GetAsync(url).ConfigureAwait(true);
            }

            _logger.LogDebug("{Message}", url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
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
                _logger.LogDebug("{Message}", "Received redirect to: " + url);
                response = await client.GetAsync(url).ConfigureAwait(true);
            }

            _logger.LogDebug("{Message}", url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                _logger.LogDebug("{Message}", rawData);
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
                _logger.LogDebug("{Message}", "Received redirect to: " + url);
                response = await client.GetAsync(url).ConfigureAwait(true);
            }

            _logger.LogDebug("{Message}", url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                pong = JsonConvert.DeserializeObject<PingResponse>(rawData);
            }

            return pong;
        }

        /// <summary>
        /// Sends video playback progress to TubeArchivist.
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
        /// Sends video playback progress to TubeArchivist.
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
                _logger.LogDebug("{Message}", $"Retrieved progress {video.Player.Position}");
            }

            return progress;
        }

        /// <summary>
        /// Sets video/channel/playlist as watched on TubeArchivist.
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

        /// <summary>
        /// Retrieves the playlists from TubeArchivist.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task<ISet<Playlist>?> GetPlaylists()
        {
            ResponseContainer<ISet<Playlist>?>? playlists = null;

            var playlistsEndpoint = "/api/playlist/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance?.Configuration.TubeArchivistUrl + playlistsEndpoint));
            var response = await client.GetAsync(url).ConfigureAwait(true);
            while (response.StatusCode == HttpStatusCode.Moved)
            {
                url = response.Headers.Location;
                _logger.LogInformation("{Message}", "Received redirect to: " + url);
                response = await client.GetAsync(Utils.SanitizeUrl(Plugin.Instance?.Configuration.TubeArchivistUrl + url)).ConfigureAwait(true);
            }

            _logger.LogInformation("{Message}", url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                playlists = JsonConvert.DeserializeObject<ResponseContainer<ISet<Playlist>?>>(rawData);
                if (playlists?.Paginate != null)
                {
                    var lastPage = playlists.Paginate.LastPage;
                    _logger.LogInformation("Pagination info: Current page {CurrentPage} / Last page {LastPage}, Total hits: {TotalHits}", playlists.Paginate.CurrentPage, playlists.Paginate.LastPage, playlists.Paginate.TotalHits);

                    while (playlists.Paginate.CurrentPage < lastPage)
                    {
                        var nextPage = playlists.Paginate.CurrentPage + 1;
                        var pagedUrl = new Uri(Utils.SanitizeUrl(Plugin.Instance?.Configuration.TubeArchivistUrl + playlistsEndpoint + "?page=" + nextPage));
                        response = await client.GetAsync(pagedUrl).ConfigureAwait(true);
                        while (response.StatusCode == HttpStatusCode.Moved)
                        {
                            url = response.Headers.Location;
                            _logger.LogInformation("{Message}", "Received redirect to: " + url);
                            response = await client.GetAsync(Utils.SanitizeUrl(Plugin.Instance?.Configuration.TubeArchivistUrl + url)).ConfigureAwait(true);
                        }

                        _logger.LogInformation("{Message}", pagedUrl + ": " + response.StatusCode);

                        if (response.IsSuccessStatusCode)
                        {
                            rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                            var nextPagePlaylists = JsonConvert.DeserializeObject<ResponseContainer<ISet<Playlist>?>>(rawData);
                            if (nextPagePlaylists?.Data != null)
                            {
                                foreach (var playlist in nextPagePlaylists.Data)
                                {
                                    playlists.Data?.Add(playlist);
                                }
                            }

                            if (nextPagePlaylists?.Paginate != null)
                            {
                                playlists.Paginate = nextPagePlaylists.Paginate;
                                _logger.LogInformation("Pagination info: Current page {CurrentPage} / Last page {LastPage}, Total hits: {TotalHits}", playlists.Paginate.CurrentPage, playlists.Paginate.LastPage, playlists.Paginate.TotalHits);
                            }
                        }
                        else
                        {
                            _logger.LogCritical("Failed to retrieve page {PageNumber} of playlists during pagination.", nextPage);
                            break;
                        }
                    }
                }
            }

            return playlists?.Data;
        }

        /// <summary>
        /// Creates a new custom playlist.
        /// </summary>
        /// <param name="creationRequest">Playlist creation request.</param>
        /// <returns>The created <see cref="Playlist"/>.</returns>
        public async Task<Playlist?> CreateCustomPlaylist(CustomPlaylistCreation creationRequest)
        {
            Playlist? playlist = null;
            var customPlaylistEndpoint = $"/api/playlist/custom/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance!.Configuration.TubeArchivistUrl + customPlaylistEndpoint));
            var body = JsonConvert.SerializeObject(creationRequest);
            _logger.LogInformation("{Message}", body);

            var response = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json")).ConfigureAwait(true);
            _logger.LogInformation("POST {Message}", url + ": " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                string rawData = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
                playlist = JsonConvert.DeserializeObject<Playlist>(rawData);
            }

            return playlist;
        }

        /// <summary>
        /// Creates a new custom playlist.
        /// </summary>
        /// <param name="playlistId">Playlist id.</param>
        /// <param name="entryAction">Playlist creation request.</param>
        /// <returns>The response <see cref="HttpStatusCode"/>.</returns>
        public async Task<HttpStatusCode> CustomPlaylistEntryAction(string playlistId, CustomPlaylistEntryAction entryAction)
        {
            var customPlaylistEndpoint = $"/api/playlist/custom/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance!.Configuration.TubeArchivistUrl + customPlaylistEndpoint + playlistId));
            var body = JsonConvert.SerializeObject(entryAction);
            _logger.LogDebug("CustomPlaylistEntryAction body: {Message}", body);

            var response = await client.PostAsync(url, new StringContent(body, Encoding.UTF8, "application/json")).ConfigureAwait(true);
            _logger.LogDebug("Response code: {Message}", response.StatusCode);

            return response.StatusCode;
        }

        /// <summary>
        /// Deletes a playlist.
        /// </summary>
        /// <param name="playlistId">Playlist id.</param>
        /// <returns>Whether the playlist has been deleted successfully or not.</returns>
        public async Task<bool> DeletePlaylist(string playlistId)
        {
            var deletePlaylistEndpoint = $"/api/playlist/";
            var url = new Uri(Utils.SanitizeUrl(Plugin.Instance!.Configuration.TubeArchivistUrl + deletePlaylistEndpoint + playlistId));
            var response = await client.DeleteAsync(url).ConfigureAwait(true);
            _logger.LogDebug("Response code: {Message}", response.StatusCode);

            return response.StatusCode == HttpStatusCode.NoContent;
        }
    }
}
