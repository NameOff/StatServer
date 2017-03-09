using System;
using System.Collections.Generic;
using System.Linq;

namespace StatServer
{
    public class PlayerStats : Serializable
    {
        public enum Field
        {
            Name, TotalMatchesPlayed, TotalMatchesWon, FavoriteServer,
            UniqueServers, FavoriteGameMode, AverageScoreboardPercent,
            MaximumMatchesPerDay, AverageMatchesPerDay,
            LastMatchPlayed, KillToDeathRatio
        }

        public string Name { get; set; }
        public int TotalMatchesPlayed { get; set; }
        public int TotalMatchesWon { get; set; }
        public string FavoriteServer { get; set; }
        public int UniqueServers { get; set; }
        public string FavoriteGameMode { get; set; }
        public double AverageScoreboardPercent { get; set; }
        public int MaximumMatchesPerDay { get; set; }
        public double AverageMatchesPerDay { get; set; }
        public DateTime LastMatchPlayed { get; set; }
        public double KillToDeathRatio { get; set; }

        private Dictionary<string, int> playedModes { get; set; }
        private Dictionary<string, int> playedServers { get; set; }

        public PlayerStats(string name, int totalMatchesPlayed, int totalMatchesWon, string encodedServers, 
            string encodedModes, double averageScoreboardPercent, DateTime lastMatchPlayed, int totalKills, int totalDeaths)
        {
            Name = name;
            TotalMatchesPlayed = totalMatchesPlayed;
            TotalMatchesWon = totalMatchesWon;
            playedServers = Extensions.DecodeElements(encodedServers);
            playedModes = Extensions.DecodeElements(encodedModes);
            AverageScoreboardPercent = averageScoreboardPercent;
            LastMatchPlayed = lastMatchPlayed;
            KillToDeathRatio = (double) totalKills / totalDeaths;
        }

        public string Serialize(params Field[] fields)
        {
            return Serialize(this, fields.Select(field => field.ToString()).ToArray());
        }

        public void CalculateAverageData(DateTime firstMatchDate, DateTime lastMatchDate)
        {
            AverageMatchesPerDay = Extensions.CalculateAverage(TotalMatchesPlayed, firstMatchDate, lastMatchDate);
        }
    }
}
