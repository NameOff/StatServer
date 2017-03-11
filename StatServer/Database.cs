using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace StatServer
{
    public class Database
    {
        public const string DatabaseName = "statistics.sqlite";

        public enum Table
        {
            ServersInformation,
            GameMatchPlayersResults,
            GameMatchStats,
            GameServersStats,
            PlayersStats
        }

        private readonly Dictionary<Table, string[]> tableFields;
        private readonly Dictionary<Table, int> tableRowsCount;
        public SQLiteConnection Connection;



        public Database()
        {
            Initialize();
            tableFields = new Dictionary<Table, string[]>
            {
                [Table.ServersInformation] = new[] { "name", "game_modes", "endpoint" },
                [Table.GameMatchPlayersResults] = new[] { "name", "frags", "kills", "deaths" },
                [Table.GameMatchStats] = new[] { "map", "game_mode", "frag_limit", "time_limit",
                    "time_elapsed", "scoreboard", "server", "timestamp" },
                [Table.GameServersStats] = new[] { "endpoint", "name" , "total_matches_played",
                    "maximum_population", "total_population", "game_modes", "maps" },
                [Table.PlayersStats] = new[] { "name", "total_matches_played", "total_matches_won", "servers",
                    "game_modes", "average_scoreboard_percent", "last_match_played", "total_kills", "total_deaths" }
            };

            tableRowsCount = new Dictionary<Table, int>();
            foreach (var table in (Table[])Enum.GetValues(typeof(Table)))
                tableRowsCount[table] = CalculateTableRowsCount(table);
        }

        private void Initialize()
        {
            if (File.Exists(DatabaseName))
            {
                Connection = new SQLiteConnection($"Data Source = {DatabaseName}; Version=3;");
                Connection.Open();
                return;
            }

            SQLiteConnection.CreateFile(DatabaseName);
            Connection = new SQLiteConnection($"Data Source = {DatabaseName}; Version=3;");
            Connection.Open();
            CreateAllTables();
        }

        public Dictionary<string, int> CreatePlayersStatsDictionary()
        {
            var playersStats = new Dictionary<string, int>();
            var rows = GetAllRows(Table.PlayersStats);
            foreach (var row in rows)
                playersStats[row[1]] = int.Parse(row[0]);
            return playersStats;
        }

        private void ExecuteQuery(params string[] commands)
        {
            foreach (var command in commands)
                new SQLiteCommand(command, Connection).ExecuteNonQuery();
        }

        public Dictionary<string, int> CreateGameServersStatsDictionary()
        {
            var rows = GetAllRows(Table.GameServersStats);
            var result = new Dictionary<string, int>();
            foreach (var row in rows)
                result[row[1]] = int.Parse(row[0]);
            return result;
        }

        public void SetDateTimeDictionaries(Cache cache)
        {
            var rows = GetAllRows(Table.GameMatchStats);
            foreach (var row in rows)
            {
                var server = row[7];
                var date = Extensions.ParseTimestamp(row[8]);
                var match = ParseGameMatchStats(row);
                cache.RecentMatches.Add(new GameMatchResult(server, date) { Results = match });
                if (cache.RecentMatches.Count >= cache.MaxCount * 10)
                    cache.UpdateRecentMatches();
                if (!cache.GameServersFirstMatchDate.ContainsKey(server) || cache.GameServersFirstMatchDate[server] > date)
                    cache.GameServersFirstMatchDate[server] = date;
                var ids = ParseIds(row[6]);
                var players = GetPlayerInfo(ids);
                foreach (var player in players)
                {
                    if (!cache.PlayersFirstMatchDate.ContainsKey(player.Name) || cache.PlayersFirstMatchDate[player.Name] > date)
                        cache.PlayersFirstMatchDate[player.Name] = date;
                    if (!cache.PlayersMatchesPerDay.ContainsKey(player.Name))
                        cache.PlayersMatchesPerDay[player.Name] = new Dictionary<DateTime, int>();
                    var playerMatches = cache.PlayersMatchesPerDay[player.Name];
                    playerMatches[date] = playerMatches.ContainsKey(date) ? playerMatches[date] + 1 : 1;
                }
                if (!cache.GameServersMatchesPerDay.ContainsKey(server))
                    cache.GameServersMatchesPerDay[server] = new Dictionary<DateTime, int>();
                var gameServerMatches = cache.GameServersMatchesPerDay[server];
                gameServerMatches[date] = gameServerMatches.ContainsKey(date) ? gameServerMatches[date] + 1 : 1;
            }
            cache.UpdateRecentMatches();
        }

        public Dictionary<string, DateTime> CreatePlayersFirstMatchDate()
        {
            var result = new Dictionary<string, DateTime>();
            var rows = GetAllRows(Table.GameMatchStats);
            foreach (var row in rows)
            {
                var ids = ParseIds(row[6]);
                var players = GetPlayerInfo(ids);
                var date = Extensions.ParseTimestamp(row[8]);
                foreach (var player in players)
                {
                    if (!result.ContainsKey(player.Name) || result[player.Name] > date)
                        result[player.Name] = date;
                }
            }
            return result;
        }

        private string[] GetTableRowById(Table table, int id)
        {
            var command = CreateSelectRowRequest(table, id);
            var cmd = new SQLiteCommand(command, Connection);
            using (var reader = cmd.ExecuteReader())
            {
                reader.Read();
                return GetValuesFrom(reader);
            }

        }

        public IEnumerable<string[]> GetAllRows(Table table)
        {
            var rowsCount = GetRowsCount(table);
            for (var id = 1; id <= rowsCount; id++)
            {
                var command = CreateSelectRowRequest(table, id);
                var cmd = new SQLiteCommand(command, Connection);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    yield return GetValuesFrom(reader);
                }
            }

        }

        private string CreateSelectRowRequest(Table table, int id) => $"SELECT * FROM {table} WHERE id = {id};";

        private string[] GetValuesFrom(SQLiteDataReader reader)
        {
            var values = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
                values[i] = reader.GetValue(i).ToString();
            return values;
        }
        public int GetRowsCount(Table table)
        {
            return tableRowsCount[table];
        }

        private int CalculateTableRowsCount(Table table)
        {
            var command = $"SELECT COUNT(id) FROM {table}";
            return int.Parse(new SQLiteCommand(command, Connection).ExecuteScalar().ToString());

        }

        private void CreateAllTables()
        {
            var commands = new[]
            {
                @"CREATE TABLE 'ServersInformation'
                    (
	                    'id' INTEGER PRIMARY KEY AUTOINCREMENT,
	                    'name' TEXT NOT NULL,
	                    'game_modes' TEXT NOT NULL,
                        'endpoint' TEXT NOT NULL
                    )",
                @"CREATE TABLE 'GameMatchPlayersResults'
                    (
	                    'id' INTEGER PRIMARY KEY AUTOINCREMENT,
	                    'name' TEXT NOT NULL,
	                    'frags' INTEGER,
	                    'kills' INTEGER,
	                    'deaths' INTEGER
                    )",
                @"CREATE TABLE 'GameMatchStats'
                    (
	                    'id' INTEGER PRIMARY KEY AUTOINCREMENT,
	                    'map' TEXT NOT NULL,
	                    'game_mode' TEXT NOT NULL,
	                    'frag_limit' INTEGER,
	                    'time_limit' INTEGER,
	                    'time_elapsed' REAL,
	                    'scoreboard' TEXT NOT NULL,
                        'server' TEXT NOT NULL,
                        'timestamp' TEXT NOT NULL
                    )",
                @"CREATE TABLE 'GameServersStats'
                    (
                        'id' INTEGER PRIMARY KEY AUTOINCREMENT,
                        'endpoint' TEXT NOT NULL,
                        'name' TEXT NOT NULL,
                        'total_matches_played' INTEGER,
                        'maximum_population' INTEGER,
                        'total_population' INTEGER,
                        'game_modes' TEXT NOT NULL,
                        'maps' TEXT NOT NULL
                    )",
                @"CREATE TABLE 'PlayersStats'
                    (
                        'id' INTEGER PRIMARY KEY AUTOINCREMENT,
                        'name' TEXT NOT NULL,
                        'total_matches_played' INTEGER,
                        'total_matches_won' INTEGER,
                        'servers' TEXT NOT NULL,
                        'game_modes' TEXT NOT NULL,
                        'average_scoreboard_percent' REAL,
                        'last_match_played' TEXT NOT NULL,
                        'total_kills' INTEGER,
                        'total_deaths' INTEGER
                    )"
            };
            ExecuteQuery(commands);
        }



        private string CreateInsertQuery(Table table, params object[] values)
        {
            var fieldsAndValues = FieldsAndValuesToString(table, values);
            return $"INSERT INTO {table} ({fieldsAndValues.Item1}) VALUES ({fieldsAndValues.Item2});";
        }

        private string CreateUpdateQuery(Table table, int id, params Tuple<string, object>[] newValues)
        {
            var fields = newValues.Select(tuple => $"{tuple.Item1} = {Extensions.ObjectToString(tuple.Item2)}");
            return $"UPDATE {table} SET {string.Join(", ", fields)} WHERE id = {id}";
        }

        private Tuple<string, string> FieldsAndValuesToString(Table table, params object[] values)
        {
            var fields = string.Join(", ", tableFields[table]);
            for (var i = 0; i < values.Length; i++)
                values[i] = Extensions.ObjectToString(values[i]);
            var valuesString = string.Join(", ", values);

            return Tuple.Create(fields, valuesString);
        }

        public void UpdatePlayerStats(int id, PlayerStats stats)
        {
            var fields = new[]
            {
                Tuple.Create("name", (object)stats.Name), Tuple.Create("total_matches_played", (object)stats.TotalMatchesPlayed),
                Tuple.Create("total_matches_won", (object)stats.TotalMatchesWon),
                Tuple.Create("servers", (object)Extensions.EncodeElements(stats.PlayedServers)),
                Tuple.Create("game_modes", (object)Extensions.EncodeElements(stats.PlayedModes)),
                Tuple.Create("average_scoreboard_percent", (object)stats.AverageScoreboardPercent),
                Tuple.Create("last_match_played", (object)stats.LastMatchPlayed),
                Tuple.Create("total_kills", (object)stats.TotalKills),
                Tuple.Create("total_deaths", (object)stats.TotalDeaths)
            };
            var cmd = CreateUpdateQuery(Table.PlayersStats, id, fields);
            ExecuteQuery(cmd);
        }

        public void UpdateGameServerStats(int id, GameServerStats stats)
        {
            var fields = new[]
            {
                Tuple.Create("endpoint", (object) stats.Endpoint), Tuple.Create("name", (object) stats.Name),
                Tuple.Create("total_matches_played", (object) stats.TotalMatchesPlayed),
                Tuple.Create("maximum_population", (object) stats.MaximumPopulation),
                Tuple.Create("total_population", (object) stats.TotalPopulation),
                Tuple.Create("game_modes", (object) Extensions.EncodeElements(stats.PlayedGameModes)),
                Tuple.Create("maps", (object) Extensions.EncodeElements(stats.PlayedMaps))
            };
            var cmd = CreateUpdateQuery(Table.GameServersStats, id, fields);
            ExecuteQuery(cmd);
        }

        public void UpdateGameServerInfo(int id, GameServerInfo info, string endpoint)
        {
            var fields = new[]
            {
                Tuple.Create("name", (object) info.Name),
                Tuple.Create("game_modes", (object) string.Join(", ", info.GameModes)),
                Tuple.Create("endpoint", (object) endpoint)
            };
            var cmd = CreateUpdateQuery(Table.ServersInformation, id, fields);
            ExecuteQuery(cmd);
        }

        private void InsertInto(Table table, params object[] values)
        {
            var command = CreateInsertQuery(table, values);
            ExecuteQuery(command);
            tableRowsCount[table]++;
        }

        public int InsertServerInformation(GameServerInfo info, string endpoint)
        {
            InsertInto(Table.ServersInformation, info.Name, info.EncodeGameModes(), endpoint);
            return tableRowsCount[Table.ServersInformation];
        }

        public Dictionary<string, int> CreateGameServersDictionary()
        {
            var gameServers = new Dictionary<string, int>();
            var rows = GetAllRows(Table.ServersInformation);
            foreach (var row in rows)
                gameServers[row[3]] = int.Parse(row[0]);
            return gameServers;
        }

        public Dictionary<GameMatchResult, int> CreateGameMatchDictionary()
        {
            var gameMatches = new Dictionary<GameMatchResult, int>();
            var rows = GetAllRows(Table.GameMatchStats);
            foreach (var row in rows)
            {
                var server = row[7];
                var timestamp = row[8];
                var id = int.Parse(row[0]);
                gameMatches[new GameMatchResult(server, timestamp)] = id;
            }
            return gameMatches;
        }

        public GameServerInfo GetServerInformation(int id)
        {
            var values = GetTableRowById(Table.ServersInformation, id);
            return new GameServerInfo(values[1], values[2]);
        }

        public IEnumerable<GameServerInfoResponse> GetAllGameServerInformation()
        {
            var rows = GetAllRows(Table.ServersInformation);
            foreach (var row in rows)
                yield return new GameServerInfoResponse(row[3], new GameServerInfo(row[1], row[2]));
        }

        public int InsertGameMatchStats(GameMatchResult info)
        {
            var indeces = new int[info.Results.Scoreboard.Length];
            for (var i = 0; i < info.Results.Scoreboard.Length; i++)
            {
                AddToTableGameMatchPlayersResults(info.Results.Scoreboard[i]);
                indeces[i] = tableRowsCount[Table.GameMatchPlayersResults];
            }
            InsertInto(Table.GameMatchStats, info.Results.Map, info.Results.GameMode, info.Results.FragLimit,
                info.Results.TimeLimit, info.Results.TimeElapsed, string.Join(", ", indeces), info.Server, info.Timestamp);
            return tableRowsCount[Table.GameMatchStats];
        }

        private PlayerInfo GetPlayerInformation(int id)
        {
            var data = GetTableRowById(Table.GameMatchPlayersResults, id);
            return new PlayerInfo(data[1], int.Parse(data[2]), int.Parse(data[3]), int.Parse(data[4]));
        }

        public int InsertGameServerStats(GameServerStats stats)
        {
            InsertInto(Table.GameServersStats, stats.Endpoint, stats.Name, stats.TotalMatchesPlayed,
                stats.MaximumPopulation, stats.TotalPopulation, Extensions.EncodeElements(stats.PlayedGameModes),
                Extensions.EncodeElements(stats.PlayedMaps));
            return tableRowsCount[Table.GameServersStats];
        }

        public GameServerStats GetGameServerStats(int id)
        {
            var row = GetTableRowById(Table.GameServersStats, id);
            return new GameServerStats(row[1], row[2], int.Parse(row[3]),
                int.Parse(row[4]), int.Parse(row[5]), row[6], row[7]);
        }

        public PlayerStats GetPlayerStats(int id)
        {
            var row = GetTableRowById(Table.PlayersStats, id);
            return new PlayerStats(row[1], int.Parse(row[2]), int.Parse(row[3]),
                Extensions.DecodeElements(row[4]), Extensions.DecodeElements(row[5]),
                double.Parse(row[6]), Extensions.ParseTimestamp(row[7]), int.Parse(row[8]), int.Parse(row[9]));
        }

        public int InsertPlayerStats(PlayerStats stats)
        {
            InsertInto(Table.PlayersStats, stats.Name, stats.TotalMatchesPlayed, stats.TotalMatchesWon,
                Extensions.EncodeElements(stats.PlayedServers), Extensions.EncodeElements(stats.PlayedModes),
                stats.AverageScoreboardPercent, stats.LastMatchPlayed, stats.TotalKills, stats.TotalDeaths);
            return tableRowsCount[Table.PlayersStats];
        }

        public GameMatchStats GetGameMatchStats(int id)
        {
            var data = GetTableRowById(Table.GameMatchStats, id);
            return ParseGameMatchStats(data);
        }

        private GameMatchStats ParseGameMatchStats(string[] data)
        {
            var scoreboard = GetPlayerInfo(ParseIds(data[6]));
            return new GameMatchStats(data[1], data[2], int.Parse(data[3]),
                int.Parse(data[4]), double.Parse(data[5]), scoreboard);
        }

        private IEnumerable<int> ParseIds(string ids)
        {
            return ids
                .Split(',')
                .Select(int.Parse);
        }

        private PlayerInfo[] GetPlayerInfo(IEnumerable<int> id)
        {
            return id
                .Select(GetPlayerInformation)
                .ToArray();
        }

        private void AddToTableGameMatchPlayersResults(PlayerInfo info)
        {
            InsertInto(Table.GameMatchPlayersResults, info.Name, info.Frags, info.Kills, info.Deaths);
        }
    }
}
