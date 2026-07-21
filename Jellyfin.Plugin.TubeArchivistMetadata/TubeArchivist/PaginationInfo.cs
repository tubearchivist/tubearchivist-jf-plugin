using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.TubeArchivistMetadata.TubeArchivist
{
    /// <summary>
    /// A class representing pagination information from the TubeArchivist API.
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaginationInfo"/> class.
        /// </summary>
        /// <param name="pageSize">The size of the page.</param>
        /// <param name="pageFrom">The starting page number.</param>
        /// <param name="prevPages">The collection of previous page numbers.</param>
        /// <param name="currentPage">The current page number.</param>
        /// <param name="maxHits">A value indicating whether the max hits have been reached.</param>
        /// <param name="parameters">The parameters used for pagination.</param>
        /// <param name="lastPage">The last page number.</param>
        /// <param name="nextPages">The collection of next page numbers.</param>
        /// <param name="totalHits">The total number of hits.</param>
        public PaginationInfo(
            int pageSize,
            int pageFrom,
            Collection<int> prevPages,
            int currentPage,
            bool maxHits,
            string parameters,
            int lastPage,
            Collection<int> nextPages,
            int totalHits)
        {
            this.PageSize = pageSize;
            this.PageFrom = pageFrom;
            this.PrevPages = prevPages;
            this.CurrentPage = currentPage;
            this.MaxHits = maxHits;
            this.Parameters = parameters;
            this.LastPage = lastPage;
            this.NextPages = nextPages;
            this.TotalHits = totalHits;
        }

        /// <summary>
        /// Gets the page size.
        /// </summary>
        [JsonProperty(PropertyName = "page_size")]
        public int PageSize { get; }

        /// <summary>
        /// Gets the page from.
        /// </summary>
        [JsonProperty(PropertyName = "page_from")]
        public int PageFrom { get; }

        /// <summary>
        /// Gets the previous pages.
        /// </summary>
        [JsonProperty(PropertyName = "prev_pages")]
        public Collection<int> PrevPages { get; }

        /// <summary>
        /// Gets the current page.
        /// </summary>
        [JsonProperty(PropertyName = "current_page")]
        public int CurrentPage { get; }

        /// <summary>
        /// Gets a value indicating whether the max hits have been reached.
        /// </summary>
        [JsonProperty(PropertyName = "max_hits")]
        public bool MaxHits { get; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        [JsonProperty(PropertyName = "params")]
        public string Parameters { get; }

        /// <summary>
        /// Gets the last page.
        /// </summary>
        [JsonProperty(PropertyName = "last_page")]
        public int LastPage { get; }

        /// <summary>
        /// Gets the next pages.
        /// </summary>
        [JsonProperty(PropertyName = "next_pages")]
        public Collection<int> NextPages { get; }

        /// <summary>
        /// Gets the total hits.
        /// </summary>
        [JsonProperty(PropertyName = "total_hits")]
        public int TotalHits { get; }
    }
}
