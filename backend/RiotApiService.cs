using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using backend.Models; // Or "MyWpfApp.Models" or whatever you have named it

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

        public async Task<List<string>?> GetMatchHistory(string puuid, int count = 20)
        {
            if (string.IsNullOrEmpty(puuid))
            {
                throw new ArgumentNullException(nameof(puuid), "PUUID cannot be null or empty.");
            }

            try
            {
                var url = $"{AMERICAS_URL}/lol/match/v5/matches/by-puuid/{puuid}/ids?type=ranked&start=0&count={count}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to get match history. Status: {response.StatusCode}, Response: {content}");
                }

                return JsonSerializer.Deserialize<List<string>>(content) ?? new List<string>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<MatchDto?> GetMatchDetails(string matchId)
        {
             if (string.IsNullOrEmpty(matchId))
            {
                throw new ArgumentNullException(nameof(matchId), "Match ID cannot be null or empty.");
            }

            try
            {
                var url = $"{AMERICAS_URL}/lol/match/v5/matches/{matchId}";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to get match details. Status: {response.StatusCode}, Response: {content}");
                }

                return JsonSerializer.Deserialize<MatchDto>(content);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<MatchTimelineDto?> GetMatchTimeline(string matchId)
        {
            if (string.IsNullOrEmpty(matchId))
            {
                throw new ArgumentNullException(nameof(matchId), "Match ID cannot be null or empty.");
            }

            try
            {
                var url = $"{AMERICAS_URL}/lol/match/v5/matches/{matchId}/timeline";
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to get match timeline. Status: {response.StatusCode}, Response: {content}");
                }

                return JsonSerializer.Deserialize<MatchTimelineDto>(content);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}