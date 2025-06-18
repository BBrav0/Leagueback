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
        // Declared as nullable to handle the case where the API key is not configured.
        private readonly RiotApiService? _riotApiService;

        public BackendApiBridge()
        {
            // Get API key from environment variable
            string apiKey = Environment.GetEnvironmentVariable("RIOT_API_KEY") ?? "YOUR_RIOT_API_KEY_HERE";

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_RIOT_API_KEY_HERE")
            {
                // This assignment is now valid because the field is nullable.
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
                // Using null-forgiving operator (!) because we've already checked for null.
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

                // FIX: Moved the null check for userParticipant to before it is used.
                var userParticipant = matchDetails.Info.Participants.FirstOrDefault(p => p.Puuid == userPuuid);
                if (userParticipant == null)
                {
                    return JsonSerializer.Serialize(new PerformanceAnalysisResult
                    {
                        Success = false,
                        Error = "User not found in match."
                    });
                }
                
                // FIX: Added a null check for the timeline.
                if (matchTimeline == null)
                {
                    return JsonSerializer.Serialize(new PerformanceAnalysisResult
                    {
                        Success = false,
                        Error = "Could not retrieve match timeline data."
                    });
                }

                var userTeam = matchDetails.Info.Teams.FirstOrDefault(t => t.TeamId == userParticipant.TeamId);
                string gameResult = userTeam?.Win == true ? "Victory" : "Defeat";

                var duration = TimeSpan.FromSeconds(matchDetails.Info.GameDuration);
                string gameTime = $"{(int)duration.TotalMinutes:D2}:{duration.Seconds:D2}";

                var performanceData = ExtractPerformanceData(matchDetails, matchTimeline, userPuuid);

                var matchSummary = new MatchSummary
                {
                    Id = matchId,
                    SummonerName = userParticipant.SummonerName,
                    Champion = userParticipant.ChampionName,
                    Rank = "Coming soon...",
                    KDA = userParticipant.KDA,
                    CS = GetCreepScore(userParticipant, matchTimeline),
                    // FIX: Added the missing 'matchTimeline' argument to this call.
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
            var dataPoints = new List<ChartDataPoint>();
            var userParticipant = matchDetails.Info.Participants.FirstOrDefault(p => p.Puuid == userPuuid);
            if (userParticipant == null) return dataPoints;

            var timestampsToReport = new HashSet<int> { 1, 5, 10, 14, 20, 25, 30 };
            int gameDurationInMinutes = (int)Math.Floor(matchDetails.Info.GameDuration / 60.0);

            double cumulativeSoloScore = 0.0;
            double cumulativeTeamScore = 0.0;

            List<PlayerStatsAtTime> previousMinuteStats = new List<PlayerStatsAtTime>();

            for (int minute = 1; minute <= gameDurationInMinutes && minute < matchTimeline.Info.Frames.Count; minute++)
            {
                double killValue = GetKillValue(minute);
                double deathValue = -killValue;
                double assistValue = killValue / 2;

                List<PlayerStatsAtTime> currentMinuteStats = GetPlayerStatsAtMinute(minute, matchDetails, matchTimeline, userParticipant.TeamId);
                
                var meCurrent = currentMinuteStats.FirstOrDefault(p => p.ParticipantId == userParticipant.ParticipantId);
                if (meCurrent == null) continue;

                int myKillsThisMinute = meCurrent.Kills;
                int myDeathsThisMinute = meCurrent.Deaths;
                int myAssistsThisMinute = meCurrent.Assists;
                int allyTeamKillsThisMinute = currentMinuteStats.Where(p => p.TeamId == 1).Sum(p => p.Kills);
                int enemyTeamKillsThisMinute = currentMinuteStats.Where(p => p.TeamId == 2).Sum(p => p.Kills);
                
                if (previousMinuteStats.Any())
                {
                    var meLast = previousMinuteStats.FirstOrDefault(p => p.ParticipantId == userParticipant.ParticipantId) ?? new PlayerStatsAtTime();
                    myKillsThisMinute -= meLast.Kills;
                    myDeathsThisMinute -= meLast.Deaths;
                    myAssistsThisMinute -= meLast.Assists;
                    
                    allyTeamKillsThisMinute -= previousMinuteStats.Where(p => p.TeamId == 1).Sum(p => p.Kills);
                    enemyTeamKillsThisMinute -= previousMinuteStats.Where(p => p.TeamId == 2).Sum(p => p.Kills);
                }

                cumulativeSoloScore += (myKillsThisMinute * killValue) + (myDeathsThisMinute * deathValue) + (myAssistsThisMinute * assistValue);
                cumulativeTeamScore += (allyTeamKillsThisMinute * killValue) - (enemyTeamKillsThisMinute * killValue);

                if (timestampsToReport.Contains(minute))
                {
                    dataPoints.Add(new ChartDataPoint
                    {
                        Minute = minute,
                        YourImpact = cumulativeSoloScore,
                        TeamImpact = cumulativeTeamScore / 4
                    });
                }
                
                previousMinuteStats = currentMinuteStats;
            }

            int finalMinute = gameDurationInMinutes > 30 ? 35 : gameDurationInMinutes;
             dataPoints.Add(new ChartDataPoint
             {
                 Minute = finalMinute,
                 YourImpact = cumulativeSoloScore,
                 TeamImpact = cumulativeTeamScore / 4
             });

            return dataPoints;
        }

        private double GetKillValue(int minute)
        {
            return minute switch
            {
                <= 1 => 25.0,
                <= 5 => 20.0,
                <= 10 => 17.5,
                <= 14 => 15.0,
                <= 20 => 10.0,
                <= 30 => 5.0,
                _ => 2.5
            };
        }

        private List<PlayerStatsAtTime> GetPlayerStatsAtMinute(int minute, MatchDto matchDetails, MatchTimelineDto matchTimeline, int userTeamId)
        {
            var statsDictionary = new Dictionary<int, PlayerStatsAtTime>();
            
            foreach (var p in matchDetails.Info.Participants)
            {
                statsDictionary[p.ParticipantId] = new PlayerStatsAtTime
                {
                    ParticipantId = p.ParticipantId,
                    SummonerName = p.SummonerName,
                    ChampionName = p.ChampionName,
                    Lane = p.Lane,
                    TeamId = (p.TeamId == userTeamId ? 1 : 2)
                };
            }

            for (int i = 1; i <= minute && i < matchTimeline.Info.Frames.Count; i++)
            {
                var frame = matchTimeline.Info.Frames[i];
                foreach (var gameEvent in frame.Events)
                {
                    if (gameEvent.Type == "CHAMPION_KILL")
                    {
                        if (statsDictionary.ContainsKey(gameEvent.VictimId)) statsDictionary[gameEvent.VictimId].Deaths++;
                        if (statsDictionary.ContainsKey(gameEvent.KillerId)) statsDictionary[gameEvent.KillerId].Kills++;
                        foreach (int assistId in gameEvent.AssistingParticipantIds ?? Enumerable.Empty<int>())
                        {
                            if (statsDictionary.ContainsKey(assistId)) statsDictionary[assistId].Assists++;
                        }
                    }
                }
            }
            return statsDictionary.Values.ToList();
        }

        private int GetParticipantId(MatchDto matchDetails, string puuid)
        {
            return matchDetails.Info.Participants.FirstOrDefault(p => p.Puuid == puuid)?.ParticipantId ?? 0;
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

        private int GetVisionScore(Participant participant, MatchTimelineDto timeline)
        {
            // The VisionScore property is not available on your Participant model,
            // so this is reverted to the original placeholder to prevent a compile error.
            return 15; // Placeholder
        }
    }
}