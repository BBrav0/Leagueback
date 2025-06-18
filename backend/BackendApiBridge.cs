using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using backend.Models; // Or the namespace where your Models.cs file is

namespace backend // This should be your project's namespace
{
    // This attribute is essential. It makes the class visible to JavaScript.
    [ComVisible(true)]
    public class BackendApiBridge
    {
        private readonly RiotApiService _riotApiService;

        public BackendApiBridge()
        {
            // Get API key from environment variable
            string apiKey = Environment.GetEnvironmentVariable("RIOT_API_KEY") ?? "YOUR_RIOT_API_KEY_HERE"; 
            
            if(string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_RIOT_API_KEY_HERE")
            {
                // This prevents the app from crashing if the key isn't set.
                // The frontend will receive a clear error message.
                _riotApiService = null; 
                return;
            }

            _riotApiService = new RiotApiService(apiKey);
        }

        // This is the public C# method that your Next.js code will be able to call.
        public async Task<string> GetAccount(string gameName, string tagLine)
        {
            if (_riotApiService == null)
            {
                return JsonSerializer.Serialize(new { error = "API Key is not configured in the C# backend." });
            }
            
            try
            {
                // We call the real service to get the data.
                AccountDto account = await _riotApiService.GetAccountByRiotIdAsync(gameName, tagLine);
                
                // We convert the C# object into a JSON string to send to the frontend.
                return JsonSerializer.Serialize(account);
            }
            catch (Exception ex)
            {
                // If any error happens in C#, we catch it and send back a
                // clean error message as a JSON string.
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
                // Get match details and timeline
                var matchDetails = await _riotApiService.GetMatchDetails(matchId);
                var matchTimeline = await _riotApiService.GetMatchTimeline(matchId);
                
                // Find the user's participant data
                var userParticipant = matchDetails.Info.Participants.FirstOrDefault(p => p.Puuid == userPuuid);
                if (userParticipant == null)
                {
                    return JsonSerializer.Serialize(new PerformanceAnalysisResult 
                    { 
                        Success = false, 
                        Error = "User not found in match." 
                    });
                }
                
                // Determine game result
                var userTeam = matchDetails.Info.Teams.FirstOrDefault(t => t.TeamId == userParticipant.TeamId);
                string gameResult = userTeam?.Win == true ? "Victory" : "Defeat";
                
                // Convert game duration to MM:SS format
                var duration = TimeSpan.FromSeconds(matchDetails.Info.GameDuration);
                string gameTime = $"{(int)duration.TotalMinutes:D2}:{duration.Seconds:D2}";
                
                // Calculate performance metrics using existing PerformanceCalculation
                var performanceCalc = new PerformanceCalculation();
                var performanceData = ExtractPerformanceData(matchDetails, matchTimeline, userPuuid);
                
                // Create match summary
                var matchSummary = new MatchSummary
                {
                    Id = matchId,
                    SummonerName = userParticipant.SummonerName,
                    Champion = userParticipant.ChampionName,
                    Rank = "Coming soon...", 
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

        private List<ChartDataPoint> ExtractPerformanceData(MatchDto matchDetails, MatchTimelineDto matchTimeline, string userPuuid)
        {
            var performanceCalc = new PerformanceCalculation();
            var dataPoints = new List<ChartDataPoint>();
            var timestampsToReport = new int[] { 1, 5, 10, 14, 20, 25, 30 };
            
            // Get all player stats throughout the game
            var playerStatsAtMinutes = new Dictionary<int, List<PlayerStatsAtTime>>();
            
            foreach (var minute in timestampsToReport)
            {
                var stats = GetPlayerStatsAtMinute(minute, matchDetails, matchTimeline);
                playerStatsAtMinutes[minute] = stats;
            }
            
            // Calculate impact scores for each timestamp
            double cumulativeSoloScore = 0;
            double cumulativeTeamScore = 0;
            
            foreach (var minute in timestampsToReport)
            {
                var stats = playerStatsAtMinutes[minute];
                var userStats = stats.FirstOrDefault(s => s.ParticipantId == GetParticipantId(matchDetails, userPuuid));
                
                if (userStats != null)
                {
                    // Calculate impact based on the performance calculation logic
                    var (soloImpact, teamImpact) = CalculateImpactForMinute(minute, stats, userStats);
                    
                    cumulativeSoloScore = soloImpact;
                    cumulativeTeamScore = teamImpact;
                    
                    dataPoints.Add(new ChartDataPoint
                    {
                        Minute = minute,
                        YourImpact = cumulativeSoloScore,
                        TeamImpact = cumulativeTeamScore
                    });
                }
            }
            
            // Add final data point if game lasted longer than 30 minutes
            var gameDurationMinutes = (int)(matchDetails.Info.GameDuration / 60);
            if (gameDurationMinutes > 30)
            {
                dataPoints.Add(new ChartDataPoint
                {
                    Minute = 35, // "Final" in frontend
                    YourImpact = cumulativeSoloScore,
                    TeamImpact = cumulativeTeamScore
                });
            }
            
            return dataPoints;
        }
        
        private (double soloImpact, double teamImpact) CalculateImpactForMinute(int minute, List<PlayerStatsAtTime> allStats, PlayerStatsAtTime userStats)
        {
            // Apply time-weighted scoring based on PerformanceCalculation logic
            double killValue = GetKillValue(minute);
            double deathValue = -killValue;
            double assistValue = killValue / 2;
            
            double soloImpact = (userStats.Kills * killValue) + (userStats.Deaths * deathValue) + (userStats.Assists * assistValue);
            
            // Calculate team impact (team kills vs enemy kills)
            var allyStats = allStats.Where(s => s.TeamId == userStats.TeamId && s.ParticipantId != userStats.ParticipantId).ToList();
            var enemyStats = allStats.Where(s => s.TeamId != userStats.TeamId).ToList();
            
            int allyKills = allyStats.Sum(s => s.Kills);
            int enemyKills = enemyStats.Sum(s => s.Kills);
            
            double teamImpact = ((allyKills - enemyKills) * killValue) / 4; // Divided by 4 teammates
            
            return (soloImpact, teamImpact);
        }
        
        private double GetKillValue(int minute)
        {
            return minute switch
            {
                <= 1 => 25,
                <= 5 => 20,
                <= 10 => 17.5,
                <= 14 => 15,
                <= 20 => 10,
                <= 30 => 5,
                _ => 2.5
            };
        }
        
        private List<PlayerStatsAtTime> GetPlayerStatsAtMinute(int minute, MatchDto matchDetails, MatchTimelineDto matchTimeline)
        {
            var stats = new List<PlayerStatsAtTime>();
            
            foreach (var participant in matchDetails.Info.Participants)
            {
                // Get timeline data for this participant at the specified minute
                var frameIndex = Math.Min(minute, matchTimeline.Info.Frames.Count - 1);
                var frame = matchTimeline.Info.Frames[frameIndex];
                
                if (frame.ParticipantFrames.TryGetValue(participant.ParticipantId.ToString(), out var participantFrame))
                {
                    stats.Add(new PlayerStatsAtTime
                    {
                        ParticipantId = participant.ParticipantId,
                        SummonerName = participant.SummonerName,
                        ChampionName = participant.ChampionName,
                        Lane = participant.Lane,
                        TeamId = participant.TeamId,
                        Kills = GetKillsAtMinute(participant.ParticipantId, minute, matchTimeline),
                        Deaths = GetDeathsAtMinute(participant.ParticipantId, minute, matchTimeline),
                        Assists = GetAssistsAtMinute(participant.ParticipantId, minute, matchTimeline),
                        Gold = participantFrame.TotalGold,
                        CreepScore = participantFrame.CreepScore,
                        DamageToChampions = participantFrame.DamageStats?.TotalDamageDoneToChampions ?? 0
                    });
                }
            }
            
            return stats;
        }
        
        private int GetParticipantId(MatchDto matchDetails, string puuid)
        {
            return matchDetails.Info.Participants.FirstOrDefault(p => p.Puuid == puuid)?.ParticipantId ?? 0;
        }
        
        private int GetCreepScore(Participant participant, MatchTimelineDto timeline)
        {
            // Get final creep score from timeline
            var lastFrame = timeline.Info.Frames.LastOrDefault();
            if (lastFrame?.ParticipantFrames.TryGetValue(participant.ParticipantId.ToString(), out var frame) == true)
            {
                return frame.CreepScore;
            }
            return 0;
        }
        
        private int GetVisionScore(Participant participant, MatchTimelineDto timeline)
        {
            // Calculate vision score from timeline events (simplified)
            // In a full implementation, you'd count ward placements, destructions, etc.
            return 15; // Placeholder - implement based on timeline events
        }
        
        private int GetKillsAtMinute(int participantId, int minute, MatchTimelineDto timeline)
        {
            int kills = 0;
            foreach (var frame in timeline.Info.Frames.Take(minute + 1))
            {
                kills += frame.Events?.Count(e => e.Type == "CHAMPION_KILL" && e.KillerId == participantId) ?? 0;
            }
            return kills;
        }
        
        private int GetDeathsAtMinute(int participantId, int minute, MatchTimelineDto timeline)
        {
            int deaths = 0;
            foreach (var frame in timeline.Info.Frames.Take(minute + 1))
            {
                deaths += frame.Events?.Count(e => e.Type == "CHAMPION_KILL" && e.VictimId == participantId) ?? 0;
            }
            return deaths;
        }
        
        private int GetAssistsAtMinute(int participantId, int minute, MatchTimelineDto timeline)
        {
            int assists = 0;
            foreach (var frame in timeline.Info.Frames.Take(minute + 1))
            {
                assists += frame.Events?.Count(e => e.Type == "CHAMPION_KILL" && e.AssistingParticipantIds?.Contains(participantId) == true) ?? 0;
            }
            return assists;
        }
    }
}