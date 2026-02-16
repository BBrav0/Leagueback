using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using backend.Models; // Or "MyWpfApp.Models" or whatever you have named it
using backend; // Access PlayerCache

namespace backend // Or "MyWpfApp"
{
    public class RiotApiService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        // Cloudflare Worker proxy URL — no secret, safe to embed in source code.
        private const string WORKER_URL = "https://riot-proxy.riot-proxy.workers.dev";

        public RiotApiService()
        {
            // No API key needed — the Worker handles authentication with Riot.
        }

        public async Task<AccountDto?> GetAccountByRiotIdAsync(string gameName, string tagLine)
        {
            if (string.IsNullOrEmpty(gameName) || string.IsNullOrEmpty(tagLine))
            {
                throw new ArgumentException("Game name and tag line must be provided.");
            }

            try
            {
                var url = $"{WORKER_URL}/api/account/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to get account by Riot ID. Status: {response.StatusCode}, Response: {content}");
                }

                return JsonSerializer.Deserialize<AccountDto>(content);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<string>?> GetMatchHistory(string puuid, int count = 10)
        {
            if (string.IsNullOrEmpty(puuid))
            {
                throw new ArgumentNullException(nameof(puuid), "PUUID cannot be null or empty.");
            }

            // Attempt to pull from cache first
            var cache = await PlayerCache.LoadCacheDataAsync();
            bool cacheValid = cache != null && cache.Puuid == puuid && PlayerCache.IsCacheValid(cache, TimeSpan.FromMinutes(10));

            if (cacheValid && cache!.MatchIds.Count >= count)
            {
                return cache.MatchIds.Take(count).ToList();
            }

            const int ApiRequestCount = 10;
            var url = $"{WORKER_URL}/api/matches/{Uri.EscapeDataString(puuid)}?type=ranked&count={ApiRequestCount}";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get match history. Status: {response.StatusCode}, Response: {content}");
            }

            var newMatchIds = JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();

            if (cache == null || cache.Puuid != puuid)
            {
                cache = new PlayerCache.CacheData { Puuid = puuid };
            }

            cache.MatchIds = newMatchIds.Concat(cache.MatchIds).Distinct().ToList();

            await PlayerCache.SaveCacheDataAsync(cache);

            return cache.MatchIds.Take(count).ToList();
        }

        public async Task<MatchDto?> GetMatchDetails(string matchId)
        {
            if (string.IsNullOrEmpty(matchId))
            {
                throw new ArgumentNullException(nameof(matchId), "Match ID cannot be null or empty.");
            }

            var cache = await PlayerCache.LoadCacheDataAsync();
            if (cache != null && cache.MatchDetails.TryGetValue(matchId, out var cachedMatch) &&
                PlayerCache.IsCacheValid(cache, TimeSpan.FromMinutes(10)))
            {
                return cachedMatch;
            }

            var url = $"{WORKER_URL}/api/match/{Uri.EscapeDataString(matchId)}";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get match details. Status: {response.StatusCode}, Response: {content}");
            }

            var matchDto = JsonSerializer.Deserialize<MatchDto>(content);

            if (matchDto != null)
            {
                if (cache == null)
                {
                    cache = new PlayerCache.CacheData();
                }

                cache.MatchDetails[matchId] = matchDto;
                await PlayerCache.SaveCacheDataAsync(cache);
            }

            return matchDto;
        }

        public async Task<MatchTimelineDto?> GetMatchTimeline(string matchId)
        {
            if (string.IsNullOrEmpty(matchId))
            {
                throw new ArgumentNullException(nameof(matchId), "Match ID cannot be null or empty.");
            }

            var cache = await PlayerCache.LoadCacheDataAsync();
            if (cache != null && cache.MatchTimelines.TryGetValue(matchId, out var cachedTimeline) &&
                PlayerCache.IsCacheValid(cache, TimeSpan.FromMinutes(10)))
            {
                return cachedTimeline;
            }

            var url = $"{WORKER_URL}/api/match/{Uri.EscapeDataString(matchId)}/timeline";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get match timeline. Status: {response.StatusCode}, Response: {content}");
            }

            var timelineDto = JsonSerializer.Deserialize<MatchTimelineDto>(content);

            if (timelineDto != null)
            {
                if (cache == null)
                {
                    cache = new PlayerCache.CacheData();
                }

                cache.MatchTimelines[matchId] = timelineDto;
                await PlayerCache.SaveCacheDataAsync(cache);
            }

            return timelineDto;
        }
    }
}
