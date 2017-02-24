using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace StatServer
{
    public class Database
    {
        public const string DatabaseName = "statistics.sqlite";

        public enum Table
        {
            ServersInformation,
            GameMatchPlayersResults,
            GameMatchStats
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
                [Table.GameMatchStats] =
                new[] { "map", "game_mode", "frag_limit", "time_limit", "time_elapsed", "scoreboard" }
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

        private string[] GetTableRowById(Table table, int id)
        {
            var command = $"SELECT * FROM {table} WHERE id = {id};";
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
                for (int id = 1; id <= rowsCount; id++)
                {
                    var command = $"SELECT * FROM {table} WHERE id = {id};";
                    var cmd = new SQLiteCommand(command, connection);
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        yield return GetValuesFrom(reader);
                    }
                }
            }
        }

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
	                    'scoreboard' TEXT NOT NULL
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
                values[i] = $"'{values[i]}'";
            var valuesString = string.Join(", ", values);

            return Tuple.Create(fields, valuesString);
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

        public Dictionary<string, int> GetGameServersDictionary()
        {
            var gameServers = new Dictionary<string, int>();
            var rows = GetAllRows(Table.ServersInformation);
            foreach (var row in rows)
                gameServers[row[3]] = int.Parse(row[0]);
            return gameServers;
        }

        public GameServerInfo GetServerInformation(int id)
        {
            var values = GetTableRowById(Table.ServersInformation, id);
            return new GameServerInfo(values[1], values[2]);
        }

        public void InsertGameMatchStats(GameMatchStats stats)
        {
            var indeces = new int[stats.Scoreboard.Length];
            for (var i = 0; i < stats.Scoreboard.Length; i++)
            {
                AddToTableGameMatchPlayersResults(stats.Scoreboard[i]);
                indeces[i] = tableRowsCount[Table.GameMatchPlayersResults];
            }
            InsertInto(Table.GameMatchStats, stats.Map, stats.GameMode, stats.FragLimit, 
                stats.TimeLimit, stats.TimeElapsed, string.Join(", ", indeces));
        }

        private void AddToTableGameMatchPlayersResults(PlayerInfo info)
        {
            InsertInto(Table.GameMatchPlayersResults, info.Name, info.Frags, info.Kills, info.Deaths);
        }
    }
}
