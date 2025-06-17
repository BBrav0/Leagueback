using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using backend.Models;

namespace backend
{
    // #############################################################################
    // ## 1. CORE ANALYSIS AND DATA CLASSES
    // #############################################################################

    /// <summary>
    /// A clean data class to hold a precise snapshot of a player's stats 
    /// at a specific moment in a match.
    /// </summary>
    public class PlayerStatsAtTime
    {
        public int ParticipantId { get; set; }
        public string SummonerName { get; set; } = string.Empty;
        public string ChampionName { get; set; } = string.Empty;
        public string Lane { get; set; } = string.Empty;
        public int TeamId { get; set; } // Will be 1 for ally team, 2 for enemy team
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public int Gold { get; set; }
        public int CreepScore { get; set; }
        public int DamageToChampions { get; set; }
    }

    /// <summary>
    /// A class dedicated to analyzing match data.
    /// </summary>
    public class PerformanceCalculation
    {
        private string myPuuid = string.Empty;

        /// <summary>
        /// Analyzes a match and generates a detailed performance report with cumulative scoring.
        /// </summary>
        public string AnalyzeMatch(MatchDto matchDetails, MatchTimelineDto matchTimeline, string userPuuid)
        {
            myPuuid = userPuuid;
            var report = new StringBuilder();
            var mainUserParticipant = matchDetails.Info.Participants.FirstOrDefault(p => p.Puuid == userPuuid);
            if (mainUserParticipant == null) return "Could not find user in the match.";

            var timestampsToReport = new HashSet<int> { 1, 5, 10, 14, 20, 25, 30 };
            
            // --- Scores that will be updated cumulatively ---
            double soloImpactScore = 0.0; // Starting at 0
            double teamImpactScore = 0.0; // Starting at 0

            // NEW: Lists to store the scores at each reporting interval for averaging later.
            var soloScoresAtIntervals = new List<double>();
            var teamScoresAtIntervals = new List<double>();

            List<PlayerStatsAtTime> previousMinuteStats = new List<PlayerStatsAtTime>();

            for (int minute = 1; minute < matchTimeline.Info.Frames.Count; minute++)
            {
                // --- Step 1: Define the point values based on the current minute ---
                double killValue, deathValue, assistValue;

                if (minute <= 1) { killValue = 25.0; deathValue = -25.0; assistValue = 12.5; }
                else if (minute <= 5) { killValue = 20.0; deathValue = -20.0; assistValue = 10.0; }
                else if (minute <= 10) { killValue = 17.5; deathValue = -17.5; assistValue = 8.7; }
                else if (minute <= 14) { killValue = 15.0; deathValue = -15.0; assistValue = 7.5; }
                else if (minute <= 20) { killValue = 10.0; deathValue = -10.0; assistValue = 5.0; }
                else if (minute <= 30) { killValue = 5.0; deathValue = -5.0; assistValue = 2.5; }
                else { killValue = 2.5; deathValue = -2.5; assistValue = 1.2; }

                // --- Step 2: Get the stats for this minute and calculate the change (delta) ---
                List<PlayerStatsAtTime> currentMinuteStats = GetPlayerStatsAtMinute(minute, matchDetails, matchTimeline);
                var me = currentMinuteStats.FirstOrDefault(p => p.ParticipantId == mainUserParticipant.ParticipantId);
                if (me == null) continue;

                int myKillsThisMinute = me.Kills;
                int myDeathsThisMinute = me.Deaths;
                int myAssistsThisMinute = me.Assists;
                int allyKillsThisMinute = currentMinuteStats.Where(p => p.TeamId == 1).Sum(p => p.Kills);
                int enemyKillsThisMinute = currentMinuteStats.Where(p => p.TeamId == 2).Sum(p => p.Kills);

                if (previousMinuteStats.Any())
                {
                    var meLastMinute = previousMinuteStats.First(p => p.ParticipantId == me.ParticipantId);
                    myKillsThisMinute -= meLastMinute.Kills;
                    myDeathsThisMinute -= meLastMinute.Deaths;
                    myAssistsThisMinute -= meLastMinute.Assists;
                    allyKillsThisMinute -= previousMinuteStats.Where(p => p.TeamId == 1).Sum(p => p.Kills);
                    enemyKillsThisMinute -= previousMinuteStats.Where(p => p.TeamId == 2).Sum(p => p.Kills);
                }

                // --- Step 3: Update scores using the VARIABLE point values ---
                soloImpactScore += (myKillsThisMinute * killValue) + (myAssistsThisMinute * assistValue) + (myDeathsThisMinute * deathValue);
                teamImpactScore += (allyKillsThisMinute * killValue) - (enemyKillsThisMinute * killValue);

                // --- Step 4: Reporting at key timestamps ---
                if (timestampsToReport.Contains(minute))
                {
                    report.AppendLine($"MINUTE {minute} STATS:");
                    report.AppendLine($"Your Score: {soloImpactScore:F1}");
                    report.AppendLine($"Avg Teammate Score: {(teamImpactScore / 4):F1}\n");

                    // NEW: Add the current scores to our lists for averaging at the end.
                    soloScoresAtIntervals.Add(soloImpactScore);
                    teamScoresAtIntervals.Add(teamImpactScore);
                }
                previousMinuteStats = currentMinuteStats;
            }

            report.AppendLine("--- FINAL GAME SUMMARY ---");
            report.AppendLine($"Your Final Score: {soloImpactScore:F1}");
            report.AppendLine($"Avg Teammate Final Score: {(teamImpactScore / 4):F1}");

            // NEW: Calculate and append the average scores.
            if (soloScoresAtIntervals.Any())
            {
                double averageSoloScore = soloScoresAtIntervals.Average();
                double averageTeamScore = teamScoresAtIntervals.Average();

                report.AppendLine("\n--- OVERALL AVERAGE SCORES ---");
                report.AppendLine($"Overall Average Your Score: {averageSoloScore:F1}");
                report.AppendLine($"Overall Average Teammate Score: {(averageTeamScore / 4):F1}");
            }

            return report.ToString();
        }

        /// <summary>
        /// Calculates the precise stats for every player at a specific minute in the game.
        /// </summary>
        private List<PlayerStatsAtTime> GetPlayerStatsAtMinute(int minute, MatchDto matchDetails, MatchTimelineDto matchTimeline)
        {
            var mainUserParticipant = matchDetails.Info.Participants.FirstOrDefault(p => p.Puuid == myPuuid);
            if (mainUserParticipant == null) return new List<PlayerStatsAtTime>();
            int userTeamId = mainUserParticipant.TeamId;

            var statsDictionary = new Dictionary<int, PlayerStatsAtTime>();
            foreach (var participant in matchDetails.Info.Participants)
            {
                statsDictionary[participant.ParticipantId] = new PlayerStatsAtTime
                {
                    ParticipantId = participant.ParticipantId,
                    SummonerName = participant.SummonerName,
                    ChampionName = participant.ChampionName,
                    Lane = participant.Lane,
                    TeamId = (participant.TeamId == userTeamId ? 1 : 2)
                };
            }

            for (int i = 1; i <= minute; i++)
            {
                var currentFrame = matchTimeline.Info.Frames[i];
                foreach (var participantFrame in currentFrame.ParticipantFrames.Values)
                {
                    if (statsDictionary.TryGetValue(participantFrame.ParticipantId, out var stats))
                    {
                        stats.Gold = participantFrame.TotalGold;
                        stats.CreepScore = participantFrame.MinionsKilled + participantFrame.JungleMinionsKilled;
                        stats.DamageToChampions = participantFrame.DamageStats.TotalDamageDoneToChampions;
                    }
                }
                foreach (var gameEvent in currentFrame.Events)
                {
                    if (gameEvent.Type == "CHAMPION_KILL")
                    {
                        if (statsDictionary.ContainsKey(gameEvent.VictimId)) statsDictionary[gameEvent.VictimId].Deaths++;
                        if (statsDictionary.ContainsKey(gameEvent.KillerId)) statsDictionary[gameEvent.KillerId].Kills++;
                        foreach (int assistId in gameEvent.AssistingParticipantIds)
                        {
                            if (statsDictionary.ContainsKey(assistId)) statsDictionary[assistId].Assists++;
                        }
                    }
                }
            }
            return statsDictionary.Values.ToList();
        }
    }
} 