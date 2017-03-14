using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;

namespace StatServer
{
    public class Processor
    {
        public static Regex GameServerInfoPath => new Regex(@"^/servers/(?<gameServerId>\S*?)/info$", RegexOptions.Compiled);
        public static Regex GameServerStatsPath => new Regex(@"^/servers/(?<gameServerId>\S*?)/stats$", RegexOptions.Compiled);
        public static Regex GameMatchStatsPath => new Regex(@"^/servers/(?<gameServerId>\S*?)/matches/(?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)", RegexOptions.Compiled);
        public static Regex AllGameServersInfoPath => new Regex(@"^/servers/info$", RegexOptions.Compiled);
        public static Regex PlayerStatsPath => new Regex(@"^/players/(?<playerName>\S*?)/stats$", RegexOptions.Compiled);
        public static Regex RecentMatchesPath => new Regex(@"^/reports/recent-matches(/(?<count>-?\d{1,}))?$", RegexOptions.Compiled);
        public static Regex BestPlayersPath => new Regex(@"^/reports/best-players(/(?<count>-?\d{1,}))?$", RegexOptions.Compiled);
        public static Regex PopularServersPath => new Regex(@"^/reports/popular-servers(/(?<count>-?\d{1,}))?$", RegexOptions.Compiled);
        private static Dictionary<Regex, Func<HttpRequest, HttpResponse>> MethodByPattern;

        private readonly Cache cache;
        private readonly Database database;

        public Processor(Database database, Cache cache)
        {
            this.database = database;
            this.cache = cache;
            MethodByPattern = CreateMethodByPattern();
            Console.WriteLine("OK. Start!");
        }

        private Dictionary<Regex, Func<HttpRequest, HttpResponse>> CreateMethodByPattern()
        {
            return new Dictionary<Regex, Func<HttpRequest, HttpResponse>>
            {
                [GameServerInfoPath] = HandleGameServerInformationRequest, //+
                [GameMatchStatsPath] = HandleGameMatchStatsRequest, //  +
                [GameServerStatsPath] = HandleGameServerStatsRequest, //+
                [AllGameServersInfoPath] = HandleAllGameServersInfoRequest, //+
                [PlayerStatsPath] = HandlePlayerStatsRequest, //+
                [RecentMatchesPath] = HandleRecentMatchesRequest, //+
                [BestPlayersPath] = HandleBestPlayersRequest, //+
                [PopularServersPath] = HandlePopularServersRequest //-
            };
        }

        public HttpResponse HandleRequest(HttpRequest request)
        {
            try
            {
                foreach (var pattern in MethodByPattern.Keys)
                    if (pattern.IsMatch(request.Uri))
                        return MethodByPattern[pattern](request);
            }
            catch (JsonReaderException)
            {
                return new HttpResponse(HttpResponse.Status.BadRequest);
            }
            return new HttpResponse(HttpResponse.Status.NotFound);
        }

        public HttpResponse HandleGameServerInformationRequest(HttpRequest request)
        {
            lock (database)
            {
                var gameServerId = GameServerInfoPath.Match(request.Uri).Groups["gameServerId"].ToString();
                if (request.Method == HttpMethod.Put)
                {
                    var info = JsonConvert.DeserializeObject<GameServerInfo>(request.Json, Serializable.Settings);
                    if (cache.GameServersInformation.ContainsKey(gameServerId))
                    {
                        database.UpdateGameServerInfo(cache.GameServersInformation[gameServerId], info, gameServerId);
                        cache.GameServersStats[gameServerId].Name = info.Name;
                    }
                    else
                    {
                        var id = database.InsertServerInformation(info, gameServerId);
                        cache.GameServersInformation[gameServerId] = id;
                        cache.GameServersStats[gameServerId] = new GameServerStats(gameServerId, info.Name);
                    }
                    cache.GameServersStats[gameServerId].Info = info;
                    return new HttpResponse(HttpResponse.Status.OK);
                }

                if (request.Method == HttpMethod.Get)
                {
                    if (!cache.GameServersInformation.ContainsKey(gameServerId))
                        return new HttpResponse(HttpResponse.Status.NotFound);
                    var info = cache.GameServersStats[gameServerId].Info;
                    return new HttpResponse(HttpResponse.Status.OK,
                        JsonConvert.SerializeObject(info, Formatting.Indented, Serializable.Settings));
                }
            }
            return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
        }

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
            var gameServerId = GameMatchStatsPath.Match(request.Uri).Groups["gameServerId"].ToString();
            var timestamp = GameMatchStatsPath.Match(request.Uri).Groups["timestamp"].ToString();
            if (!cache.GameServersInformation.ContainsKey(gameServerId))
                return new HttpResponse(HttpResponse.Status.BadRequest);
            var date = Extensions.ParseTimestamp(timestamp);
            var matchInfo = new GameMatchResult(gameServerId, timestamp);
            if (request.Method == HttpMethod.Put)
            {
                Stopwatch a = new Stopwatch();
                a.Start();

                PutGameMatchResult(request.Json, matchInfo);

                a.Stop();
                Console.WriteLine($"PUT запрос статистики матча. Общее время: {a.Elapsed}");

                return new HttpResponse(HttpResponse.Status.OK);
            }

            if (request.Method == HttpMethod.Get)
            {
                Stopwatch a = new Stopwatch();
                a.Start();

                if (!cache.GameMatches.ContainsKey(matchInfo))
                    return new HttpResponse(HttpResponse.Status.NotFound);
                var stats = database.GetGameMatchStats(cache.GameMatches[matchInfo]);
                var json = JsonConvert.SerializeObject(stats, Formatting.Indented, Serializable.Settings);

                a.Stop();
                Console.WriteLine($"GET запрос статистики матча. Общее время: {a.Elapsed}");

                return new HttpResponse(HttpResponse.Status.OK, json);
            }

            return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
        }

        private void PutGameMatchResult(string json, GameMatchResult matchInfo)
        {
            var date = matchInfo.Timestamp;
            UpdateLastDate(date);
            var matchStats = JsonConvert.DeserializeObject<GameMatchStats>(json, Serializable.Settings);
            matchInfo.Results = matchStats;
            var id = database.InsertGameMatchStats(matchInfo);
            var players = matchStats.Scoreboard.Select(playerInfo => playerInfo.Name).ToArray();
            AddOrdUpdateFirstMatchDate(date, matchInfo.Server, players);
            cache.RecentMatches.Add(matchInfo);
            cache.UpdateRecentMatches();
            cache.GameMatches[matchInfo] = id;

            var a = new Stopwatch();
            a.Start();

            foreach (var player in players)
                AddOrUpdatePlayersStats(player, matchInfo);
            if (!cache.GameServersMatchesPerDay.ContainsKey(matchInfo.Server))
                cache.GameServersMatchesPerDay[matchInfo.Server] = new Dictionary<DateTime, int>();
            cache.GameServersStats[matchInfo.Server].Update(matchInfo, cache.GameServersMatchesPerDay[matchInfo.Server]);
            cache.GameServersStats[matchInfo.Server].CalculateAverageData(cache.GameServersFirstMatchDate[matchInfo.Server], cache.LastMatchDate);

            a.Stop();
            Console.WriteLine($"PUT запрос статистики матча. БД: {a.Elapsed}");
        }

        private void AddOrdUpdateFirstMatchDate(DateTime date, string endpoint, string[] players)
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
            var stats = cache.PlayersStats.ContainsKey(name) ? database.GetPlayerStats(cache.PlayersStats[name]) : new PlayerStats(name);
            if (!cache.PlayersMatchesPerDay.ContainsKey(name))
                cache.PlayersMatchesPerDay[name] = new Dictionary<DateTime, int>();
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
            var answer = database.GetAllGameServerInformation().ToArray();
            var json = JsonConvert.SerializeObject(answer, Formatting.Indented, Serializable.Settings);
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public HttpResponse HandlePlayerStatsRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            var name = HttpUtility.UrlDecode(PlayerStatsPath.Match(request.Uri).Groups["playerName"].ToString());
            if (!cache.PlayersMatchesPerDay.ContainsKey(name))
                return new HttpResponse(HttpResponse.Status.NotFound);
            var stats = database.GetPlayerStats(cache.PlayersStats[name]);
            if (cache.PlayersFirstMatchDate.ContainsKey(name))
                stats.CalculateAverageData(cache.PlayersFirstMatchDate[name], cache.LastMatchDate);
            var json = stats.SerializeForGetResponse();
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public HttpResponse HandleGameServerStatsRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            var gameServerId = GameServerStatsPath.Match(request.Uri).Groups["gameServerId"].ToString();
            if (!cache.GameServersStats.ContainsKey(gameServerId))
                return new HttpResponse(HttpResponse.Status.NotFound);
            var stats = cache.GameServersStats[gameServerId];
            if (cache.GameServersFirstMatchDate.ContainsKey(gameServerId))
                stats.CalculateAverageData(cache.GameServersFirstMatchDate[gameServerId], cache.LastMatchDate);
            var json = stats.SerializeForGetResponse();
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public HttpResponse HandleRecentMatchesRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            var count = Extensions.StringCountToInt(RecentMatchesPath.Match(request.Uri).Groups["count"].ToString());
            var matches = cache.GetRecentMatches(count);
            var json = JsonConvert.SerializeObject(matches, Formatting.Indented, Serializable.Settings);
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public HttpResponse HandleBestPlayersRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            var count = Extensions.StringCountToInt(BestPlayersPath.Match(request.Uri).Groups["count"].ToString());
            var players = cache.GetTopPlayers(count);
            var json = Extensions.SerializeTopPlayers(players);
            return new HttpResponse(HttpResponse.Status.OK, json);
        }

        public HttpResponse HandlePopularServersRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Status.MethodNotAllowed);
            var count = Extensions.StringCountToInt(BestPlayersPath.Match(request.Uri).Groups["count"].ToString());
            var servers = cache.GetPopularServers(count);
            var json = Extensions.SerializePopularServers(servers);
            return new HttpResponse(HttpResponse.Status.OK, json);
        }
    }
}
