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

        private readonly Cache cache;
        private readonly Database database;

        public Processor()
        {
            database = new Database();
            cache = database.CreateCache();
            MethodByPattern = CreateMethodByPattern();
            Console.WriteLine("OK. Start!");
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
                    var info = JsonConvert.DeserializeObject<GameServerInfo>(request.Json);
                    database.InsertServerInformation(info, gameServerId);
                    cache.gameServersInformation[gameServerId] = database.GetRowsCount(Database.Table.ServersInformation);
                    return new HttpResponse(HttpResponse.Answer.OK);
                }

                if (request.Method == HttpMethod.Get)
                {
                    if (!cache.gameServersInformation.ContainsKey(gameServerId))
                        return new HttpResponse(HttpResponse.Answer.NotFound);
                    var info = database.GetServerInformation(cache.gameServersInformation[gameServerId]);
                    return new HttpResponse(HttpResponse.Answer.OK,
                        JsonConvert.SerializeObject(info, Formatting.Indented, Serializable.Settings));
                }
            }
            return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
        }

        private void UpdateLastDate(DateTime date)
        {
            if (cache.lastMatchDate == default(DateTime) || date > cache.lastMatchDate)
                cache.lastMatchDate = date;
        }

        public HttpResponse HandleGameMatchStatsRequest(HttpRequest request)
        {
            var gameServerId = gameMatchStatsPath.Match(request.Uri).Groups["gameServerId"].ToString();
            var timestamp = gameMatchStatsPath.Match(request.Uri).Groups["timestamp"].ToString();
            if (!cache.gameServersInformation.ContainsKey(gameServerId))
                return new HttpResponse(HttpResponse.Answer.BadRequest);
            var date = Extensions.ParseTimestamp(timestamp);
            var matchInfo = new GameMatchResult(gameServerId, timestamp);
            if (request.Method == HttpMethod.Put)
            {
                PutGameMatchResult(request.Json, matchInfo, date);
                return new HttpResponse(HttpResponse.Answer.OK);
            }

            if (request.Method == HttpMethod.Get)
            {
                if (!cache.gameMatches.ContainsKey(matchInfo))
                    return new HttpResponse(HttpResponse.Answer.NotFound);
                var stats = database.GetGameMatchStats(cache.gameMatches[matchInfo]);
                var json = JsonConvert.SerializeObject(stats, Formatting.Indented, Serializable.Settings);
                return new HttpResponse(HttpResponse.Answer.OK, json);
            }

            return new HttpResponse(HttpResponse.Answer.MethodNotAllowed);
        }

        private void PutGameMatchResult(string json, GameMatchResult matchInfo, DateTime date)
        {
            UpdateLastDate(date);
            var matchStats = JsonConvert.DeserializeObject<GameMatchStats>(json);
            matchInfo.Results = matchStats;
            var id = database.InsertGameMatchStats(matchInfo);
            cache.gameMatches[matchInfo] = id;
            foreach (var player in matchStats.Scoreboard)
                AddOrUpdatePlayersStats(player.Name, matchInfo);
            var serverName = database.GetServerInformation(cache.gameServersInformation[matchInfo.Server]).Name;
            AddOrUpdateGameServerStats(serverName, matchInfo);
        }

        private void AddOrUpdateGameServerStats(string name, GameMatchResult matchResult)
        {
            var gameServerId = matchResult.Server;
            var stats = cache.gameServersStats.ContainsKey(gameServerId)
                ? database.GetGameServerStats(cache.gameServersStats[gameServerId])
                : new GameServerStats(gameServerId, name);
            stats.Update(matchResult, cache.gameServersMatchesPerDay[gameServerId]);

            if (cache.gameServersStats.ContainsKey(gameServerId))
            {
                var id = cache.gameServersStats[gameServerId];
                database.UpdateGameServerStats(id, stats);
            }
            else
            {
                var id = database.InsertGameServerStats(stats);
                cache.gameServersStats[gameServerId] = id;
            }
        }

        private void AddOrUpdatePlayersStats(string name, GameMatchResult matchResult)
        {
            var stats = cache.playersStats.ContainsKey(name) ? database.GetPlayerStats(cache.playersStats[name]) : new PlayerStats(name);
            stats.UpdateStats(matchResult, cache.playersMatchesPerDay[name]);
            if (cache.playersStats.ContainsKey(name))
            {
                var id = cache.playersStats[name];
                database.UpdatePlayerStats(id, stats);
            }
            else
            {
                var id = database.InsertPlayerStats(stats);
                cache.playersStats[name] = id;
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
            if (!cache.players.ContainsKey(name))
                return new HttpResponse(HttpResponse.Answer.NotFound);
            var stats = database.GetPlayerStats(cache.players[name]);
            stats.CalculateAverageData(cache.playersFirstMatchDate[name], cache.lastMatchDate);
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
            if (!cache.gameServersStats.ContainsKey(gameServerId))
                return new HttpResponse(HttpResponse.Answer.NotFound);
            var stats = database.GetGameServerStats(cache.gameServersStats[gameServerId]);
            stats.CalculateAverageData(cache.gameServersFirstMatchDate[gameServerId], cache.lastMatchDate);
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
