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

        public Processor()
        {
            
        }

        public HttpResponse HandleRequest(string uri, HttpMethod method, string json = null)
        {
            throw new NotImplementedException();
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
