using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using backend.Models; // Or the namespace where your Models.cs file is

namespace backend // This should be your project's namespace
{
    [ComVisible(true)]
    public class BackendApiBridge
    {
        private readonly RiotApiService? _riotApiService;

        public BackendApiBridge()
        {
            string apiKey = Environment.GetEnvironmentVariable("RIOT_API_KEY") ?? "YOUR_RIOT_API_KEY_HERE";

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_RIOT_API_KEY_HERE")
            {
                _riotApiService = null;
                return;
            }

            _riotApiService = new RiotApiService(apiKey);
        }

        public async Task<string> GetAccount(string gameName, string tagLine)
        {
            if (_riotApiService == null)
            {
                return JsonSerializer.Serialize(new { error = "API Key is not configured in the C# backend." });
            }

            try
            {
                AccountDto account = await _riotApiService!.GetAccountByRiotIdAsync(gameName, tagLine);
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

                var matchSummary = new MatchSummary
                {
                    Id = matchId,
                    SummonerName = userParticipant.SummonerName,
                    Champion = userParticipant.ChampionName,
                    Rank = "Feature coming soon 👀",
                    KDA = userParticipant.KDA,
                    CS = GetCreepScore(userParticipant, matchTimeline),
                    VisionScore = GetVisionScore(userParticipant, matchTimeline),
                    GameResult = gameResult,
                    GameTime = gameTime,
                    Data = performanceData
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
        
        // Helper methods for populating the MatchSummary remain here, as that is part of the Bridge's job.
        private int GetCreepScore(Participant participant, MatchTimelineDto timeline)
        {
            var lastFrame = timeline.Info.Frames.LastOrDefault();
            if (lastFrame?.ParticipantFrames.TryGetValue(participant.ParticipantId.ToString(), out var frame) == true)
            {
                return frame.MinionsKilled + frame.JungleMinionsKilled;
            }
            return 0;
        }

        private int GetVisionScore(Participant participant, MatchTimelineDto timeline)
        {
            // The VisionScore property is not available on your Participant model,
            // so this is reverted to the original placeholder to prevent a compile error.
            return 0; // Placeholder
        }
    }
}