using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Providers
{
    /// <summary>
    /// Image provider which interacts with TubeArchivist library.
    /// </summary>
    public class SeriesImageProvider : IRemoteImageProvider
    {
        private ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesImageProvider"/> class.
        /// </summary>
        public SeriesImageProvider()
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
        public bool Supports(BaseItem item) => item is Series;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();
            var taApi = TubeArchivistApi.GetInstance();
            var channelTAId = item.Path.Split("/").Last();
            var channel = await taApi.GetChannel(channelTAId).ConfigureAwait(true);
            _logger.LogInformation("{Message}", string.Format(CultureInfo.CurrentCulture, "Getting images for channel: {0} ({1})", channel?.Name, channelTAId));
            _logger.LogInformation("{Message}", "Thumb URI: " + channel?.ThumbUrl);
            _logger.LogInformation("{Message}", "TVArt URI: " + channel?.TvartUrl);
            _logger.LogInformation("{Message}", "Banner URI: " + channel?.BannerUrl);

            if (channel != null)
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = channel.ThumbUrl
                });
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Art,
                    Url = channel.TvartUrl
                });
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Banner,
                    Url = channel.BannerUrl
                });
            }

            return list;
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
                return await Plugin.Instance.HttpClient.GetAsync(new Uri(Plugin.Instance.Configuration.TubeArchivistUrl + url), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
