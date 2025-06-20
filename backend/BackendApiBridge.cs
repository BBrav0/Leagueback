using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using backend.Models;
using System.Net.Http;

namespace backend // This should be your project's namespace
{
    [ComVisible(true)]
    public class BackendApiBridge
    {
        private readonly RiotApiService? _riotApiService;

        // Supabase Edge Function that returns the Riot API key stored as a secret
        private const string SUPABASE_FUNCTION_URL = "https://ucbsqhyoerxkvjhuirfo.functions.supabase.co/riot-proxy";
        // Public anon key (safe to embed)
        private const string SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InVjYnNxaHlvZXJ4a3ZqaHVpcmZvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTAyNzYyMjEsImV4cCI6MjA2NTg1MjIyMX0.a85ZF8TXqBGSVizzkCjQwWflkUSgiZutHelEy8ru5D4";
        private static readonly HttpClient _httpClient = new HttpClient();

        public BackendApiBridge()
        {
            // Attempt to retrieve the key from Supabase first
            string apiKey = FetchApiKeyFromSupabase();

            // Fallback to environment variable if the call fails (helps during dev)
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("RIOT_API_KEY") ?? string.Empty;
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                _riotApiService = null;
                return;
            }

            _riotApiService = new RiotApiService(apiKey);
        }

        private static string FetchApiKeyFromSupabase()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, SUPABASE_FUNCTION_URL);
                request.Headers.Add("apikey", SUPABASE_ANON_KEY);
                request.Headers.Add("Authorization", $"Bearer {SUPABASE_ANON_KEY}");

                var response = _httpClient.Send(request);
                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                var content = response.Content.ReadAsStringAsync().Result.Trim();
                return content;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> GetAccount(string gameName, string tagLine)
        {
            if (_riotApiService == null)
            {
                return JsonSerializer.Serialize(new { error = "API Key is not configured in the C# backend." });
            }

            try
            {
                    #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                AccountDto account = await _riotApiService!.GetAccountByRiotIdAsync(gameName, tagLine);
                    #pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                return JsonSerializer.Serialize(account);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> GetMatchHistory(string puuid, int count = 5)
        {
            if (_riotApiService == null)
            {
                return JsonSerializer.Serialize(new { error = "API Key is not configured in the C# backend." });
            }

            try
            {
                var matchIds = await _riotApiService!.GetMatchHistory(puuid, count);
                return JsonSerializer.Serialize(matchIds);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> AnalyzeMatchPerformance(string matchId, string userPuuid)
        {
            if (_riotApiService == null)
            {
                return JsonSerializer.Serialize(new PerformanceAnalysisResult
                {
                    Success = false,
                    Error = "API Key is not configured in the C# backend."
                });
            }

            try
            {
                var matchDetails = await _riotApiService.GetMatchDetails(matchId);
                var matchTimeline = await _riotApiService.GetMatchTimeline(matchId);

                var userParticipant = matchDetails.Info.Participants.FirstOrDefault(p => p.Puuid == userPuuid);
                if (userParticipant == null)
                {
                    return JsonSerializer.Serialize(new PerformanceAnalysisResult { Success = false, Error = "User not found in match." });
                }
                
                if (matchTimeline == null)
                {
                    return JsonSerializer.Serialize(new PerformanceAnalysisResult { Success = false, Error = "Could not retrieve match timeline data." });
                }
                var userTeam = matchDetails.Info.Teams.FirstOrDefault(t => t.TeamId == userParticipant.TeamId);
                string gameResult = userTeam?.Win == true ? "Victory" : "Defeat";

                var duration = TimeSpan.FromSeconds(matchDetails.Info.GameDuration);
                string gameTime = $"{(int)duration.TotalMinutes:D2}:{duration.Seconds:D2}";

                // --- REFACTORED SECTION ---
                // 1. Instantiate the specialist calculation class.
                var calculator = new PerformanceCalculation();

                // 2. Delegate the complex analysis to the specialist.
                var performanceData = calculator.GenerateChartData(matchDetails, matchTimeline, userPuuid);
                // --- END REFACTORED SECTION ---


                ChartDataPoint impacts = performanceData.FirstOrDefault(point => point.Minute == -1);
                double teamImpactAvg = impacts.TeamImpact;
                double yourImpactAvg = impacts.YourImpact;

                performanceData.Remove(impacts);

                var matchSummary = new MatchSummary
                {
                    Id = matchId,
                    SummonerName = userParticipant.SummonerName,
                    Champion = userParticipant.ChampionName,
                    Rank = "Feature coming soon 👀",
                    KDA = userParticipant.KDA,
                    CS = GetCreepScore(userParticipant, matchTimeline),
                    VisionScore = GetVisionScore(userParticipant, matchDetails),
                    GameResult = gameResult,
                    GameTime = gameTime,
                    Data = performanceData,
                    TeamImpact = teamImpactAvg,
                    YourImpact = yourImpactAvg

                };

                return JsonSerializer.Serialize(new PerformanceAnalysisResult
                {
                    Success = true,
                    MatchSummary = matchSummary
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new PerformanceAnalysisResult
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }
        
        private int GetCreepScore(Participant participant, MatchTimelineDto timeline)
        {
            var lastFrame = timeline.Info.Frames.LastOrDefault();
            if (lastFrame?.ParticipantFrames.TryGetValue(participant.ParticipantId.ToString(), out var frame) == true)
            {
                return frame.MinionsKilled + frame.JungleMinionsKilled;
            }
            return 0;
        }

        private int GetVisionScore(Participant participant, MatchDto match)
        {
            return match.Info.Participants.FirstOrDefault(p => p.ParticipantId == participant.ParticipantId)?.VisionScore ?? 0;
        }
    }
}