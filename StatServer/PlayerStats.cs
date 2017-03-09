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

        private int TotalKills { get; set; }
        private int TotalDeaths { get; set; }
        private Dictionary<string, int> playedModes { get; }
        private Dictionary<string, int> playedServers { get; }

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
            TotalKills = totalKills;
            TotalDeaths = totalDeaths;
            UpdateKillToDeathRatio();
        }

        public PlayerStats(string name)
        {
            Name = name;
            playedModes = new Dictionary<string, int>();
            playedServers = new Dictionary<string, int>();
        }

        private void UpdateKillToDeathRatio()
        {
            KillToDeathRatio = (double)TotalKills / TotalDeaths;
        }

        public double CalculateScoreboardPercent(GameMatchResult match)
        {
            var place = -1;
            var players = match.Results.Scoreboard;
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].Name != Name) continue;
                place = i + 1;
                break;
            }
            return (double)(players.Length - place) / (players.Length - 1) * 100;
        }

        public void UpdateStats(GameMatchResult match, Dictionary<string, Dictionary<DateTime, int>> playersMatchesPerDay)
        {
            var scoreboardPercent = CalculateScoreboardPercent(match);
            if (match.Results.Scoreboard.First().Name == Name)
                TotalMatchesWon++;
            AverageScoreboardPercent = (AverageScoreboardPercent * TotalMatchesPlayed + scoreboardPercent) / ++TotalMatchesPlayed;
            var mode = match.Results.GameMode;
            var server = match.Server;
            var date = match.Timestamp;
            playedModes[mode] = playedModes.ContainsKey(mode) ? playedModes[mode] + 1 : 1;
            playedServers[server] = playedServers.ContainsKey(server) ? playedServers[server] + 1 : 1;
            FavoriteServer = playedServers.Keys.OrderByDescending(key => playedServers[key]).First();
            FavoriteGameMode = playedModes.Keys.OrderByDescending(key => playedModes[key]).First();
            UniqueServers = playedServers.Keys.Count;
            playersMatchesPerDay[Name][date] = playersMatchesPerDay[Name].ContainsKey(date)
                ? playersMatchesPerDay[Name][date] + 1
                : 1;
            MaximumMatchesPerDay = playersMatchesPerDay[Name].Values.Max();
            var playerResult = match.Results.Scoreboard.First(info => info.Name == Name);
            TotalKills += playerResult.Kills;
            TotalDeaths += playerResult.Deaths;
            UpdateKillToDeathRatio();
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
