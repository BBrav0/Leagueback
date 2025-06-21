using System;
using System.Collections.Generic;
using System.Linq;
using backend.Models;

namespace backend
{
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
    }

    /// <summary>
    /// A class dedicated to analyzing match data. This is the core calculation engine.
    /// </summary>
    public class PerformanceCalculation
    {
        /// <summary>
        /// Analyzes a match and generates a list of data points for performance charts.
        /// </summary>
        /// <returns>A list of ChartDataPoint objects representing performance over time.</returns>
        public List<ChartDataPoint> GenerateChartData(MatchDto matchDetails, MatchTimelineDto matchTimeline, string userPuuid)
        {
            var dataPoints = new List<ChartDataPoint>();

            
            var userParticipant = matchDetails.Info.Participants.FirstOrDefault(p => p.Puuid == userPuuid);
            if (userParticipant == null) return dataPoints;

            var timestampsToReport = new HashSet<int> { 1, 5, 10, 14, 20, 25, 30 };
            int gameDurationInMinutes = (int)Math.Floor(matchDetails.Info.GameDuration / 60.0);

            double cumulativeSoloScore = 0.0;
            double cumulativeTeamScore = 0.0;

            List<PlayerStatsAtTime> previousMinuteStats = new List<PlayerStatsAtTime>();

            var cumScores = new double[2, timestampsToReport.Count+1];
            int count = 0;

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
                    cumScores[0, count] = cumulativeSoloScore;
                    cumScores[1, count] = cumulativeTeamScore / 4;
                    count++;
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
            cumScores[0, count] = cumulativeSoloScore;
            cumScores[1, count] = cumulativeTeamScore / 4;

            dataPoints.Add(new ChartDataPoint
            {
                Minute = -1,

                YourImpact = Enumerable.Range(0, cumScores.GetLength(1)).Select(col => cumScores[0, col]).Average(),

                TeamImpact = Enumerable.Range(0, cumScores.GetLength(1)).Select(col => cumScores[1, col]).Average()
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
    }
}