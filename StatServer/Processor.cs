using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;

namespace StatServer
{
    public class Processor
    {
        public const int MaxCount = 50;
        public const int MinCount = 0;

        private readonly Regex gameServerInfoPath = new Regex(@"^/servers/(?<gameServerId>\S*?)/info$", RegexOptions.Compiled);
        private readonly Regex gameServerStatsPath = new Regex(@"^/servers/(?<gameServerId>\S*?)/stats$", RegexOptions.Compiled);
        private readonly Regex gameMatchStatsPath = new Regex(@"^/servers/(?<gameServerId>\S*?)/matches/(?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)", RegexOptions.Compiled);
        private readonly Regex allGameServersInfoPath = new Regex(@"^/servers/info$", RegexOptions.Compiled);
        private readonly Regex playerStatsPath = new Regex(@"^/players/(?<playerName>\S*?)/stats$", RegexOptions.Compiled);
        private readonly Regex recentMatchesPath = new Regex(@"^/reports/recent_matches(?<count>/-{0,1}\d{1})?$", RegexOptions.Compiled);
        private readonly Regex bestPlayersPath = new Regex(@"^/reports/best_players(?<count>/-{0,1}\d{1})?$", RegexOptions.Compiled);
        private readonly Regex popularServersPath = new Regex(@"^/reports/popular_servers(?<count>/-{0,1}\d{1})?$", RegexOptions.Compiled);
        private readonly Dictionary<Regex, Func<HttpRequest, HttpResponse>> MethodByPattern;

        private DateTime lastDate { get; set; }

        private Dictionary<string, int> players;
        private readonly Dictionary<string, int> gameServers;
        private readonly Dictionary<GameMatchResult, int> gameMatches;
        private readonly Dictionary<string, DateTime> gameServersFirstMatchDate;
        private readonly Dictionary<string, DateTime> playersFirstMatchDate;
        //private Dictionary<int, Dictionary<int, int>> playersGameServers;
        private readonly Dictionary<string, int> gameServersStats;
        private readonly Database database;

        public Processor()
        {
            database = new Database();
            players = new Dictionary<string, int>();
            gameMatches = database.CreateGameMatchDictionary();
            gameServers = database.CreateGameServersDictionary();
            gameServersStats = database.CreateGameServersStatsDictionary();
            gameServersFirstMatchDate = database.CreateGameServersFirstMatchDate();
            playersFirstMatchDate = database.CreatePlayersFirstMatchDate();
            //playersGameServers = new Dictionary<int, Dictionary<int, int>>();
            MethodByPattern = CreateMethodByPattern();
        }

        private Dictionary<Regex, Func<HttpRequest, HttpResponse>> CreateMethodByPattern()
        {
            return new Dictionary<Regex, Func<HttpRequest, HttpResponse>>
            {
                [gameServerInfoPath] = HandleGameServerInformationRequest, //+
                [gameMatchStatsPath] = HandleGameMatchStatsRequest, //  +/-
                [gameServerStatsPath] = HandleGameServerStatsRequest, //+
                [allGameServersInfoPath] = HandleAllGameServersInfoRequest, //+
                [playerStatsPath] = HandlePlayerStatsRequest, //-
                [recentMatchesPath] = HandleRecentMatchesRequest, //-
                [bestPlayersPath] = HandleBestPlayersRequest, //-
                [popularServersPath] = HandlePopularServersRequest //-
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
            catch (JsonReaderException)
            {
                return new HttpResponse(HttpResponse.Answer.BadRequest);
            }
            return new HttpResponse(HttpResponse.Answer.NotFound);
        }

        public HttpResponse HandleGameServerInformationRequest(HttpRequest request)
        {
            var endpoint = gameServerInfoPath.Match(request.Uri).Groups["gameServerId"].ToString();
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

        private void UpdateLastDate(DateTime date)
        {
            if (lastDate == default(DateTime) || date > lastDate)
                lastDate = date;
        }

        public HttpResponse HandleGameMatchStatsRequest(HttpRequest request)
        {
            var endpoint = gameMatchStatsPath.Match(request.Uri).Groups["gameServerId"].ToString();
            var timestamp = gameMatchStatsPath.Match(request.Uri).Groups["timestamp"].ToString();
            if (!gameServers.ContainsKey(endpoint))
                return new HttpResponse(HttpResponse.Answer.BadRequest);
            var date = Extensions.ParseTimestamp(timestamp);
            var matchInfo = new GameMatchResult(endpoint, timestamp);
            if (request.Method == HttpMethod.Put)
            {
                UpdateLastDate(date);
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

        private void AddOrUpdateGameServerStats(string endpoint, DateTime timestamp, GameMatchStats stats)
        {
            //if (!gameServersStats.ContainsKey())
            throw new NotImplementedException();
        }

        private void AddOrUpdatePlayersStats(string endpoint, DateTime timestamp, GameMatchStats stats)
        {
            throw new NotImplementedException();
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
            var name = HttpUtility.UrlDecode(playerStatsPath.Match(request.Uri).Groups["playerName"].ToString());
            
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
            var endpoint = gameServerStatsPath.Match(request.Uri).Groups["gameServerId"].ToString();
            if (!gameServersStats.ContainsKey(endpoint))
                return new HttpResponse(HttpResponse.Answer.NotFound);
            var stats = database.GetGameServerStats(gameServersStats[endpoint]);
            var json = stats.Serialize(GameServerStats.Field.TotalMatchesPlayed,
                GameServerStats.Field.MaximumMatchesPerDay, GameServerStats.Field.AverageMatchesPerDay,
                GameServerStats.Field.MaximumPopulation, GameServerStats.Field.AveragePopulation,
                GameServerStats.Field.Top5GameModes, GameServerStats.Field.Top5Maps);
            return new HttpResponse(HttpResponse.Answer.OK, json);
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
