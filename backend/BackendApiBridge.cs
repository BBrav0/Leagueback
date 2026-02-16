using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using backend.Models;

namespace backend
{
    [ComVisible(true)]
    public class BackendApiBridge
    {
        private readonly RiotApiService _riotApiService;

        public BackendApiBridge()
        {
            _riotApiService = new RiotApiService();
        }

        public async Task<string> GetAccount(string gameName, string tagLine)
        {
            try
            {
                AccountDto? account = await _riotApiService.GetAccountByRiotIdAsync(gameName, tagLine);
                return JsonSerializer.Serialize(account);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> GetMatchHistory(string puuid, int count = 5)
        {
            try
            {
                var matchIds = await _riotApiService.GetMatchHistory(puuid, count);
                return JsonSerializer.Serialize(matchIds);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        public async Task<string> AnalyzeMatchPerformance(string matchId, string userPuuid)
        {
            try
            {
                var matchDetails = await _riotApiService.GetMatchDetails(matchId);
                var matchTimeline = await _riotApiService.GetMatchTimeline(matchId);

                if (matchDetails == null)
                {
                    return JsonSerializer.Serialize(new PerformanceAnalysisResult { Success = false, Error = "Could not retrieve match details." });
                }

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


                ChartDataPoint? impacts = performanceData.FirstOrDefault(point => point.Minute == -1);
                double teamImpactAvg = impacts?.TeamImpact ?? 0;
                double yourImpactAvg = impacts?.YourImpact ?? 0;

                // === Determine impact category to store in cache ===
                bool youHigher = yourImpactAvg > teamImpactAvg;
                string category;
                if (gameResult == "Victory" && youHigher) category = "impactWins";
                else if (gameResult == "Defeat" && !youHigher) category = "impactLosses";
                else if (gameResult == "Victory") category = "guaranteedWins";
                else category = "guaranteedLosses";

                // Persist to lifetime impact cache (fire & forget)
                _ = ImpactCache.AddOrUpdateCategoryAsync(matchId, category);

                if (impacts != null)
                {
                    performanceData.Remove(impacts);
                }

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

        public string ClearPlayerCache()
        {
            bool success = PlayerCache.DeleteCacheFile();
            return JsonSerializer.Serialize(new { success });
        }

        public string ClearImpactCache()
        {
            bool success = ImpactCache.DeleteCacheFile();
            return JsonSerializer.Serialize(new { success });
        }

        public string ClearAllCaches()
        {
            bool p = PlayerCache.DeleteCacheFile();
            bool i = ImpactCache.DeleteCacheFile();
            bool u = UserCache.DeleteCacheFile();
            return JsonSerializer.Serialize(new { success = p && i && u });
        }
    }
}