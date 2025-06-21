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
        private const string AMERICAS_URL = "https://americas.api.riotgames.com";

        // The constructor no longer needs any UI components passed to it.
        public RiotApiService(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "Riot API key cannot be null or empty.");
            }
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Riot-Token", apiKey);
        }

        public async Task<AccountDto?> GetAccountByRiotIdAsync(string gameName, string tagLine)
        {
            if (string.IsNullOrEmpty(gameName) || string.IsNullOrEmpty(tagLine))
            {
                // Throw an exception instead of writing to a UI element.
                throw new ArgumentException("Game name and tag line must be provided.");
            }

            try
            {
                var url = $"{AMERICAS_URL}/riot/account/v1/accounts/by-riot-id/{gameName}/{tagLine}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // Throw a detailed exception on failure.
                    throw new HttpRequestException($"Failed to get account by Riot ID. Status: {response.StatusCode}, Response: {content}");
                }

                return JsonSerializer.Deserialize<AccountDto>(content);
            }
            catch (Exception)
            {
                // Re-throw the original exception to let the caller handle it.
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
                // We already have enough recent matches stored
                return cache.MatchIds.Take(count).ToList();
            }

            // Either cache is invalid or doesn't have enough matches – call Riot API but request ONLY 10 matches max
            const int ApiRequestCount = 10;
            var url = $"{AMERICAS_URL}/lol/match/v5/matches/by-puuid/{puuid}/ids?type=ranked&start=0&count={ApiRequestCount}";
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get match history. Status: {response.StatusCode}, Response: {content}");
            }

            var newMatchIds = JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();

            // Prepare cache object
            if (cache == null || cache.Puuid != puuid)
            {
                cache = new PlayerCache.CacheData { Puuid = puuid };
            }

            // Merge lists ensuring newest→oldest order (API already returns newest first)
            cache.MatchIds = newMatchIds.Concat(cache.MatchIds).Distinct().ToList();

            await PlayerCache.SaveCacheDataAsync(cache);

            // Return as many matches as requested (up to what we have)
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

            var url = $"{AMERICAS_URL}/lol/match/v5/matches/{matchId}";
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

            var url = $"{AMERICAS_URL}/lol/match/v5/matches/{matchId}/timeline";
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