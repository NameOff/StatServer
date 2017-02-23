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

        private Dictionary<Table, string[]> fields;
        private Dictionary<Table, int> rowsCount;

        public Database()
        {
            Initialize();
            fields = new Dictionary<Table, string[]>
            {
                [Table.ServersInformation] = new[] { "name", "game_modes" },
                [Table.GameMatchPlayersResults] = new[] { "name", "frags", "kills", "deaths" },
                [Table.GameMatchStats] =
                new[] { "map", "game_mode", "frag_limit", "time_limit", "time_elapsed", "scoreboard" }
            };
            rowsCount = new Dictionary<Table, int>();
            foreach (var table in (Table[])Enum.GetValues(typeof(Table)))
                rowsCount[table] = CalculateTableRowsCount(table);
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


        private string CreateInsertQuery(Table table, params object[] values)
        {
            var fieldsAndValues = FieldsAndValuesToString(table, values);
            return $"INSERT INTO {table} ({fieldsAndValues.Item1}), ({fieldsAndValues.Item2});";
        }

        private string CreateUpdateQuery(Table table, int id, string field, object newValue)
        {
            return $"UPDATE {table} SET {field} = {newValue} WHERE id = {id}";
        }

        private Tuple<string, string> FieldsAndValuesToString(Table table, params object[] values)
        {
            var fields = string.Join(", ", this.fields[table]);
            for (var i = 0; i < values.Length; i++)
                values[i] = $"'{values[i]}'";
            var valuesString = string.Join(", ", values);

            return Tuple.Create(fields, valuesString);
        }

        private void InsertInto(Table table, params object[] values)
        {
            var command = CreateInsertQuery(table, values);
            ExecuteQuery(command);
            rowsCount[table]++;
        }

        public void AddToTableServersInformation(GameServerInfo info)
        {
            InsertInto(Table.ServersInformation, info.Name, info.EncodeGameModes());
        }

        public void AddToTableGameMatchStats(GameMatchStats stats)
        {
            var indeces = new int[stats.Scoreboard.Length];
            for (var i = 0; i < stats.Scoreboard.Length; i++)
            {
                AddToTableGameMatchPlayersResults(stats.Scoreboard[i]);
                indeces[i] = rowsCount[Table.GameMatchPlayersResults];
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
