using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace StatServer
{
    public class Processor
    {
        public const int MaxCount = 50;
        public const int MinCount = 0;

        private Regex gameServerInfoPath = new Regex(@"^/servers/(\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}-\d{1,5})/info$", RegexOptions.Compiled);
        private Regex gameServerStatsPath = new Regex(@"^/servers/(\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}-\d{1,5})/stats$", RegexOptions.Compiled);
        private Regex gameMatchStatsPath = new Regex(@"^/servers/(\d{1,3}.\d{1,3}.\d{1,3}.\d{1,3}-\d{1,5})/matches/(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)", RegexOptions.Compiled);
        private Regex allGameServersInfoPath = new Regex(@"^/servers/info$", RegexOptions.Compiled);
        private Regex playerStatsPath = new Regex(@"^/players/(\S*?)/stats$", RegexOptions.Compiled);
        private Regex recentMatchesPath = new Regex(@"^/reports/recent_matches(/-{0,1}\d{1})?$", RegexOptions.Compiled);
        private Regex bestPlayersPath = new Regex(@"^/reports/best_players(/-{0,1}\d{1})?$", RegexOptions.Compiled);
        private Regex popularServersPath = new Regex(@"^/reports/popular_servers(/-{0,1}\d{1})?$", RegexOptions.Compiled);
        private Dictionary<Regex, Func<HttpRequest, HttpResponse>> MethodByPattern;

        private Dictionary<string, int> players;
        private Dictionary<string, int> gameServers;
        private Dictionary<GameMatchResult, int> gameMatches;
        //private Dictionary<int, Dictionary<int, int>> playersGameServers;
        private readonly Database database;

        public Processor()
        {
            database = new Database();
            players = new Dictionary<string, int>();
            gameMatches = database.GetGameMatchDictionary();
            gameServers = database.GetGameServersDictionary();
            //playersGameServers = new Dictionary<int, Dictionary<int, int>>();
            MethodByPattern = CreateMethodByPattern();
        }

        private Dictionary<Regex, Func<HttpRequest, HttpResponse>> CreateMethodByPattern()
        {
            return new Dictionary<Regex, Func<HttpRequest, HttpResponse>>
            {
                [gameServerInfoPath] = HandleGameServerInformationRequest,
                [gameMatchStatsPath] = HandleGameMatchStatsRequest,
                [gameServerStatsPath] = HandleGameServerStatsRequest,
                [allGameServersInfoPath] = HandleAllGameServersInfoRequest,
                [playerStatsPath] = HandlePlayerStatsRequest,
                [recentMatchesPath] = HandleRecentMatchesRequest,
                [bestPlayersPath] = HandleBestPlayersRequest,
                [popularServersPath] = HandlePopularServersRequest
            };
        }

        public HttpResponse HandleRequest(HttpRequest request)
        {
            //lock
            try
            {
                foreach (var pattern in MethodByPattern.Keys)
                    if (pattern.IsMatch(request.Uri))
                        return MethodByPattern[pattern](request);
            }
            catch (JsonReaderException e)
            {
                return new HttpResponse(HttpResponse.Answer.BadRequest);
            }
            return new HttpResponse(HttpResponse.Answer.NotFound);
        }

        public HttpResponse HandleGameServerInformationRequest(HttpRequest request)
        {
            var endpoint = gameServerInfoPath.Match(request.Uri).Groups[1].ToString();
            if (request.Method == HttpMethod.Put)
            {
                var info = JsonConvert.DeserializeObject<GameServerInfo>(request.Json);
                database.InsertServerInformation(info, endpoint);
                gameServers[endpoint] = database.GetRowsCount(Database.Table.ServersInformation);
                return new HttpResponse(HttpResponse.Answer.OK);
            }

            if (request.Method == HttpMethod.Get)
            {
                if (!gameServers.ContainsKey(endpoint))
                    return new HttpResponse(HttpResponse.Answer.NotFound);
                var info = database.GetServerInformation(gameServers[endpoint]);
                return new HttpResponse(HttpResponse.Answer.OK, JsonConvert.SerializeObject(info, Formatting.Indented, Serializable.Settings));
            }

            return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
        }

        public HttpResponse HandleGameMatchStatsRequest(HttpRequest request)
        {
            var endpoint = gameMatchStatsPath.Match(request.Uri).Groups[1].ToString();
            var timestamp = gameMatchStatsPath.Match(request.Uri).Groups[2].ToString();
            if (!gameServers.ContainsKey(endpoint))
                return new HttpResponse(HttpResponse.Answer.BadRequest);
            var matchInfo = new GameMatchResult(endpoint, timestamp);
            if (request.Method == HttpMethod.Put)
            {
                var matchStats = JsonConvert.DeserializeObject<GameMatchStats>(request.Json);
                matchInfo.Results = matchStats;
                database.InsertGameMatchStats(matchInfo);
                gameMatches[matchInfo] = database.GetRowsCount(Database.Table.GameMatchStats);
                return new HttpResponse(HttpResponse.Answer.OK);
            }

            if (request.Method == HttpMethod.Get)
            {
                if (!gameMatches.ContainsKey(matchInfo))
                    return new HttpResponse(HttpResponse.Answer.NotFound);
                var stats = database.GetGameMatchStats(gameMatches[matchInfo]);
                var json = JsonConvert.SerializeObject(stats, Formatting.Indented, Serializable.Settings);
                return new HttpResponse(HttpResponse.Answer.OK, json);
            }

            return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
        }

        public HttpResponse HandleAllGameServersInfoRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
            var answer = database.GetAllGameServerInformation().ToArray();
            var json = JsonConvert.SerializeObject(answer, Formatting.Indented, Serializable.Settings);
            return new HttpResponse(HttpResponse.Answer.OK, json);
        }

        public HttpResponse HandlePlayerStatsRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
            throw new NotImplementedException();
        }

        public HttpResponse HandleRecentMatchesRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
            throw new NotImplementedException();
        }

        public HttpResponse HandleBestPlayersRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
            throw new NotImplementedException();
        }

        public HttpResponse HandlePopularServersRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
            throw new NotImplementedException();
        }

        public HttpResponse HandleGameServerStatsRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
            throw new NotImplementedException();
        }

        public HttpResponse PutServerInformation(string json)
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
