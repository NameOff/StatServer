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

        private Dictionary<Table, string[]> Fields;
        private Dictionary<Table, int> RowsCount;

        public Database()
        {
            Initialize();
            Fields = new Dictionary<Table, string[]>
            {
                [Table.ServersInformation] = new[] { "name", "game_modes" },
                [Table.GameMatchPlayersResults] = new[] { "name", "frags", "kills", "deaths" },
                [Table.GameMatchStats] =
                new[] { "map", "game_mode", "frag_limit", "time_limit", "time_elapsed", "scoreboard" }
            };
            RowsCount = new Dictionary<Table, int>();
            foreach (var table in (Table[]) Enum.GetValues(typeof(Table)))
                RowsCount[table] = CalculateTableRowsCount(table);
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

        private string ExecuteQueryAndGetAnswer(string command)
        {
            using (var connection = new SQLiteConnection($"Data Source = {DatabaseName}; Version=3;"))
            {
                connection.Open();
                return new SQLiteCommand(command, connection).ExecuteNonQuery().ToString();
            }
        }

        private int CalculateTableRowsCount(Table table)
        {
            var command = $"SELECT COUNT(id) FROM {table}";
            return int.Parse(ExecuteQueryAndGetAnswer(command));
        }

        private void CreateAllTables()
        {
            var commands = new[]
            {
                @"CREATE TABLE 'ServersInformation'
                    (
	                    'id' INTEGER PRIMARY KEY AUTOINCREMENT,
	                    'name' TEXT NOT NULL,
	                    'game_modes' TEXT NOT NULL
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

        private string CreateInsertQuery(Table tableName, params object[] values)
        {
            return null;
        }

        public int AddToTableServersInformation(GameServerInfo info)
        {
            var command = $"INSERT INTO ServersInformation (name, game_modes) VALUES ('{info.Name}', '{info.EncodeGameModes()}');";
            ExecuteQuery(command);
            return ++RowsCount[Table.ServersInformation];
        }

        public void Add(Table tableName, params object[] values)
        {
            throw new NotImplementedException();
        }
    }
}
