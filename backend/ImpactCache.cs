using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace backend
{
    /// <summary>
    /// Persists lifetime impact categories for each match (impactWins, impactLosses, guaranteedWins, guaranteedLosses)
    /// The cache file lives at ./caches/impact_cache.json next to the executable.
    /// </summary>
    public static class ImpactCache
    {
        private static readonly string CacheFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "caches",
            "impact_cache.json");

        public class CacheData
        {
            // Key = matchId, Value = category string
            public Dictionary<string, string> MatchCategories { get; set; } = new();
            public DateTime LastUpdated { get; set; }
        }

        private static async Task SaveCacheDataAsync(CacheData data)
        {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);

            data.LastUpdated = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(CacheFilePath, json);
        }

        private static async Task<CacheData?> LoadCacheDataAsync()
        {
            try
            {
                if (!File.Exists(CacheFilePath))
                    return null;

                var json = await File.ReadAllTextAsync(CacheFilePath);
                return JsonSerializer.Deserialize<CacheData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Adds or updates a match category in the cache.
        /// </summary>
        public static async Task AddOrUpdateCategoryAsync(string matchId, string category)
        {
            var cache = await LoadCacheDataAsync() ?? new CacheData();

            cache.MatchCategories[matchId] = category;
            await SaveCacheDataAsync(cache);
        }

        public static bool DeleteCacheFile()
        {
            try
            {
                if (File.Exists(CacheFilePath))
                {
                    File.Delete(CacheFilePath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 