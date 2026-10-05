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
    /// Image provider which interacts with TubeArchivist library.
    /// </summary>
    public class EpisodeImageProvider : IRemoteImageProvider
    {
        private readonly ILogger _logger;
        private readonly TubeArchivistApi _taApi;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeImageProvider"/> class.
        /// </summary>
        public EpisodeImageProvider()
            : this(
                TubeArchivistApi.GetInstance(),
                Plugin.Instance?.Logger ?? throw new DataException("Uninitialized plugin!"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeImageProvider"/> class.
        /// </summary>
        /// <param name="taApi">TubeArchivist API client.</param>
        /// <param name="logger">Logger used for provider diagnostics.</param>
        internal EpisodeImageProvider(TubeArchivistApi taApi, ILogger logger)
        {
            _taApi = taApi ?? throw new ArgumentNullException(nameof(taApi));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the provider name.
        /// </summary>
        public string Name => "TubeArchivist";

        /// <inheritdoc />
        public bool Supports(BaseItem item) => item is Episode;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var list = new List<RemoteImageInfo>();
            var videoTAId = Utils.GetVideoNameFromPath(item.Path);
            var video = await _taApi.GetVideo(videoTAId).ConfigureAwait(true);
            _logger.LogDebug("{Message}", string.Format(CultureInfo.CurrentCulture, "Getting images for video: {0} ({1})", video?.Title, videoTAId));
            _logger.LogDebug("{Message}", "Thumb URI: " + video?.VidThumbUrl);

            if (video != null)
            {
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = _taApi.BuildImageUrl(video.VidThumbUrl).AbsoluteUri
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
                return await Plugin.Instance.HttpClient.GetAsync(_taApi.BuildImageUrl(url), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
