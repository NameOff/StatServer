using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
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
            GameServersStats
        }

        private Dictionary<Table, string[]> tableFields;
        private Dictionary<Table, int> tableRowsCount;

        public Database()
        {
            Initialize();
            tableFields = new Dictionary<Table, string[]>
            {
                [Table.ServersInformation] = new[] { "name", "game_modes", "endpoint" },
                [Table.GameMatchPlayersResults] = new[] { "name", "frags", "kills", "deaths" },
                [Table.GameMatchStats] = new[] { "map", "game_mode", "frag_limit", "time_limit",
                    "time_elapsed", "scoreboard", "server", "timestamp" },
                [Table.GameServersStats] = new[] { "endpoint", "name" , "total_matches_played", "maximum_matches_per_day",
                    "maximum_population", "game_modes", "maps" }
            };
            tableRowsCount = new Dictionary<Table, int>();
            foreach (var table in (Table[])Enum.GetValues(typeof(Table)))
                tableRowsCount[table] = CalculateTableRowsCount(table);
        }

        private void Initialize()
        {
            if (File.Exists(DatabaseName))
                return;

            SQLiteConnection.CreateFile(DatabaseName);
            CreateAllTables();
        }

        private void ExecuteQuery(params string[] commands)
        {
            using (var connection = new SQLiteConnection($"Data Source = {DatabaseName}; Version=3;"))
            {
                connection.Open();
                foreach (var command in commands)
                    new SQLiteCommand(command, connection).ExecuteNonQuery();
            }
        }

        public Dictionary<string, int> CreateGameServersStatsDictionary()
        {
            var rows = GetAllRows(Table.GameServersStats);
            var result = new Dictionary<string, int>();
            foreach (var row in rows)
                result[row[1]] = int.Parse(row[0]);
            return result;
        }

        public Dictionary<string, DateTime> CreateGameServersFirstMatchDate()
        {
            var result = new Dictionary<string, DateTime>();
            var rows = GetAllRows(Table.GameMatchStats);
            foreach (var row in rows)
            {
                var server = row[7];
                var date = Extensions.ParseTimestamp(row[8]);
                if (!result.ContainsKey(server) || result[server] > date)
                    result[server] = date;
            }
            return result;
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
            using (var connection = new SQLiteConnection($"Data Source = {DatabaseName}; Version=3;"))
            {
                connection.Open();
                var cmd = new SQLiteCommand(command, connection);
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return GetValuesFrom(reader);
                }
            }
        }

        public IEnumerable<string[]> GetAllRows(Table table)
        {
            using (var connection = new SQLiteConnection($"Data Source = {DatabaseName}; Version=3;"))
            {
                connection.Open();
                var rowsCount = GetRowsCount(table);
                for (var id = 1; id <= rowsCount; id++)
                {
                    var command = CreateSelectRowRequest(table, id);
                    var cmd = new SQLiteCommand(command, connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        yield return GetValuesFrom(reader);
                    }
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
            using (var connection = new SQLiteConnection($"Data Source = {DatabaseName}; Version=3;"))
            {
                connection.Open();
                return int.Parse(new SQLiteCommand(command, connection).ExecuteScalar().ToString());
            }
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
                        'maximum_matches_per_day' INTEGER,
                        'maximum_population' INTEGER,
                        'game_modes' TEXT NOT NULL,
                        'maps' TEXT NOT NULL
                    )"
            };
            ExecuteQuery(commands);
        }



        private string CreateInsertQuery(Table table, params object[] values)
        {
            var fieldsAndValues = FieldsAndValuesToString(table, values);
            return $"INSERT INTO {table} ({fieldsAndValues.Item1}) VALUES ({fieldsAndValues.Item2});";
        }

        private string CreateUpdateQuery(Table table, int id, string field, object newValue)
        {
            return $"UPDATE {table} SET {field} = {newValue} WHERE id = {id}";
        }

        private Tuple<string, string> FieldsAndValuesToString(Table table, params object[] values)
        {
            var fields = string.Join(", ", tableFields[table]);
            for (var i = 0; i < values.Length; i++)
                values[i] = ObjectToString(values[i]);
            var valuesString = string.Join(", ", values);

            return Tuple.Create(fields, valuesString);
        }

        private string ObjectToString(object obj)
        {
            var nfi = new NumberFormatInfo { NumberDecimalSeparator = "." };
            if (obj is string || obj is DateTime)
                return $"'{obj}'";
            if (obj is double)
                return ((double)obj).ToString(nfi);
            return obj.ToString();
        }

        private void InsertInto(Table table, params object[] values)
        {
            var command = CreateInsertQuery(table, values);
            ExecuteQuery(command);
            tableRowsCount[table]++;
        }

        public void InsertServerInformation(GameServerInfo info, string endpoint)
        {
            InsertInto(Table.ServersInformation, info.Name, info.EncodeGameModes(), endpoint);
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

        public void InsertGameMatchStats(GameMatchResult info)
        {
            var indeces = new int[info.Results.Scoreboard.Length];
            for (var i = 0; i < info.Results.Scoreboard.Length; i++)
            {
                AddToTableGameMatchPlayersResults(info.Results.Scoreboard[i]);
                indeces[i] = tableRowsCount[Table.GameMatchPlayersResults];
            }
            InsertInto(Table.GameMatchStats, info.Results.Map, info.Results.GameMode, info.Results.FragLimit,
                info.Results.TimeLimit, info.Results.TimeElapsed, string.Join(", ", indeces), info.Server, info.Timestamp);
        }

        private PlayerInfo GetPlayerInformation(int id)
        {
            var data = GetTableRowById(Table.GameMatchPlayersResults, id);
            return new PlayerInfo(data[1], int.Parse(data[2]), int.Parse(data[3]), int.Parse(data[4]));
        }

        public GameServerStats GetGameServerStats(int id)
        {
            var row = GetTableRowById(Table.GameServersStats, id);
            return new GameServerStats(row[1], row[2], int.Parse(row[3]), 
                int.Parse(row[4]), int.Parse(row[5]), row[6], row[7]);
        }

        public GameMatchStats GetGameMatchStats(int id)
        {
            var data = GetTableRowById(Table.GameMatchStats, id);
            var scoreboard = GetPlayerInfo(ParseIds(data[6]));
            return new GameMatchStats(data[1], data[2], int.Parse(data[3]), int.Parse(data[4]), double.Parse(data[5]), scoreboard);
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
