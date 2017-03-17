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
        private static Dictionary<Regex, Func<HttpRequest, HttpResponse>> MethodByPattern;

        private Cache cache;
        private Database database;

        public int RequestsCount { get; private set; }

        public Processor()
        {
            Console.WriteLine("Wait...");
            database = new Database();
            cache = new Cache(database);
            MethodByPattern = CreateMethodByPattern();
            Console.WriteLine("OK. Start!");
        }

        private Dictionary<Regex, Func<HttpRequest, HttpResponse>> CreateMethodByPattern()
        {
            return new Dictionary<Regex, Func<HttpRequest, HttpResponse>>
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

        public HttpResponse HandleRequest(HttpRequest request)
        {
            try
            {
                RequestsCount++;
                foreach (var pattern in MethodByPattern.Keys)
                    if (pattern.IsMatch(request.Uri))
                        return MethodByPattern[pattern](request);
            }
            catch (JsonReaderException)
            {
                return new HttpResponse(HttpResponse.Status.BadRequest);
            }
            Console.WriteLine("Incorrect");
            return new HttpResponse(HttpResponse.Status.NotFound);
        }

        public HttpResponse HandleGameServerInformationRequest(HttpRequest request)
        {
            var endpoint = GameServerInfoPath.Match(request.Uri).Groups["endpoint"].ToString();
            if (request.Method == HttpMethod.Put)
            {
                Console.WriteLine("PUT запрос GameServerInfo");
                var info = JsonConvert.DeserializeObject<GameServerInfo>(request.Json, Serializable.Settings);
                PutGameServerInfo(info, endpoint);
                return new HttpResponse(HttpResponse.Status.OK);
            }
            if (request.Method == HttpMethod.Get)
            {
                Console.WriteLine("GET запрос GameServerInfo");
                var info = GetGameServerInfo(endpoint);
                if (info == null)
                    return new HttpResponse(HttpResponse.Status.NotFound);
                var json = JsonConvert.SerializeObject(info, Formatting.Indented, Serializable.Settings);
                return new HttpResponse(HttpResponse.Status.OK, json);
            }
            return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
        }

        public void PutGameServerInfo(GameServerInfo info, string endpoint)
        {
            if (cache.GameServersInformation.ContainsKey(endpoint))
            {
                database.UpdateGameServerInfo(cache.GameServersInformation[endpoint], info, endpoint);
                cache.GameServersStats[endpoint].Name = info.Name;
            }
            else
            {
                var id = database.InsertServerInformation(info, endpoint);
                cache.GameServersInformation[endpoint] = id;
                cache.GameServersStats[endpoint] = new GameServerStats(endpoint, info.Name);
                cache.GameServersMatchesPerDay[endpoint] = new ConcurrentDictionary<DateTime, int>();
            }
            cache.GameServersStats[endpoint].Info = info;
        }

        public GameServerInfo GetGameServerInfo(string endpoint) => cache.GameServersInformation.ContainsKey(endpoint) ? cache.GameServersStats[endpoint].Info : null;

        private void UpdateLastDate(DateTime date)
        {
            if (cache.LastMatchDate == default(DateTime) || date > cache.LastMatchDate)
            {
                cache.LastMatchDate = date;
                foreach (var endpoint in cache.GameServersStats.Keys)
                {
                    if (!cache.GameServersFirstMatchDate.ContainsKey(endpoint))
                        continue;
                    cache.GameServersStats[endpoint].CalculateAverageData(cache.GameServersFirstMatchDate[endpoint], date);
                }
            }
        }

        public HttpResponse HandleGameMatchStatsRequest(HttpRequest request)
        {
            var endpoint = GameMatchStatsPath.Match(request.Uri).Groups["endpoint"].ToString();
            var timestamp = GameMatchStatsPath.Match(request.Uri).Groups["timestamp"].ToString();
            if (!cache.GameServersInformation.ContainsKey(endpoint))
                return new HttpResponse(HttpResponse.Status.BadRequest);
            var matchInfo = new GameMatchResult(endpoint, timestamp);
            if (request.Method == HttpMethod.Put)
            {
                //Console.WriteLine("PUT запрос GameMatchStats");
                var matchStats = JsonConvert.DeserializeObject<GameMatchStats>(request.Json);
                matchInfo.Results = matchStats;
                PutGameMatchResult(matchInfo);
                return new HttpResponse(HttpResponse.Status.OK);
            }
            if (request.Method == HttpMethod.Get)
            {
                Console.WriteLine("GET запрос GameMatchStats");
                if (!cache.GameMatches.ContainsKey(matchInfo))
                    return new HttpResponse(HttpResponse.Status.NotFound);
                var stats = database.GetGameMatchStats(cache.GameMatches[matchInfo]);
                var json = JsonConvert.SerializeObject(stats, Formatting.Indented, Serializable.Settings);
                return new HttpResponse(HttpResponse.Status.OK, json);
            }
            return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
        }


        private void PutGameMatchResult(GameMatchResult matchInfo)
        {
            var date = matchInfo.Timestamp;
            UpdateLastDate(date);
            var id = database.InsertGameMatchStats(matchInfo);
            var players = GetMatchPlayers(matchInfo.Results);

            AddOrUpdateFirstMatchDate(date, matchInfo.Server, players);
            lock (cache.RecentMatches)
            {
                cache.RecentMatches.Add(matchInfo);
            }
            cache.UpdateRecentMatches();
            cache.GameMatches[matchInfo] = id;

            UpdateGameServerAndPlayerStats(matchInfo);
        }

        private void UpdateGameServerAndPlayerStats(GameMatchResult matchResult)
        {
            foreach (var player in GetMatchPlayers(matchResult.Results))
                AddOrUpdatePlayersStats(player, matchResult);

            cache.GameServersStats[matchResult.Server].Update(matchResult, cache.GameServersMatchesPerDay[matchResult.Server]);
            cache.GameServersStats[matchResult.Server].CalculateAverageData(cache.GameServersFirstMatchDate[matchResult.Server], cache.LastMatchDate);
        }

        private IEnumerable<string> GetMatchPlayers(GameMatchStats stats) => stats.Scoreboard.Select(playerInfo => playerInfo.Name);

        private void AddOrUpdateFirstMatchDate(DateTime date, string endpoint, IEnumerable<string> players)
        {
            date = date.Date;
            if (!cache.GameServersFirstMatchDate.ContainsKey(endpoint) || cache.GameServersFirstMatchDate[endpoint] > date)
                cache.GameServersFirstMatchDate[endpoint] = date;

            foreach (var player in players)
            {
                if (!cache.PlayersFirstMatchDate.ContainsKey(player) || cache.PlayersFirstMatchDate[player] > date)
                    cache.PlayersFirstMatchDate[player] = date;
            }
        }

        private void AddOrUpdatePlayersStats(string name, GameMatchResult matchResult)
        {
            var stats = cache.PlayersStats.ContainsKey(name)
                ? database.GetPlayerStats(cache.PlayersStats[name])
                : new PlayerStats(name);
            if (!cache.PlayersMatchesPerDay.ContainsKey(name))
                cache.PlayersMatchesPerDay[name] = new ConcurrentDictionary<DateTime, int>();
            stats.UpdateStats(matchResult, cache.PlayersMatchesPerDay[name]);
            if (cache.PlayersStats.ContainsKey(name))
            {
                var id = cache.PlayersStats[name];
                database.UpdatePlayerStats(id, stats);
            }
            else
            {
                var id = database.InsertPlayerStats(stats);
                cache.PlayersStats[name] = id;
            }
            if (stats.TotalMatchesPlayed >= 10 && stats.TotalDeaths != 0)
                cache.Players[stats.Name] = stats.KillToDeathRatio;
        }

        public HttpResponse HandleAllGameServersInfoRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос AllGameServersInfo");
            var servers = GetAllServersInfo();
            var json = JsonConvert.SerializeObject(servers, Formatting.Indented, Serializable.Settings);
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public GameServerInfoResponse[] GetAllServersInfo()
        {
            return cache.GameServersStats.Keys
                .Select(endpoint => new GameServerInfoResponse(endpoint, cache.GameServersStats[endpoint].Info))
                .ToArray();
        }

        public HttpResponse HandlePlayerStatsRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос PlayerStats");
            var name = HttpUtility.UrlDecode(PlayerStatsPath.Match(request.Uri).Groups["playerName"].ToString());
            var stats = GetPlayerStats(name);
            if (stats == null)
                return new HttpResponse(HttpResponse.Status.NotFound);
            var json = stats.SerializeForGetResponse();
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public PlayerStats GetPlayerStats(string name)
        {
            if (!cache.PlayersMatchesPerDay.ContainsKey(name))
                return null;
            var stats = database.GetPlayerStats(cache.PlayersStats[name]);
            if (cache.PlayersFirstMatchDate.ContainsKey(name))
                stats.CalculateAverageData(cache.PlayersFirstMatchDate[name], cache.LastMatchDate);
            return stats;
        }

        public HttpResponse HandleGameServerStatsRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос GameServerStats");
            var endpoint = GameServerStatsPath.Match(request.Uri).Groups["endpoint"].ToString();
            var stats = GetGameServerStats(endpoint);
            if (stats == null)
                return new HttpResponse(HttpResponse.Status.NotFound);
            var json = stats.SerializeForGetResponse();
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public GameServerStats GetGameServerStats(string endpoint)
        {
            if (!cache.GameServersStats.ContainsKey(endpoint))
                return null;
            var stats = cache.GameServersStats[endpoint];
            if (cache.GameServersFirstMatchDate.ContainsKey(endpoint))
                stats.CalculateAverageData(cache.GameServersFirstMatchDate[endpoint], cache.LastMatchDate);
            return stats;
        }

        public HttpResponse HandleRecentMatchesRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос RecentMatches");
            var count = Extensions.StringCountToInt(RecentMatchesPath.Match(request.Uri).Groups["count"].ToString());
            var matches = cache.GetRecentMatches(count);
            var json = JsonConvert.SerializeObject(matches, Formatting.Indented, Serializable.Settings);
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public HttpResponse HandleBestPlayersRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос BestPlayers");
            var count = Extensions.StringCountToInt(BestPlayersPath.Match(request.Uri).Groups["count"].ToString());
            var players = cache.GetTopPlayers(count);
            var json = Extensions.SerializeTopPlayers(players);
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public HttpResponse HandlePopularServersRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            Console.WriteLine("GET запрос PopularServers");
            var count = Extensions.StringCountToInt(BestPlayersPath.Match(request.Uri).Groups["count"].ToString());
            var servers = cache.GetPopularServers(count);
            var json = Extensions.SerializePopularServers(servers);
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public void ClearDatabaseAndCache()
        {
            database.DropAllTables();
            database.CreateAllTables();
            cache = new Cache(database);
        }
    }
}
