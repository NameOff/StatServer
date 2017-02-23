using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace StatServer
{
    public class Processor
    {
        public const int MaxCount = 50;
        public const int MinCount = 0;

        private Dictionary<string, int> players;
        private Dictionary<string, int> gameServers;
        private Dictionary<int, Dictionary<int, int>> playersGameServers;

        public Processor()
        {
            var patterns = new[]
            {
                @"^/servers/\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}-\d{1,5}/info$",
                @"^/servers/\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}-\d{1,5}/stats$",
                @"^/servers/\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}-\d{1,5}/matches/\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z",
                @"^/servers/info$",
                @"^/players/\S*?/stats$",
                @"^/reports/recent_matches(/-{0,1}\d{1})?$",
                @"^/reports/best_players(/-{0,1}\d{1})?$",
                @"^/reports/popular_servers(/-{0,1}\d{1})?$"
            };
            players = new Dictionary<string, int>();
            gameServers = new Dictionary<string, int>();
            playersGameServers = new Dictionary<int, Dictionary<int, int>>();
        }

        public HttpResponse HandleRequest(string uri, HttpMethod method, string json = null)
        {
            return new HttpResponse(200);
        }

        public HttpResponse GetServerInformation(string address)
        {
            throw new NotImplementedException();
        }

        public void PutServerInformation(GameServerInfo information)
        {
            throw new NotImplementedException();
        }

        public HttpResponse GetServerStatistics(string address)
        {
            throw new NotImplementedException();
        }

        public GameServerInfo[] GetAllServersInformation()
        {
            throw new NotImplementedException();
        }

        public GameMatchStats GetMatchStatistics(string serverAddress, DateTime matchEndTime)
        {
            throw new NotImplementedException();
        }

        public GameMatchStats AddMatchStatistics(string serverAddress, DateTime matchEndTime)
        {
            throw new NotImplementedException();
        }

        public PlayerInfo GetPlayerStatistics(string name)
        {
            throw new NotImplementedException();
        }

        public GameServerStats AddServerStatistics(string address, GameServerInfo information)
        {
            throw new NotImplementedException();
        }

        private static int AdjustCount(int count)
        {
            if (count > MaxCount)
                return MaxCount;
            if (count < MinCount)
                return MinCount;
            return count;
        }

        public GameMatchStats[] GetRecentMatches(int count = 0)
        {
            count = AdjustCount(count);
            throw new NotImplementedException();
        }

        public int GetBestPlayers(int count = 0)  //TODO
        {
            count = AdjustCount(count);
            throw new NotImplementedException();
        }

        public HttpResponse GetPopularServers(int count = 0)
        {
            count = AdjustCount(count);
            throw new NotImplementedException();
        }
    }
}
