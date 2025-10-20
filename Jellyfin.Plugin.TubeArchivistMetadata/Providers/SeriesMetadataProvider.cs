using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
    public class SeriesMetadataProvider : IRemoteMetadataProvider<Series, SeriesInfo>
    {
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesMetadataProvider"/> class.
        /// </summary>
        public SeriesMetadataProvider()
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
        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();
            var taApi = TubeArchivistApi.GetInstance();
            var channelTAId = Utils.GetChannelNameFromPath(info.Path);
            var channel = await taApi.GetChannel(channelTAId).ConfigureAwait(true);
            _logger.LogDebug("{Message}", string.Format(CultureInfo.CurrentCulture, "Getting metadata for channel: {0} ({1})", channel?.Name, channelTAId));
            _logger.LogDebug("{Message}", "Received metadata: \n" + JsonConvert.SerializeObject(channel));

            if (channel != null)
            {
                var peopleInfo = new List<PersonInfo>();
                result.HasMetadata = true;
                result.Item = channel.ToSeries();
                result.Provider = Name;
                result.People = peopleInfo;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var results = new List<RemoteSearchResult>();

            var taApi = TubeArchivistApi.GetInstance();
            var channelTAId = Utils.GetChannelNameFromPath(searchInfo.Path);
            var channel = await taApi.GetChannel(channelTAId).ConfigureAwait(true);
            if (channel != null)
            {
                results.Add(channel.ToSearchResult());
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
