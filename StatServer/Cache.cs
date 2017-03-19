using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace StatServer
{
    public class Cache
    {
        public DateTime LastMatchDate { get; set; }

        public ConcurrentDictionary<string, double> Players { get; set; }
        public ConcurrentDictionary<string, int> GameServersInformation { get; set; }
        public ConcurrentDictionary<GameMatchResult, int> GameMatches { get; set; }
        public ConcurrentDictionary<string, DateTime> GameServersFirstMatchDate { get; set; }
        public ConcurrentDictionary<string, DateTime> PlayersFirstMatchDate { get; set; }
        public ConcurrentDictionary<string, GameServerStats> GameServersStats { get; set; }
        public ConcurrentDictionary<string, int> PlayersStats { get; set; }
        public ConcurrentDictionary<string, ConcurrentDictionary<DateTime, int>> GameServersMatchesPerDay { get; set; }
        public ConcurrentDictionary<string, ConcurrentDictionary<DateTime, int>> PlayersMatchesPerDay { get; set; }
        public List<GameMatchResult> RecentMatches { get; set; }

        public Cache()
        {
            Players = new ConcurrentDictionary<string, double>();
            GameServersInformation = new ConcurrentDictionary<string, int>();
            GameMatches = new ConcurrentDictionary<GameMatchResult, int>();
            GameServersFirstMatchDate = new ConcurrentDictionary<string, DateTime>();
            PlayersFirstMatchDate = new ConcurrentDictionary<string, DateTime>();
            GameServersStats = new ConcurrentDictionary<string, GameServerStats>();
            PlayersStats = new ConcurrentDictionary<string, int>();
            GameServersMatchesPerDay = new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, int>>();
            PlayersMatchesPerDay = new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, int>>();
            RecentMatches = new List<GameMatchResult>();
        }

        public void RecalculateGameServerStatsAverageData()
        {
            foreach (var server in GameServersStats.Keys)
            {
                if (!GameServersFirstMatchDate.ContainsKey(server))
                    continue;
                GameServersStats[server].CalculateAverageData(GameServersFirstMatchDate[server], LastMatchDate);
            }
        }

        public void UpdateRecentMatches()
        {
            lock (RecentMatches)
            {
                RecentMatches = RecentMatches
                    .OrderByDescending(result => result.Timestamp)
                    .Take(StatServer.ReportStatsMaxCount)
                    .ToList();
            }
        }

        public GameMatchResult[] GetRecentMatches(int count)
        {
            return RecentMatches
                .Take(count)
                .ToArray();
        }

        public PlayerStats[] GetTopPlayers(int count)
        {
            return Players.Keys
                .OrderByDescending(name => Players[name])
                .Take(count)
                .Select(name => new PlayerStats(name, Players[name]))
                .ToArray();
        }

        public GameServerStats[] GetPopularServers(int count)
        {
            return GameServersStats.Values
                .OrderByDescending(server => server.AverageMatchesPerDay)
                .Take(count)
                .ToArray();
        }
    }
}
