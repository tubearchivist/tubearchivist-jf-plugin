using System;
using System.Collections.Generic;
using System.Data;
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

namespace Jellyfin.Plugin.TubeArchivistMetadata.Providers
{
    /// <summary>
    /// Image provider which interacts with TubeArchivist library.
    /// </summary>
    public class SeriesImageProvider : IRemoteImageProvider
    {
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
                Console.WriteLine("Channel images: " + Plugin.Instance.Configuration.TubeArchivistUrl + url);
                return await Plugin.Instance.HttpClient.GetAsync(new Uri(Plugin.Instance.Configuration.TubeArchivistUrl + url), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
