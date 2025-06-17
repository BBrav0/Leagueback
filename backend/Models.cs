using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace backend.Models
{
    // DTO for the local client API response
    public class SummonerDto
    {
        [JsonPropertyName("gameName")]
        public string GameName { get; set; } = string.Empty;

        [JsonPropertyName("tagLine")]
        public string TagLine { get; set; } = string.Empty;

        [JsonPropertyName("puuid")]
        public string Puuid { get; set; } = string.Empty;
        public string DisplayName => $"{GameName}#{TagLine}";
    }

    // DTO for the public Account API response
    public class AccountDto
    {
        [JsonPropertyName("puuid")]
        public string Puuid { get; set; } = string.Empty;

        [JsonPropertyName("gameName")]
        public string GameName { get; set; } = string.Empty;

        [JsonPropertyName("tagLine")]
        public string TagLine { get; set; } = string.Empty;
    }

    // DTOs for Match Details
    public class MatchDto
    {
        [JsonPropertyName("info")]
        public MatchInfo Info { get; set; } = new();
    }

    public class MatchInfo
    {
        [JsonPropertyName("participants")]
        public List<Participant> Participants { get; set; } = new();
        [JsonPropertyName("teams")]
        public List<Team> Teams { get; set; } = new();
        [JsonPropertyName("gameDuration")]
        public long GameDuration { get; set; }
    }

    public class Participant
    {
        [JsonPropertyName("summonerName")]
        public string SummonerName { get; set; } = string.Empty;
        [JsonPropertyName("championName")]
        public string ChampionName { get; set; } = string.Empty;
        [JsonPropertyName("kills")]
        public int Kills { get; set; }
        [JsonPropertyName("deaths")]
        public int Deaths { get; set; }
        [JsonPropertyName("assists")]
        public int Assists { get; set; }
        [JsonPropertyName("totalDamageDealtToChampions")]
        public int TotalDamageDealtToChampions { get; set; }
        [JsonPropertyName("teamId")]
        public int TeamId { get; set; }
        [JsonPropertyName("puuid")]
        public string Puuid { get; set; } = string.Empty;
        public string KDA => $"{Kills}/{Deaths}/{Assists}";
        [JsonPropertyName("participantId")]
        public int ParticipantId { get; set; }
        [JsonPropertyName("teamPosition")]
        public string Lane { get; set; } = string.Empty;
    }

    public class Team
    {
        [JsonPropertyName("teamId")]
        public int TeamId { get; set; }
        [JsonPropertyName("win")]
        public bool Win { get; set; }
    }

    // DTOs for Match Timeline
    public class MatchTimelineDto
    {
        [JsonPropertyName("info")]
        public TimelineInfoDto Info { get; set; } = new();
    }

    // DTOs for Frontend Integration
    public class ChartDataPoint
    {
        [JsonPropertyName("minute")]
        public int Minute { get; set; }
        [JsonPropertyName("yourImpact")]
        public double YourImpact { get; set; }
        [JsonPropertyName("teamImpact")]
        public double TeamImpact { get; set; }
    }

    public class MatchSummary
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("summonerName")]
        public string SummonerName { get; set; } = string.Empty;
        [JsonPropertyName("champion")]
        public string Champion { get; set; } = string.Empty;
        [JsonPropertyName("rank")]
        public string Rank { get; set; } = "Unranked";
        [JsonPropertyName("kda")]
        public string KDA { get; set; } = string.Empty;
        [JsonPropertyName("cs")]
        public int CS { get; set; }
        [JsonPropertyName("visionScore")]
        public int VisionScore { get; set; }
        [JsonPropertyName("gameResult")]
        public string GameResult { get; set; } = string.Empty;
        [JsonPropertyName("gameTime")]
        public string GameTime { get; set; } = string.Empty;
        [JsonPropertyName("data")]
        public List<ChartDataPoint> Data { get; set; } = new();
    }

    public class PerformanceAnalysisResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        [JsonPropertyName("matchSummary")]
        public MatchSummary? MatchSummary { get; set; }
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class TimelineInfoDto
    {
        [JsonPropertyName("frames")]
        public List<TimelineFrameDto> Frames { get; set; } = new();
    }

    public class TimelineFrameDto
    {
        [JsonPropertyName("participantFrames")]
        public Dictionary<string, TimelineParticipantFrameDto> ParticipantFrames { get; set; } = new();

        [JsonPropertyName("events")]
        public List<TimelineEventDto> Events { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }
    }

    public class TimelineParticipantFrameDto
    {
        [JsonPropertyName("participantId")]
        public int ParticipantId { get; set; }

        [JsonPropertyName("totalGold")]
        public int TotalGold { get; set; }

        [JsonPropertyName("minionsKilled")]
        public int MinionsKilled { get; set; }

        [JsonPropertyName("jungleMinionsKilled")]
        public int JungleMinionsKilled { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("damageStats")]
        public DamageStatsDto DamageStats { get; set; } = new();

        public int CreepScore => MinionsKilled + JungleMinionsKilled;
    }

    // DTOs for Event Data
    public class DamageStatsDto
    {
        [JsonPropertyName("totalDamageDoneToChampions")]
        public int TotalDamageDoneToChampions { get; set; }
    }

    public class TimelineEventDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("killerId")]
        public int KillerId { get; set; }

        [JsonPropertyName("victimId")]
        public int VictimId { get; set; }

        [JsonPropertyName("assistingParticipantIds")]
        public List<int> AssistingParticipantIds { get; set; } = new();
    }
} 