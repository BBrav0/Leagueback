using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using backend.Models;

namespace backend
{
    public class PlayerCache
    {
        // Cache file will live in ./caches relative to the executable location
        private static readonly string CacheFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "caches",
            "player_cache.json"
        );

        public class CacheData
        {
            public string Puuid { get; set; } = string.Empty;
            public string GameName { get; set; } = string.Empty;
            public string TagLine { get; set; } = string.Empty;

            // Match identifiers returned by GetMatchHistory
            public List<string> MatchIds { get; set; } = new();

            // Cached match details keyed by matchId
            public Dictionary<string, MatchDto> MatchDetails { get; set; } = new();

            // Cached timelines keyed by matchId
            public Dictionary<string, MatchTimelineDto> MatchTimelines { get; set; } = new();

            public DateTime LastUpdated { get; set; }
        }

        public static async Task SaveCacheDataAsync(string puuid, string gameName, string tagLine)
        {
            var cacheData = new CacheData
            {
                Puuid = puuid,
                GameName = gameName,
                TagLine = tagLine,
                LastUpdated = DateTime.UtcNow
            };

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);

            var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(CacheFilePath, json);
        }

        // Overload that allows saving the full cache object (including matches)
        public static async Task SaveCacheDataAsync(CacheData cacheData)
        {
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);

            cacheData.LastUpdated = DateTime.UtcNow;
            var json = JsonSerializer.Serialize(cacheData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(CacheFilePath, json);
        }

        public static async Task<CacheData?> LoadCacheDataAsync()
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

        public static bool IsCacheValid(CacheData? cacheData, TimeSpan? maxAge = null)
        {
            if (cacheData == null)
                return false;

            var threshold = maxAge ?? TimeSpan.FromHours(24);
            return (DateTime.UtcNow - cacheData.LastUpdated) < threshold;
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