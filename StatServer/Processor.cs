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
        public const int MaxCount = 50;
        public const int MinCount = 0;
        public const int DefaultCount = 5;

        private readonly Regex gameServerInfoPath = new Regex(@"^/servers/(?<gameServerId>\S*?)/info$", RegexOptions.Compiled);
        private readonly Regex gameServerStatsPath = new Regex(@"^/servers/(?<gameServerId>\S*?)/stats$", RegexOptions.Compiled);
        private readonly Regex gameMatchStatsPath = new Regex(@"^/servers/(?<gameServerId>\S*?)/matches/(?<timestamp>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)", RegexOptions.Compiled);
        private readonly Regex allGameServersInfoPath = new Regex(@"^/servers/info$", RegexOptions.Compiled);
        private readonly Regex playerStatsPath = new Regex(@"^/players/(?<playerName>\S*?)/stats$", RegexOptions.Compiled);
        private readonly Regex recentMatchesPath = new Regex(@"^/reports/recent-matches(/(?<count>-{0,1}\d{1}))?$", RegexOptions.Compiled);
        private readonly Regex bestPlayersPath = new Regex(@"^/reports/best-players(/(?<count>-{0,1}\d{1}))?$", RegexOptions.Compiled);
        private readonly Regex popularServersPath = new Regex(@"^/reports/popular-servers(/(?<count>-{0,1}\d{1}))?$", RegexOptions.Compiled);
        private readonly Dictionary<Regex, Func<HttpRequest, HttpResponse>> MethodByPattern;

        private readonly Cache cache;
        public readonly Database database;

        public Processor()
        {
            database = new Database();
            cache = new Cache(database, MaxCount);
            MethodByPattern = CreateMethodByPattern();
            Console.WriteLine("OK. Start!");
        }

        private Dictionary<Regex, Func<HttpRequest, HttpResponse>> CreateMethodByPattern()
        {
            return new Dictionary<Regex, Func<HttpRequest, HttpResponse>>
            {
                [gameServerInfoPath] = HandleGameServerInformationRequest, //+
                [gameMatchStatsPath] = HandleGameMatchStatsRequest, //  +
                [gameServerStatsPath] = HandleGameServerStatsRequest, //+
                [allGameServersInfoPath] = HandleAllGameServersInfoRequest, //+
                [playerStatsPath] = HandlePlayerStatsRequest, //+
                [recentMatchesPath] = HandleRecentMatchesRequest, //+
                [bestPlayersPath] = HandleBestPlayersRequest, //-
                [popularServersPath] = HandlePopularServersRequest //-
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
                return new HttpResponse(HttpResponse.Answer.BadRequest);
            }
            return new HttpResponse(HttpResponse.Answer.NotFound);
        }

        public HttpResponse HandleGameServerInformationRequest(HttpRequest request)
        {
            lock (database)
            {
                var gameServerId = gameServerInfoPath.Match(request.Uri).Groups["gameServerId"].ToString();
                if (request.Method == HttpMethod.Put)
                {
                    var info = JsonConvert.DeserializeObject<GameServerInfo>(request.Json, Serializable.Settings);
                    if (cache.GameServersInformation.ContainsKey(gameServerId))
                        database.UpdateGameServerInfo(cache.GameServersInformation[gameServerId], info, gameServerId);
                    else
                    {
                        var id = database.InsertServerInformation(info, gameServerId);
                        cache.GameServersInformation[gameServerId] = id;
                        var statsId = database.InsertGameServerStats(new GameServerStats(gameServerId, info.Name));
                        cache.GameServersStats[gameServerId] = statsId;
                    }
                    return new HttpResponse(HttpResponse.Answer.OK);
                }

                if (request.Method == HttpMethod.Get)
                {
                    if (!cache.GameServersInformation.ContainsKey(gameServerId))
                        return new HttpResponse(HttpResponse.Answer.NotFound);
                    var info = database.GetServerInformation(cache.GameServersInformation[gameServerId]);
                    return new HttpResponse(HttpResponse.Answer.OK,
                        JsonConvert.SerializeObject(info, Formatting.Indented, Serializable.Settings));
                }
            }
            return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
        }

        private void UpdateLastDate(DateTime date)
        {
            if (cache.LastMatchDate == default(DateTime) || date > cache.LastMatchDate)
                cache.LastMatchDate = date;
        }

        public HttpResponse HandleGameMatchStatsRequest(HttpRequest request)
        {
            var gameServerId = gameMatchStatsPath.Match(request.Uri).Groups["gameServerId"].ToString();
            var timestamp = gameMatchStatsPath.Match(request.Uri).Groups["timestamp"].ToString();
            if (!cache.GameServersInformation.ContainsKey(gameServerId))
                return new HttpResponse(HttpResponse.Answer.BadRequest);
            var date = Extensions.ParseTimestamp(timestamp);
            var matchInfo = new GameMatchResult(gameServerId, timestamp);
            if (request.Method == HttpMethod.Put)
            {
                Stopwatch a = new Stopwatch();
                a.Start();
                PutGameMatchResult(request.Json, matchInfo, date);
                a.Stop();
                Console.WriteLine($"PUT запрос статистики матча. Общее время: {a.Elapsed}");
                return new HttpResponse(HttpResponse.Answer.OK);
            }

            if (request.Method == HttpMethod.Get)
            {
                Stopwatch a = new Stopwatch();
                a.Start();
                if (!cache.GameMatches.ContainsKey(matchInfo))
                    return new HttpResponse(HttpResponse.Answer.NotFound);
                var stats = database.GetGameMatchStats(cache.GameMatches[matchInfo]);
                var json = JsonConvert.SerializeObject(stats, Formatting.Indented, Serializable.Settings);
                a.Stop();
                Console.WriteLine($"GET запрос статистики матча. Общее время: {a.Elapsed}");
                return new HttpResponse(HttpResponse.Answer.OK, json);
            }

            return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
        }

        private void PutGameMatchResult(string json, GameMatchResult matchInfo, DateTime date)
        {
            UpdateLastDate(date);
            var matchStats = JsonConvert.DeserializeObject<GameMatchStats>(json, Serializable.Settings);
            matchInfo.Results = matchStats;
            var id = database.InsertGameMatchStats(matchInfo);
            cache.RecentMatches.Add(matchInfo);
            cache.UpdateRecentMatches();
            cache.GameMatches[matchInfo] = id;
            var a = new Stopwatch();
            a.Start();
            foreach (var player in matchStats.Scoreboard)
                AddOrUpdatePlayersStats(player.Name, matchInfo);
            var serverName = database.GetServerInformation(cache.GameServersInformation[matchInfo.Server]).Name;
            AddOrUpdateGameServerStats(serverName, matchInfo);
            a.Stop();
            Console.WriteLine($"PUT запрос статистики матча. БД: {a.Elapsed}");
        }

        private void AddOrUpdateGameServerStats(string name, GameMatchResult matchResult)
        {
            var gameServerId = matchResult.Server;
            var stats = cache.GameServersStats.ContainsKey(gameServerId)
                ? database.GetGameServerStats(cache.GameServersStats[gameServerId])
                : new GameServerStats(gameServerId, name);

            if (!cache.GameServersMatchesPerDay.ContainsKey(gameServerId))
                cache.GameServersMatchesPerDay[gameServerId] = new Dictionary<DateTime, int>();
            stats.Update(matchResult, cache.GameServersMatchesPerDay[gameServerId]);

            if (cache.GameServersStats.ContainsKey(gameServerId))
            {
                var id = cache.GameServersStats[gameServerId];
                database.UpdateGameServerStats(id, stats);
            }
            else
            {
                var id = database.InsertGameServerStats(stats);
                cache.GameServersStats[gameServerId] = id;
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
            if (!cache.Players.ContainsKey(name))
                return new HttpResponse(HttpResponse.Answer.NotFound);
            var stats = database.GetPlayerStats(cache.Players[name]);
            if (cache.PlayersFirstMatchDate.ContainsKey(name))
                stats.CalculateAverageData(cache.PlayersFirstMatchDate[name], cache.LastMatchDate);
            var json = stats.Serialize(PlayerStats.Field.TotalMatchesPlayed, PlayerStats.Field.TotalMatchesWon,
                PlayerStats.Field.FavoriteServer, PlayerStats.Field.UniqueServers, PlayerStats.Field.FavoriteGameMode,
                PlayerStats.Field.AverageScoreboardPercent, PlayerStats.Field.MaximumMatchesPerDay,
                PlayerStats.Field.AverageMatchesPerDay, PlayerStats.Field.LastMatchPlayed,
                PlayerStats.Field.KillToDeathRatio);
            return new HttpResponse(HttpResponse.Answer.OK, json);
        }

        public HttpResponse HandleGameServerStatsRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
            var gameServerId = gameServerStatsPath.Match(request.Uri).Groups["gameServerId"].ToString();
            if (!cache.GameServersStats.ContainsKey(gameServerId))
                return new HttpResponse(HttpResponse.Answer.NotFound);
            var stats = database.GetGameServerStats(cache.GameServersStats[gameServerId]);
            if (cache.GameServersFirstMatchDate.ContainsKey(gameServerId))
                stats.CalculateAverageData(cache.GameServersFirstMatchDate[gameServerId], cache.LastMatchDate);
            var json = stats.Serialize(GameServerStats.Field.TotalMatchesPlayed,
                GameServerStats.Field.MaximumMatchesPerDay, GameServerStats.Field.AverageMatchesPerDay,
                GameServerStats.Field.MaximumPopulation, GameServerStats.Field.AveragePopulation,
                GameServerStats.Field.Top5GameModes, GameServerStats.Field.Top5Maps);
            return new HttpResponse(HttpResponse.Answer.OK, json);
        }

        public HttpResponse HandleRecentMatchesRequest(HttpRequest request)
        {
            if (request.Method != HttpMethod.Get)
                return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
            var count = StringCountToInt(recentMatchesPath.Match(request.Uri).Groups["count"].ToString());
            var matches = cache.RecentMatches.Take(count).ToArray();
            var json = JsonConvert.SerializeObject(matches, Formatting.Indented, Serializable.Settings);
            return new HttpResponse(HttpResponse.Answer.OK, json);
        }

        private int StringCountToInt(string count)
        {
            int result;
            var isParsed = int.TryParse(count, out result);
            return isParsed ? AdjustCount(result) : DefaultCount;
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

        private static int AdjustCount(int count)
        {
            if (count > MaxCount)
                return MaxCount;
            if (count < MinCount)
                return MinCount;
            return count;
        }
    }
}
