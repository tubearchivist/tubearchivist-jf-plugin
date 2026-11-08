using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Providers
{
    /// <summary>
    /// Sets the season of each TubeArchivist episode to the year of the video.
    /// </summary>
    /// <remarks>
    /// Jellyfin parses a season number from the path of each episode before the metadata providers run.
    /// A YouTube ID can hold such a number by chance, for example 00-3.
    /// Jellyfin does not let the season of a remote provider replace a parsed season.
    /// This provider runs after the remote providers and sets the season again.
    /// </remarks>
    public class EpisodeSeasonProvider : ICustomMetadataProvider<Episode>
    {
        /// <inheritdoc />
        public string Name => "TubeArchivist Season";

        /// <inheritdoc />
        public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            // The episode provider sets the production year and the season to the same year.
            if (!item.HasProviderId(Constants.ProviderName)
                || !item.ProductionYear.HasValue
                || item.ParentIndexNumber == item.ProductionYear)
            {
                return Task.FromResult(ItemUpdateType.None);
            }

            item.ParentIndexNumber = item.ProductionYear;
            return Task.FromResult(ItemUpdateType.MetadataEdit);
        }
    }
}
