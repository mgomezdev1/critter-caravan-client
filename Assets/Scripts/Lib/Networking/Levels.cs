using Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Networking
{
    [JsonConverter(typeof(ILevel))]
    public class LevelConverter : JsonConverter<ILevel>
    {
        public override ILevel? ReadJson(JsonReader reader, Type objectType, ILevel? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            // Parse the current object as a JToken
            JToken value = JToken.ReadFrom(reader);
            if (value is not JObject rawLevel)
                throw new ServerAPIInvalidFormatException<ILevel>(HttpVerb.Get, "unknown endpoint", value.ToString());

            if (rawLevel.ContainsKey("world"))
            {
                Level? levelResult = rawLevel.ToObject<Level>();
                if (levelResult != null && levelResult.WorldData.obstacles != null)
                {
                    return levelResult;
                }
            }

            AsyncLevel? asyncLevelResult = rawLevel.ToObject<AsyncLevel>();
            if (asyncLevelResult != null && asyncLevelResult.LevelId != null)
            {
                return asyncLevelResult;
            }

            throw new ServerAPIInvalidFormatException<ILevel>(HttpVerb.Get, "unknown endpoint", rawLevel.ToString());
        }

        public override void WriteJson(JsonWriter writer, ILevel? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public static partial class ServerAPI
    {
        public static class Levels
        {
            public enum SortCriterion
            {
                CreationTime = 0,
                UpdateTime = 1,
                Likes = 2,
                Completions = 3,
                Name = 4
            }
            public static string SortCriterionToString(SortCriterion criterion)
            {
                return criterion switch
                {
                    SortCriterion.CreationTime => "upload_date",
                    SortCriterion.UpdateTime => "modified_date",
                    SortCriterion.Likes => "likes",
                    SortCriterion.Completions => "completions",
                    SortCriterion.Name => "name",
                    _ => throw new NotImplementedException($"Unknown sort criterion conversion to string {criterion}.")
                };
            }

            public struct QueryParams
            {
                public int PerPage { get; set; }
                public string? Category { get; set; }
                public string? Author { get; set; }
                public bool SortAscending { get; set; }
                public SortCriterion SortCriterion { get; set; }
                public VerificationLevel? MinVerificationLevel { get; set; }
                public VerificationLevel? MaxVerificationLevel { get; set; }

                public override readonly string ToString()
                {
                    List<string> segments = new();

                    if (SortAscending != default) segments.Add("sort_asc=true");
                    if (SortCriterion != default) segments.Add($"sort={SortCriterionToString(SortCriterion)}");
                    if (Author != default) segments.Add($"author={Author}");
                    if (Category != default) segments.Add($"category={Category}");
                    if (PerPage > 0) segments.Add($"per_page={PerPage}");
                    if (MinVerificationLevel.HasValue) segments.Add($"min_verification={(int)MinVerificationLevel.Value}");
                    if (MaxVerificationLevel.HasValue) segments.Add($"max_verification={(int)MaxVerificationLevel.Value}");

                    if (segments.Count == 0)
                    {
                        return "";
                    }
                    return $"?{string.Join('&', segments)}";
                }
            }

            public static async Task<IPaginator<ILevel>> FetchLevels(QueryParams? queryParams = null, CancellationToken cancellationToken = default)
            {
                var qParams = queryParams.HasValue ? queryParams.Value.ToString() : "";
                var endpoint = $"levels{qParams}";
                var pages = await GetAsync<PaginatedLevels>(endpoint, cancellationToken);
                return pages;
            }

            public static readonly int LEVEL_FETCH_REQUEST_SIZE = 50;
            public static async IAsyncEnumerable<LevelCompletionResult> FetchAllCompletions([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await foreach (var result in await InternalCompletionFetch("stats/levels", cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result.MarkDownloaded();
                    yield return result;
                }
            }
            public static async IAsyncEnumerable<LevelCompletionResult> FetchCompletions(IEnumerable<string> levelIds, [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                foreach (var idSet in levelIds.InSetsOf(LEVEL_FETCH_REQUEST_SIZE))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string uri = GetLevelCompletionFetchUri(idSet);
                    await foreach (var result in await InternalCompletionFetch(uri, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        result.MarkDownloaded();
                        yield return result;
                    }
                }
            }

            private static string GetLevelCompletionFetchUri(IEnumerable<string> levelIds)
            {
                return $"stats/levels?{string.Join('&', levelIds.Select(s => $"levelId={s}"))}";
            }
            private static async Task<Paginated<LevelCompletionResult>> InternalCompletionFetch(string uri, CancellationToken cancellationToken = default)
            {
                return await GetAsync<Paginated<LevelCompletionResult>>(uri, cancellationToken);
            }

            public static async Task<LevelCompletionResult> UploadCompletion(LevelCompletionResult completion, CancellationToken cancellationToken = default)
            {
                var result = await PostAsync<LevelCompletionResult>($"stats/levels", completion, cancellationToken);
                result.MarkDownloaded();
                return result;
            }
            public static async Task<IEnumerable<LevelCompletionResult>> UploadCompletions(IEnumerable<LevelCompletionResult> completions, CancellationToken cancellationToken = default)
            {
                var result = await PostAsync<LevelCompletionResult[]>($"stats/levels/bulk", completions.ToArray(), cancellationToken);
                DateTime downloadTime = DateTime.Now;
                foreach (var item in result)
                {
                    item.MarkDownloaded(downloadTime);
                }
                return result;
            }

            public static async Task<Level> UploadLevel(string rawWorldData, string worldName, bool privacy, string? category = null, string? thumbnail = null, CancellationToken cancellationToken = default)
            {
                var level = new LevelForUpload(worldName, rawWorldData, privacy, thumbnail, category);
                return await PostAsync<Level>("levels", level, cancellationToken);
            }
        }
    }
}