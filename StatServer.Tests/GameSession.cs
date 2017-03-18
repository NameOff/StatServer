using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatServer.Tests
{
    class GameSession
    {
        public List<GameMatchResult> Matches { get; set; }
        public List<PlayerStats> PlayersStats { get; set; }
        public List<GameServerInfoResponse> Servers { get; set; }
        public List<GameServerStats> ServersStats { get; set; }

        public GameSession()
        {
            Matches = new List<GameMatchResult>();
            PlayersStats = new List<PlayerStats>();
            Servers = new List<GameServerInfoResponse>();
            ServersStats = new List<GameServerStats>();
        }
    }
}
