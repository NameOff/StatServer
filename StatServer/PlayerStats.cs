﻿using System;
using System.Collections.Concurrent;
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

        public int TotalKills { get; set; }
        public int TotalDeaths { get; set; }
        public ConcurrentDictionary<string, int> PlayedModes { get; set; }
        public ConcurrentDictionary<string, int> PlayedServers { get; set; }

        public PlayerStats(string name, int totalMatchesPlayed, int totalMatchesWon, ConcurrentDictionary<string, int> servers,
            ConcurrentDictionary<string, int> modes, double averageScoreboardPercent,
            DateTime lastMatchPlayed, int maximumMatchesPerDay, int totalKills, int totalDeaths)
        {
            Name = name.ToLower();
            TotalMatchesPlayed = totalMatchesPlayed;
            TotalMatchesWon = totalMatchesWon;
            PlayedServers = servers;
            PlayedModes = modes;
            AverageScoreboardPercent = averageScoreboardPercent;
            LastMatchPlayed = lastMatchPlayed;
            FavoriteServer = CalculateFavoriteServer(servers);
            FavoriteGameMode = CalculateFavoriteMode(modes);
            UniqueServers = servers.Keys.Count;
            MaximumMatchesPerDay = maximumMatchesPerDay;
            TotalKills = totalKills;
            TotalDeaths = totalDeaths;
            if (TotalDeaths != 0)
                KillToDeathRatio = CalculateKillToDeathRatio(TotalKills, TotalDeaths);
        }

        private string CalculateFavoriteServer(ConcurrentDictionary<string, int> servers)
        {
            return servers.Keys.OrderByDescending(key => servers[key]).First();
        }

        private string CalculateFavoriteMode(ConcurrentDictionary<string, int> modes)
        {
            return modes.Keys.OrderByDescending(mode => modes[mode]).First();
        }

        public PlayerStats()
        {

        }

        public PlayerStats(string name)
        {
            Name = name.ToLower();
            PlayedModes = new ConcurrentDictionary<string, int>();
            PlayedServers = new ConcurrentDictionary<string, int>();
        }

        public PlayerStats(string name, double killToDeathRatio)
        {
            Name = name.ToLower();
            KillToDeathRatio = killToDeathRatio;
        }

        public static double CalculateKillToDeathRatio(int totalKills, int totalDeaths)
        {
            return (double)totalKills / totalDeaths;
        }

        public double CalculateScoreboardPercent(PlayerInfo[] players)
        {
            if (players.Length == 1)
                return 100;
            var place = -1;
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i].Name.ToLower() != Name.ToLower()) continue;
                place = i + 1;
                break;
            }
            return (double)(players.Length - place) / (players.Length - 1) * 100;
        }

        public void UpdateStats(GameMatchResult match, ConcurrentDictionary<DateTime, int> matchesPerDay)
        {
            var scoreboardPercent = CalculateScoreboardPercent(match.Results.Scoreboard);
            if (match.Results.Scoreboard.First().Name == Name)
                TotalMatchesWon++;
            AverageScoreboardPercent = (AverageScoreboardPercent * TotalMatchesPlayed + scoreboardPercent) / ++TotalMatchesPlayed;
            var mode = match.Results.GameMode;
            var server = match.Server;
            PlayedModes[mode] = PlayedModes.ContainsKey(mode) ? PlayedModes[mode] + 1 : 1;
            PlayedServers[server] = PlayedServers.ContainsKey(server) ? PlayedServers[server] + 1 : 1;
            FavoriteServer = CalculateFavoriteServer(PlayedServers);
            FavoriteGameMode = CalculateFavoriteMode(PlayedModes);
            UniqueServers = PlayedServers.Keys.Count;
            MaximumMatchesPerDay = matchesPerDay.Values.DefaultIfEmpty().Max();
            var playerResult = match.Results.Scoreboard.First(info => info.Name == Name);
            TotalKills += playerResult.Kills;
            TotalDeaths += playerResult.Deaths;
            KillToDeathRatio = CalculateKillToDeathRatio(TotalKills, TotalDeaths);
            if (LastMatchPlayed < match.Timestamp)
                LastMatchPlayed = match.Timestamp;
        }

        public string Serialize(params Field[] fields)
        {
            return Serialize(this, fields.Select(field => field.ToString()).ToArray());
        }

        public string SerializeForGetResponse()
        {
            return Serialize(Field.TotalMatchesPlayed, Field.TotalMatchesWon, Field.FavoriteServer,
                Field.UniqueServers, Field.FavoriteGameMode, Field.AverageScoreboardPercent,
                Field.MaximumMatchesPerDay, Field.AverageMatchesPerDay, Field.LastMatchPlayed, Field.KillToDeathRatio);
        }

        public void CalculateAverageData(DateTime firstMatchDate, DateTime lastMatchDate)
        {
            AverageMatchesPerDay = Extensions.CalculateAverage(TotalMatchesPlayed, firstMatchDate, lastMatchDate);
        }
    }
}
