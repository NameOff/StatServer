using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatServer
{
    public class Cache
    {
        public DateTime LastMatchDate { get; set; }

        public Dictionary<string, double> Players { get; set; }
        public Dictionary<string, int> GameServersInformation { get; set; }
        public Dictionary<GameMatchResult, int> GameMatches { get; set; }
        public Dictionary<string, DateTime> GameServersFirstMatchDate { get; set; }
        public Dictionary<string, DateTime> PlayersFirstMatchDate { get; set; }
        public Dictionary<string, GameServerStats> GameServersStats { get; set; }
        public Dictionary<string, int> PlayersStats { get; set; }
        public Dictionary<string, Dictionary<DateTime, int>> GameServersMatchesPerDay { get; set; }
        public Dictionary<string, Dictionary<DateTime, int>> PlayersMatchesPerDay { get; set; }
        public List<GameMatchResult> RecentMatches { get; set; }

        public Cache(Database database)
        {
            Players = database.CreatePlayersDictionary();
            GameMatches = database.CreateGameMatchDictionary();
            GameServersInformation = database.CreateGameServersDictionary();
            GameServersStats = new Dictionary<string, GameServerStats>();
            PlayersStats = database.CreatePlayersStatsDictionary();
            GameServersFirstMatchDate = new Dictionary<string, DateTime>();
            GameServersMatchesPerDay = new Dictionary<string, Dictionary<DateTime, int>>();
            PlayersFirstMatchDate = new Dictionary<string, DateTime>();
            PlayersMatchesPerDay = new Dictionary<string, Dictionary<DateTime, int>>();
            RecentMatches = new List<GameMatchResult>();
            database.SetDateTimeDictionaries(this);
        }

        public void UpdateRecentMatches()
        {
            RecentMatches = RecentMatches
                .OrderByDescending(result => result.Timestamp)
                .Take(Extensions.MaxCount)
                .ToList();
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
    }
}
