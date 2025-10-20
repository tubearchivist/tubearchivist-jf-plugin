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
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeMetadataProvider"/> class.
        /// </summary>
        public EpisodeMetadataProvider()
        {
            if (Plugin.Instance == null)
            {
                throw new DataException("Uninitialized plugin!");
            }
            else
            {
                _logger = Plugin.Instance.Logger;
            }
        }

        /// <summary>
        /// Gets the provider name.
        /// </summary>
        public string Name => "TubeArchivist";

        /// <inheritdoc />
        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Episode>();
            var taApi = TubeArchivistApi.GetInstance();
            var videoTAId = Utils.GetVideoNameFromPath(info.Path);
            var video = await taApi.GetVideo(videoTAId).ConfigureAwait(true);
            _logger.LogDebug("{Message}", string.Format(CultureInfo.CurrentCulture, "Getting metadata for video: {0} ({1})", video?.Title, videoTAId));
            _logger.LogDebug("{Message}", "Received metadata: \n" + JsonConvert.SerializeObject(video));

            if (video != null)
            {
                var peopleInfo = new List<PersonInfo>();
                PeopleHelper.AddPerson(peopleInfo, new PersonInfo
                {
                    Name = video.Channel.Name,
                    ImageUrl = video.Channel.ThumbUrl,
                    Type = Data.Enums.PersonKind.Actor,
                });
                result.HasMetadata = true;
                result.Item = video.ToEpisode();
                result.Provider = Name;
                result.People = peopleInfo;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = new List<RemoteSearchResult>();

            var taApi = TubeArchivistApi.GetInstance();
            var videoTAId = Utils.GetVideoNameFromPath(searchInfo.Path);
            var video = await taApi.GetVideo(videoTAId).ConfigureAwait(true);
            if (video != null)
            {
                results.Add(video.ToSearchResult());
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
                return await Plugin.Instance.HttpClient.GetAsync(new Uri(Utils.SanitizeUrl(Plugin.Instance.Configuration.TubeArchivistUrl + url).TrimEnd('/')), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
