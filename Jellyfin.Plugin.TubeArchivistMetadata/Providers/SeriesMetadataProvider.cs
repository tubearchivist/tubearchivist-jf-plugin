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
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.Providers
{
    /// <summary>
    /// Metadata provider which interacts with TubeArchivist library.
    /// </summary>
    public class SeriesMetadataProvider : IRemoteMetadataProvider<Series, SeriesInfo>
    {
        /// <summary>
        /// Gets the provider name.
        /// </summary>
        public string Name => "TubeArchivist";

        /// <inheritdoc />
        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();
            var taApi = TubeArchivistApi.GetInstance();
            var channelTAId = info.Path.Split("/").Last();
            var channel = await taApi.GetChannel(channelTAId).ConfigureAwait(true);

            if (channel != null)
            {
                var peopleInfo = new List<PersonInfo>();
                PeopleHelper.AddPerson(peopleInfo, new PersonInfo
                {
                    Name = channel.Name,
                    ImageUrl = channel.ThumbUrl,
                    Type = PersonType.Actor,
                });
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
            var channelTAId = searchInfo.Path.Split("/").Last();
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
                return await Plugin.Instance.HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
