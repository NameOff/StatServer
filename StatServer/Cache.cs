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

        public Dictionary<string, int> Players { get; set; }
        public Dictionary<string, int> GameServersInformation { get; set; }
        public Dictionary<GameMatchResult, int> GameMatches { get; set; }
        public Dictionary<string, DateTime> GameServersFirstMatchDate { get; set; }
        public Dictionary<string, DateTime> PlayersFirstMatchDate { get; set; }
        public Dictionary<string, int> GameServersStats { get; set; }
        public Dictionary<string, int> PlayersStats { get; set; }
        public Dictionary<string, Dictionary<DateTime, int>> GameServersMatchesPerDay { get; set; }
        public Dictionary<string, Dictionary<DateTime, int>> PlayersMatchesPerDay { get; set; }
        public List<GameMatchResult> RecentMatches { get; set; }

        public readonly int MaxCount;

        public Cache(Database database, int maxCount)
        {
            MaxCount = maxCount;
            Players = new Dictionary<string, int>();
            GameMatches = database.CreateGameMatchDictionary();
            GameServersInformation = database.CreateGameServersDictionary();
            GameServersStats = database.CreateGameServersStatsDictionary();
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
                .Take(MaxCount)
                .ToList();
        }
    }
}
