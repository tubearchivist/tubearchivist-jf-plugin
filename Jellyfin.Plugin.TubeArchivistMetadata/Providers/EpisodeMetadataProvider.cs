using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist;
using Jellyfin.Plugin.TubeArchivistMetadata.Utilities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Providers
{
    /// <summary>
    /// Metadata provider which interacts with TubeArchivist library.
    /// </summary>
    public class EpisodeMetadataProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        private readonly ILogger _logger;
        private readonly TubeArchivistApi _taApi;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeMetadataProvider"/> class.
        /// </summary>
        public EpisodeMetadataProvider()
            : this(
                TubeArchivistApi.GetInstance(),
                Plugin.Instance?.Logger ?? throw new DataException("Uninitialized plugin!"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeMetadataProvider"/> class.
        /// </summary>
        /// <param name="taApi">TubeArchivist API client.</param>
        /// <param name="logger">Logger used for provider diagnostics.</param>
        internal EpisodeMetadataProvider(TubeArchivistApi taApi, ILogger logger)
        {
            _taApi = taApi ?? throw new ArgumentNullException(nameof(taApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the provider name.
        /// </summary>
        public string Name => "TubeArchivist";

        /// <inheritdoc />
        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>();
            var videoTAId = Utils.GetVideoNameFromPath(info.Path);
            var video = await _taApi.GetVideo(videoTAId).ConfigureAwait(true);
            _logger.LogDebug("{Message}", string.Format(CultureInfo.CurrentCulture, "Getting metadata for video: {0} ({1})", video?.Title, videoTAId));
            _logger.LogDebug("{Message}", "Received metadata: \n" + JsonConvert.SerializeObject(video));

            if (video != null)
            {
                var peopleInfo = new List<PersonInfo>();
                PeopleHelper.AddPerson(peopleInfo, new PersonInfo
                {
                    Name = video.Channel.Name,
                    ImageUrl = _taApi.BuildImageUrl(video.Channel.ThumbUrl).AbsoluteUri,
                    Type = Data.Enums.PersonKind.Actor,
                });
                result.HasMetadata = true;
                result.Item = video.ToEpisode();
                result.Item.Path = info.Path;
                result.Provider = Name;
                result.People = peopleInfo;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = new List<RemoteSearchResult>();

            var videoTAId = Utils.GetVideoNameFromPath(searchInfo.Path);
            var video = await _taApi.GetVideo(videoTAId).ConfigureAwait(true);
            if (video != null)
            {
                var searchResult = video.ToSearchResult();
                searchResult.ImageUrl = _taApi.BuildImageUrl(video.VidThumbUrl).AbsoluteUri;
                results.Add(searchResult);
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            if (Plugin.Instance == null)
            {
                throw new DataException("Uninitialized plugin!");
            }
            else
            {
                return await Plugin.Instance.HttpClient.GetAsync(_taApi.BuildImageUrl(url), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
