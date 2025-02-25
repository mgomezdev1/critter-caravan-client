using Extensions;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Networking
{
    [Serializable]
    [JsonObject]
    public class Paginated<T> : IAsyncEnumerable<T>
    {
        [JsonProperty("total")]
        public int Total { get; set; }
        [JsonProperty("per_page")]
        public int PerPage { get; set; }
        [JsonProperty("current_page")]
        public int CurrentPage { get; set; }
        [JsonProperty("last_page")]
        public int PageCount { get; set; }
        [JsonProperty("first_page_url")]
        public string FirstPageUrl { get; set; }
        [JsonProperty("last_page_url")]
        public string LastPageUrl { get; set; }
        [JsonProperty("prev_page_url")]
        public string? PrevPageUrl { get; set; } = null;
        [JsonProperty("next_page_url")]
        public string? NextPageUrl { get; set; } = null;
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("from")]
        public int From { get; set; }
        [JsonProperty("to")]
        public int To { get; set; }

        [JsonProperty("data")]
        public T[] Data { get; set; }

        [JsonIgnore]
        public Paginated<T>? NextPage { get; protected set; } = null;

        public Paginated(int total, int perPage, int currentPage, int pageCount, string firstPageUrl, string lastPageUrl, string? prevPageUrl, string? nextPageUrl, string path, int from, int to, T[] data)
        {
            Total = total;
            PerPage = perPage;
            CurrentPage = currentPage;
            PageCount = pageCount;
            FirstPageUrl = firstPageUrl;
            LastPageUrl = lastPageUrl;
            PrevPageUrl = prevPageUrl;
            NextPageUrl = nextPageUrl;
            Path = path;
            From = from;
            To = to;
            Data = data;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (var item in Data ?? Array.Empty<T>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
            if (CurrentPage >= PageCount || NextPageUrl == null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield break;
            }

            NextPage ??= await ServerAPI.GetAsync<Paginated<T>>(NextPageUrl);

            await foreach (var item in NextPage)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }

    public class PaginatedCache<T> : IAsyncEnumerable<T>
    {
        public class PageStore
        {
            public T[] values;
            public DateTime fetchTime;

            public PageStore(T[] values)
            {
                this.values = values;
                this.fetchTime = DateTime.Now;
            }
        }

        private int paginatorPageSize;
        private readonly string path;

        private int total;
        public int Total => total;
        public int PageCount => (total - 1) / externalPageSize + 1;

        public readonly List<PageStore?> pageCache = new();
        private readonly List<string?> pageUrls = new();

        private int externalPageSize = 0;
        public int PageSize { 
            get => externalPageSize;
            set => externalPageSize = value;
        }

        public TimeSpan PageLifetime { get; set; } = TimeSpan.FromSeconds(60);

        public PaginatedCache(Paginated<T> paginated)
        {
            path = paginated.Path;
            LoadPaginator(paginated);
        }
        public PaginatedCache(string path)
        {
            this.path = path;
        }

        public PageStore LoadPaginator(Paginated<T> page)
        {
            int pageNumber = page.CurrentPage;
            if (paginatorPageSize != page.PerPage)
            {
                // invalidate previous cache, page size changed.
                Invalidate();
                paginatorPageSize = page.PerPage;
            }
            PageStore cacheEntry = new(page.Data);
            while (pageCache.Count < pageNumber)
            {
                pageCache.Add(null);
            }
            pageCache[pageNumber - 1] = cacheEntry;
            total = page.Total;
            UpdatePageUrl(page.CurrentPage, page.Path);
            UpdatePageUrl(page.CurrentPage - 1, page.PrevPageUrl);
            UpdatePageUrl(page.CurrentPage + 1, page.NextPageUrl);
            UpdatePageUrl(page.PageCount, page.LastPageUrl);
            UpdatePageUrl(1, page.FirstPageUrl);
            return cacheEntry;
        }
        public void Invalidate()
        {
            pageCache.Clear();
            pageUrls.Clear();
        }
        private void UpdatePageUrl(int pageNumber, string? url)
        {
            if (url != null) return;
            while (pageUrls.Count < pageNumber)
            {
                pageUrls.Add(null);
            }
            pageUrls[pageNumber - 1] = url;
        }

        private int InternalPageCount => (total - 1) / paginatorPageSize + 1;
        private PageStore? TryGetPageFromCache(int pageIndex)
        {
            if (pageIndex >= pageCache.Count) return null;
                
            PageStore? result = pageCache[pageIndex];
            if (result == null) { return null; }

            TimeSpan timeSinceFetch = result.fetchTime.TimeUntilNow();
            if (timeSinceFetch > PageLifetime) return null;
            
            return result;
        }
        private string GeneratePageUrl(int pageIndex)
        {
            if (path.Contains("page="))
            {
                return Regex.Replace(path, @"page=[0-9]+", $"page={pageIndex + 1}");
            }
            string queryParamPrefix = path.Contains("?") ? "&" : "?";
            return path + $"{queryParamPrefix}page={pageIndex + 1}";
        }
        private string GetPageUrl(int pageIndex)
        {
            if (pageUrls.Count > pageIndex)
            {
                var cacheResult = pageUrls[pageIndex];
                if (cacheResult != null) return cacheResult;
            }
            string url = GeneratePageUrl(pageIndex);
            return url;
        }
        private async Task<PageStore> GetPage(int pageIndex, CancellationToken cancellationToken = default)
        {
            if (pageIndex >= total / InternalPageCount)
            {
                throw new IndexOutOfRangeException($"The internal pageIndex provided ({pageIndex}) must be below the internal number of pages in the paginator ({InternalPageCount})");
            }
            PageStore? cacheResult = TryGetPageFromCache(pageIndex);
            if (cacheResult != null) return cacheResult;
            string? pageUrl = GetPageUrl(pageIndex);
            var pageData = await ServerAPI.GetAsync<Paginated<T>>(pageUrl, cancellationToken);
            return LoadPaginator(pageData);
        }
        public async IAsyncEnumerable<T> FetchPage(int pageIndex, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            int startIndex = pageIndex * externalPageSize;
            for (int i = startIndex; i < (pageIndex + 1) * externalPageSize; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return await FetchByIndex(i, cancellationToken);
            }
        }
        public async Task<T> FetchByIndex(int index, CancellationToken cancellationToken = default)
        {
            int pageIndex = index / paginatorPageSize;
            PageStore store = await GetPage(pageIndex, cancellationToken);
            int inStoreIndex = index - pageIndex * paginatorPageSize;
            return store.values[inStoreIndex];
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            int i = 0;
            while (i < total)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return await FetchByIndex(i++);
            }
        }
    }
}