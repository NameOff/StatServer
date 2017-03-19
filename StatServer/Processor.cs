using System;
using System.Collections.Concurrent;
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
        public static readonly Regex GameServerInfoPath = new Regex(@"^/servers/(?<endpoint>\S*?)/info$", RegexOptions.Compiled);
        public static readonly Regex GameServerStatsPath = new Regex(@"^/servers/(?<endpoint>\S*?)/stats$", RegexOptions.Compiled);
        public static readonly Regex GameMatchStatsPath = new Regex(@"^/servers/(?<endpoint>\S*?)/matches/(?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)", RegexOptions.Compiled);
        public static readonly Regex AllGameServersInfoPath = new Regex(@"^/servers/info$", RegexOptions.Compiled);
        public static readonly Regex PlayerStatsPath = new Regex(@"^/players/(?<playerName>\S*?)/stats$", RegexOptions.Compiled);
        public static readonly Regex RecentMatchesPath = new Regex(@"^/reports/recent-matches(/(?<count>-?\d{1,}))?$", RegexOptions.Compiled);
        public static readonly Regex BestPlayersPath = new Regex(@"^/reports/best-players(/(?<count>-?\d{1,}))?$", RegexOptions.Compiled);
        public static readonly Regex PopularServersPath = new Regex(@"^/reports/popular-servers(/(?<count>-?\d{1,}))?$", RegexOptions.Compiled);
        private static Dictionary<Regex, Func<Request, Response>> MethodByPattern;

        public Cache Cache;
        private readonly Database database;

        public Processor()
        {
            Console.WriteLine("Wait. Database is loading...");
            database = new Database();
            Cache = database.CreateCache();
            MethodByPattern = CreateMethodByPattern();
        }

        private Dictionary<Regex, Func<Request, Response>> CreateMethodByPattern()
        {
            return new Dictionary<Regex, Func<Request, Response>>
            {
                [GameServerInfoPath] = HandleGameServerInformationRequest,
                [GameMatchStatsPath] = HandleGameMatchStatsRequest,
                [GameServerStatsPath] = HandleGameServerStatsRequest,
                [AllGameServersInfoPath] = HandleAllGameServersInfoRequest,
                [PlayerStatsPath] = HandlePlayerStatsRequest,
                [RecentMatchesPath] = HandleRecentMatchesRequest,
                [BestPlayersPath] = HandleBestPlayersRequest,
                [PopularServersPath] = HandlePopularServersRequest
            };
        }

        public Response HandleRequest(Request request)
        {
            try
            {
                foreach (var pattern in MethodByPattern.Keys)
                    if (pattern.IsMatch(request.Uri))
                        return MethodByPattern[pattern](request);
            }
            catch (JsonReaderException)
            {
                Logger.Log.Error($"Wrong json format. Client: {request.ClientEndPoint}");
                return new Response(Response.Status.BadRequest);
            }
            catch (JsonSerializationException)
            {
                Logger.Log.Error($"Wrong json format. Client: {request.ClientEndPoint}");
                return new Response(Response.Status.BadRequest);
            }
            Logger.Log.Error($"Wrong Uri. Клиент: {request.ClientEndPoint}. Uri: {request.Uri}");
            return new Response(Response.Status.NotFound);
        }

        public Response HandleGameServerInformationRequest(Request request)
        {
            var endpoint = GameServerInfoPath.Match(request.Uri).Groups["endpoint"].ToString();
            if (request.Method == HttpMethod.Put)
            {
                Console.WriteLine("PUT запрос GameServerInfo");
                var info = JsonConvert.DeserializeObject<GameServerInfo>(request.Json, Serializable.Settings);
                PutGameServerInfo(info, endpoint);
                return new Response(Response.Status.OK);
            }
            if (request.Method == HttpMethod.Get)
            {
                Console.WriteLine("GET запрос GameServerInfo");
                var info = GetGameServerInfo(endpoint);
                if (info == null)
                    return new Response(Response.Status.NotFound);
                var json = JsonConvert.SerializeObject(info, Formatting.Indented, Serializable.Settings);
                return new Response(Response.Status.OK, json);
            }
            return new Response(Response.Status.MethodNotAllowed);
        }

        public void PutGameServerInfo(GameServerInfo info, string endpoint)
        {
            if (Cache.GameServersInformation.ContainsKey(endpoint))
            {
                database.UpdateGameServerInfo(Cache.GameServersInformation[endpoint], info, endpoint);
                Cache.GameServersStats[endpoint].Name = info.Name;
            }
            else
            {
                var id = database.InsertServerInformation(info, endpoint);
                Cache.GameServersInformation[endpoint] = id;
                Cache.GameServersStats[endpoint] = new GameServerStats(endpoint, info.Name);
                Cache.GameServersMatchesPerDay[endpoint] = new ConcurrentDictionary<DateTime, int>();
            }
            Cache.GameServersStats[endpoint].Info = info;
        }

        public GameServerInfo GetGameServerInfo(string endpoint) => Cache.GameServersInformation.ContainsKey(endpoint) ? Cache.GameServersStats[endpoint].Info : null;

        private void UpdateLastDate(DateTime date)
        {
            if (date > Cache.LastMatchDate)
            {
                Cache.LastMatchDate = date;
                Cache.RecalculateGameServerStatsAverageData();
            }
        }

        public Response HandleGameMatchStatsRequest(Request request)
        {
            var endpoint = GameMatchStatsPath.Match(request.Uri).Groups["endpoint"].ToString();
            var timestamp = GameMatchStatsPath.Match(request.Uri).Groups["timestamp"].ToString();
            if (!Cache.GameServersInformation.ContainsKey(endpoint))
                return new Response(Response.Status.BadRequest);
            var matchInfo = new GameMatchResult(endpoint, Extensions.ParseTimestamp(timestamp));

            if (request.Method == HttpMethod.Put)
            {
                Console.WriteLine("PUT запрос GameMatchStats");
                var matchStats = JsonConvert.DeserializeObject<GameMatchStats>(request.Json, Serializable.Settings);
                matchInfo.Results = matchStats;
                PutGameMatchResult(matchInfo);
                return new Response(Response.Status.OK);
            }
            if (request.Method == HttpMethod.Get)
            {
                Console.WriteLine("GET запрос GameMatchStats");
                if (!Cache.GameMatches.ContainsKey(matchInfo))
                    return new Response(Response.Status.NotFound);
                var stats = database.GetGameMatchStats(Cache.GameMatches[matchInfo]);
                var json = JsonConvert.SerializeObject(stats, Formatting.Indented, Serializable.Settings);
                return new Response(Response.Status.OK, json);
            }
            return new Response(Response.Status.MethodNotAllowed);
        }


        public void PutGameMatchResult(GameMatchResult matchInfo)
        {
            var date = matchInfo.Timestamp;
            UpdateLastDate(date);
            var id = database.InsertGameMatchStats(matchInfo);
            var players = GetMatchPlayers(matchInfo.Results);

            AddOrUpdateFirstMatchDate(date, matchInfo.Server, players);
            lock (Cache.RecentMatches)
            {
                Cache.RecentMatches.Add(matchInfo);
            }
            Cache.UpdateRecentMatches();
            Cache.GameMatches[matchInfo] = id;

            UpdateGameServerAndPlayerStats(matchInfo);
        }

        private void UpdateGameServerAndPlayerStats(GameMatchResult matchResult)
        {
            lock (database)
            {
                foreach (var player in GetMatchPlayers(matchResult.Results))
                    AddOrUpdatePlayersStats(player, matchResult);
            }

            var date = matchResult.Timestamp.Date;
            var matchesPerDay = Cache.GameServersMatchesPerDay[matchResult.Server];
            matchesPerDay[date] = matchesPerDay.ContainsKey(date) ? matchesPerDay[date] + 1 : 1;
            Cache.GameServersStats[matchResult.Server].Update(matchResult, matchesPerDay);
            Cache.GameServersStats[matchResult.Server].CalculateAverageData(Cache.GameServersFirstMatchDate[matchResult.Server], Cache.LastMatchDate);
        }

        private IEnumerable<string> GetMatchPlayers(GameMatchStats stats) => stats.Scoreboard.Select(playerInfo => playerInfo.Name);

        private void AddOrUpdateFirstMatchDate(DateTime date, string endpoint, IEnumerable<string> players)
        {
            date = date.Date;
            if (!Cache.GameServersFirstMatchDate.ContainsKey(endpoint) || Cache.GameServersFirstMatchDate[endpoint] > date)
                Cache.GameServersFirstMatchDate[endpoint] = date;

            foreach (var player in players)
            {
                if (!Cache.PlayersFirstMatchDate.ContainsKey(player) || Cache.PlayersFirstMatchDate[player] > date)
                    Cache.PlayersFirstMatchDate[player] = date;
            }
        }

        private void AddOrUpdatePlayersStats(string name, GameMatchResult matchResult)
        {
            var stats = Cache.PlayersStats.ContainsKey(name)
                ? database.GetPlayerStats(Cache.PlayersStats[name])
                : new PlayerStats(name);
            if (!Cache.PlayersMatchesPerDay.ContainsKey(name))
                Cache.PlayersMatchesPerDay[name] = new ConcurrentDictionary<DateTime, int>();

            var date = matchResult.Timestamp.Date;
            var matchesPerDay = Cache.PlayersMatchesPerDay[name];
            matchesPerDay[date] = matchesPerDay.ContainsKey(date) ? matchesPerDay[date] + 1 : 1;
            stats.UpdateStats(matchResult, matchesPerDay);

            if (Cache.PlayersStats.ContainsKey(name))
            {
                var id = Cache.PlayersStats[name];
                database.UpdatePlayerStats(id, stats);
            }
            else
            {
                var id = database.InsertPlayerStats(stats);
                Cache.PlayersStats[name] = id;
            }
            if (stats.TotalMatchesPlayed >= 10 && stats.TotalDeaths != 0)
                Cache.Players[stats.Name] = stats.KillToDeathRatio;
        }

        public Response HandleAllGameServersInfoRequest(Request request)
        {
            if (request.Method != HttpMethod.Get)
                return new Response(Response.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос AllGameServersInfo");
            var servers = GetAllServersInfo();
            var json = JsonConvert.SerializeObject(servers, Formatting.Indented, Serializable.Settings);
            return new Response(Response.Status.OK, json);
        }

        public GameServerInfoResponse[] GetAllServersInfo()
        {
            return Cache.GameServersStats.Keys
                .Select(endpoint => new GameServerInfoResponse(endpoint, Cache.GameServersStats[endpoint].Info))
                .ToArray();
        }

        public Response HandlePlayerStatsRequest(Request request)
        {
            if (request.Method != HttpMethod.Get)
                return new Response(Response.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос PlayerStats");
            var name = HttpUtility.UrlDecode(PlayerStatsPath.Match(request.Uri).Groups["playerName"].ToString()).ToLower();
            var stats = GetPlayerStats(name);
            if (stats == null)
                return new Response(Response.Status.NotFound);
            var json = stats.SerializeForGetResponse();
            return new Response(Response.Status.OK, json);
        }

        public PlayerStats GetPlayerStats(string name)
        {
            name = name.ToLower();
            if (!Cache.PlayersMatchesPerDay.ContainsKey(name))
                return null;
            var stats = database.GetPlayerStats(Cache.PlayersStats[name]);
            if (Cache.PlayersFirstMatchDate.ContainsKey(name))
                stats.CalculateAverageData(Cache.PlayersFirstMatchDate[name], Cache.LastMatchDate);
            return stats;
        }

        public Response HandleGameServerStatsRequest(Request request)
        {
            if (request.Method != HttpMethod.Get)
                return new Response(Response.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос GameServerStats");
            var endpoint = GameServerStatsPath.Match(request.Uri).Groups["endpoint"].ToString();
            var stats = GetGameServerStats(endpoint);
            if (stats == null)
                return new Response(Response.Status.NotFound);
            var json = stats.SerializeForGetResponse();
            return new Response(Response.Status.OK, json);
        }

        public GameServerStats GetGameServerStats(string endpoint)
        {
            if (!Cache.GameServersStats.ContainsKey(endpoint))
                return null;
            var stats = Cache.GameServersStats[endpoint];
            if (Cache.GameServersFirstMatchDate.ContainsKey(endpoint))
                stats.CalculateAverageData(Cache.GameServersFirstMatchDate[endpoint], Cache.LastMatchDate);
            return stats;
        }

        public Response HandleRecentMatchesRequest(Request request)
        {
            if (request.Method != HttpMethod.Get)
                return new Response(Response.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос RecentMatches");
            var count = Extensions.StringCountToInt(RecentMatchesPath.Match(request.Uri).Groups["count"].ToString());
            var matches = GetRecentMatches(count);
            var json = JsonConvert.SerializeObject(matches, Formatting.Indented, Serializable.Settings);
            return new Response(Response.Status.OK, json);
        }

        public GameMatchResult[] GetRecentMatches(int count) => Cache.GetRecentMatches(count);

        public Response HandleBestPlayersRequest(Request request)
        {
            if (request.Method != HttpMethod.Get)
                return new Response(Response.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос BestPlayers");
            var count = Extensions.StringCountToInt(BestPlayersPath.Match(request.Uri).Groups["count"].ToString());
            var players = GetBestPlayers(count);
            var json = Extensions.SerializeTopPlayers(players);
            return new Response(Response.Status.OK, json);
        }

        public PlayerStats[] GetBestPlayers(int count) => Cache.GetTopPlayers(count);

        public Response HandlePopularServersRequest(Request request)
        {
            if (request.Method != HttpMethod.Get)
                return new Response(Response.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос PopularServers");
            var count = Extensions.StringCountToInt(PopularServersPath.Match(request.Uri).Groups["count"].ToString());
            var servers = GetPopularServers(count);
            var json = Extensions.SerializePopularServers(servers);
            return new Response(Response.Status.OK, json);
        }

        public GameServerStats[] GetPopularServers(int count) => Cache.GetPopularServers(count);

        public void ClearDatabaseAndCache()
        {
            database.DropAllTables();
            database.CreateAllTables();
            Cache = new Cache();
        }
    }
}
