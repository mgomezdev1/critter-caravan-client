using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#nullable enable
namespace Networking
{
    public interface IPayload<T>
    {
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class SessionResponse : IPayload<SessionResponse>
    {
        [JsonProperty(propertyName: "user")]
        public User User { get; set; }
        [JsonProperty(propertyName: "token")]
        public string Token { get; set; }
        [JsonProperty(propertyName: "expires")]
        public DateTime ExpirationDate { get; set; }

        public SessionResponse(User user, string token, DateTime expirationDate)
        {
            User = user;
            Token = token;
            ExpirationDate = expirationDate;
        }
    }

    public class ClientLevel : IPayload<ClientLevel>
    {
        [JsonProperty("levelId")]
        public virtual string? MaybeLevelId { get; set; } = null;
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("private")]
        public bool Privacy { get; set; }
        [JsonProperty("thumbnail")]
        public string? Thumbnail { get; set; }
        [JsonProperty("category")]
        public string? Category { get; set; }
        // This property is managed by a special serializer
        [JsonProperty("world")]
        public WorldSaveData WorldData { get; set; }

        public ClientLevel(string name, bool privacy, string? thumbnail, string? category, WorldSaveData worldData, string? levelId = null)
        {
            MaybeLevelId = levelId;
            Name = name;
            Privacy = privacy;
            Thumbnail = thumbnail;
            Category = category;
            WorldData = worldData;
        }
    }

    public enum VerificationLevel
    {
        Unverified = 0,
        Verified = 1,
        Ranked = 2,
        Spotlight = 3,
        Official = 4
    }

    public interface ILevel
    {
        [JsonIgnore]
        public string LevelId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("private")]
        public bool Privacy { get; set; }
        [JsonProperty("category")]
        public string? Category { get; set; }
        [JsonProperty("authorId")]
        public string AuthorId { get; set; }
        [JsonProperty("uploadDate")]
        public DateTime Created { get; set; }
        [JsonProperty("modifiedDate")]
        public DateTime Updated { get; set; }
        [JsonProperty("reviews")]
        public ReviewData ReviewData { get; set; }
        [JsonProperty("verificationLevel")]
        public VerificationLevel VerificationLevel { get; set; }

        public Task<WorldSaveData> FetchWorldData(CancellationToken cancellationToken = default);
        public Task<Sprite> GetThumbnail(CancellationToken cancellationToken = default);

        public async Task<WorldSaveData> RefreshWorldData(CancellationToken cancellationToken = default)
        {
            string levelId = LevelId;
            var refreshedData = await ServerAPI.GetAsync<Level>($"levels/{levelId}", cancellationToken);
            Name = refreshedData.Name;
            Privacy = refreshedData.Privacy;
            Category = refreshedData.Category;
            AuthorId = refreshedData.AuthorId;
            Created = refreshedData.Created;
            Updated = refreshedData.Updated;
            ReviewData = refreshedData.ReviewData;

            return refreshedData.WorldData;
        }
    }

    [JsonObject]
    public class Level : ClientLevel, IPayload<ClientLevel>, ILevel
    {
        public Level(string levelId, string name, bool privacy, string? thumbnail, string? category, WorldSaveData worldData, string authorId, DateTime created, DateTime updated, ReviewData reviewData, VerificationLevel verificationLevel)
            : base(name, privacy, thumbnail, category, worldData, levelId)
        {
            AuthorId = authorId;
            Created = created;
            Updated = updated;
            ReviewData = reviewData;
            VerificationLevel = verificationLevel;
        }

        [JsonIgnore]
        public string LevelId { 
            get
            {
                return MaybeLevelId ?? throw new NullReferenceException($"Instance of {nameof(Level)} with a null LevelId");
            }
            set
            {
                MaybeLevelId = value;
            }
        }

        [JsonProperty("authorId")]
        public string AuthorId { get; set; }
        [JsonProperty("uploadDate")]
        public DateTime Created { get; set; }
        [JsonProperty("modifiedDate")]
        public DateTime Updated { get; set; }
        [JsonProperty("reviews")]
        public ReviewData ReviewData { get; set; }
        [JsonProperty("verificationLevel")]
        public VerificationLevel VerificationLevel { get; set; }

        public Task<WorldSaveData> FetchWorldData(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(WorldData);
        }

        public async Task<Sprite> GetThumbnail(CancellationToken cancellationToken = default)
        {
            if (Thumbnail == null)
            {
                return AssetManager.DefaultLevelThumbnail;
            }
            return await AssetManager.GetSprite(Thumbnail, cancellationToken: cancellationToken);
        }
    }

    public class AsyncLevel: IPayload<AsyncLevel>, ILevel
    {
        [JsonProperty("id")]
        public string LevelId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("private")]
        public bool Privacy { get; set; }
        [JsonProperty("thumbnail")]
        public string? Thumbnail { get; set; }
        [JsonProperty("category")]
        public string? Category { get; set; }
        [JsonProperty("author_id")]
        public string AuthorId { get; set; }
        [JsonProperty("created_at")]
        public DateTime Created { get; set; }
        [JsonProperty("updated_at")]
        public DateTime Updated { get; set; }
        [JsonProperty("reviews")]
        public ReviewData ReviewData { get; set; }
        [JsonProperty("verification_level")]
        public VerificationLevel VerificationLevel { get; set; }
        [JsonProperty("world")]
        public string? RawWorldData
        {
            get
            {
                if (CachedData == null) return null;
                return CachedData.Value.Serialize(WorldManager.SerializationOptions);
            }
            set
            {
                if (value == null) { CachedData = null; return; }
                CachedData = WorldSaveData.Deserialize(value, out _);
            }
        }

        protected WorldSaveData? CachedData { get; set; } = null;

        public AsyncLevel(string levelId, string name, bool privacy, string? thumbnail, string? category, string authorId, DateTime created, DateTime updated, ReviewData reviewData, VerificationLevel verificationLevel, string? rawWorldData = null)
        {
            LevelId = levelId;
            Name = name;
            Privacy = privacy;
            Thumbnail = thumbnail;
            Category = category;
            AuthorId = authorId;
            Created = created;
            Updated = updated;
            ReviewData = reviewData;
            VerificationLevel = verificationLevel;
            RawWorldData = rawWorldData;
        }

        public async Task<WorldSaveData> FetchWorldData(CancellationToken cancellationToken = default)
        {
            if (CachedData.HasValue) { return CachedData.Value; }

            CachedData = await ((ILevel)this).RefreshWorldData(cancellationToken);

            return CachedData.Value;
        }

        public async Task<Sprite> GetThumbnail(CancellationToken cancellationToken)
        {
            if (Thumbnail == null)
            {
                return AssetManager.DefaultLevelThumbnail;
            }
            return await AssetManager.GetSprite(Thumbnail, cancellationToken: cancellationToken);
        }
    }

    public class LevelForUpload: IPayload<LevelForUpload>
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("world")]
        public string RawData { get; set; }
        [JsonProperty("private")]
        public bool Privacy { get; set; }
        [JsonProperty("category")]
        public string Category { get; set; }
        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }

        public LevelForUpload(string name, string rawData, bool privacy, string? thumbnail = null, string? category = null)
        {
            Name = name;
            Privacy = privacy;
            Thumbnail = thumbnail ?? string.Empty;
            Category = category ?? string.Empty;
            RawData = rawData;
        }
    }

    public class PaginatedLevels : Paginated<AsyncLevel>, IPayload<PaginatedLevels>, IPaginator<ILevel>
    {
        protected PaginatedCache<AsyncLevel> cache;

        public PaginatedLevels(int total, int perPage, int currentPage, int pageCount, string firstPageUrl, string lastPageUrl, string? prevPageUrl, string? nextPageUrl, string path, int? from, int? to, AsyncLevel[] data) 
            : base(total, perPage, currentPage, pageCount, firstPageUrl, lastPageUrl, prevPageUrl, nextPageUrl, path, from, to, data) 
        {
            cache = new(this);
        }

        public async Task<ILevel> FetchByIndex(int index, CancellationToken cancellationToken = default)
        {
            return await cache.FetchByIndex(index, cancellationToken: cancellationToken);
        }

        public async IAsyncEnumerable<ILevel> FetchPage(int pageIndex, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var value in cache.FetchPage(pageIndex, cancellationToken)) { yield return value; }
        }

        public void Invalidate()
        {
            cache.Invalidate();
        }

        IAsyncEnumerator<ILevel> IAsyncEnumerable<ILevel>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return GetAsyncEnumerator(cancellationToken);
        }

        public override string ToString()
        {
            return cache.ToString();
        }
    }

    [JsonObject]
    public class ReviewData
    {
        [JsonProperty("likes")]
        public int Likes { get; set; } = 0;
        [JsonProperty("completions")]
        public int Completions { get; set; } = 0;
    }
}