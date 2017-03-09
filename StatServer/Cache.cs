using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatServer
{
    public class Cache
    {
        public DateTime lastMatchDate { get; set; }

        public Dictionary<string, int> players { get; set; }
        public Dictionary<string, int> gameServersInformation { get; set; }
        public Dictionary<GameMatchResult, int> gameMatches { get; set; }
        public Dictionary<string, DateTime> gameServersFirstMatchDate { get; set; }
        public Dictionary<string, DateTime> playersFirstMatchDate { get; set; }
        public Dictionary<string, int> gameServersStats { get; set; }
        public Dictionary<string, int> playersStats { get; set; }
        public Dictionary<string, Dictionary<DateTime, int>> gameServersMatchesPerDay { get; set; }
        public Dictionary<string, Dictionary<DateTime, int>> playersMatchesPerDay { get; set; }
    }
}
