using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace backend
{
    /// <summary>
    /// Stores basic summoner information (puuid / riot id) in a non-expiring cache.
    /// The file lives inside ./caches/user_cache.json next to the executable.
    /// </summary>
    public static class UserCache
    {
        private static readonly string CacheFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "caches",
            "user_cache.json");

        public class CacheData
        {
            public string Puuid { get; set; } = string.Empty;
            public string GameName { get; set; } = string.Empty;
            public string TagLine { get; set; } = string.Empty;
        }

        public static async Task SaveCacheDataAsync(string puuid, string gameName, string tagLine)
        {
            var data = new CacheData
            {
                Puuid = puuid,
                GameName = gameName,
                TagLine = tagLine
            };

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(CacheFilePath)!);

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
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