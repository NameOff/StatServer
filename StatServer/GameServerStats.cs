using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace StatServer
{
    public class GameServerStats : Serializable
    {
        public enum Field
        {
            Endpoint, Name, TotalMatchesPlayed, MaximumMatchesPerDay,
            AverageMatchesPerDay, MaximumPopulation, AveragePopulation,
            Top5GameModes, Top5Maps, Info
        }

        public string Endpoint { get; set; }
        public string Name { get; set; }
        public int TotalMatchesPlayed { get; set; }
        public int MaximumMatchesPerDay { get; set; }
        public double AverageMatchesPerDay { get; set; }
        public int MaximumPopulation { get; set; }
        public double AveragePopulation { get; set; }
        public string[] Top5GameModes { get; set; }
        public string[] Top5Maps { get; set; }
        public GameServerInfo Info { get; set; }

        public int TotalPopulation { get; set; }
        public Dictionary<string, int> PlayedGameModes { get; set; }
        public Dictionary<string, int> PlayedMaps { get; set; }

        public GameServerStats(string endpoint, string name, int totalMatchesPlayed,
            int maximumPopulation, int totalPopulation, Dictionary<string, int> modes, Dictionary<string, int> maps)
        {
            Endpoint = endpoint;
            Name = name;
            TotalMatchesPlayed = totalMatchesPlayed;
            MaximumPopulation = maximumPopulation;
            TotalPopulation = totalPopulation;
            PlayedGameModes = modes;
            PlayedMaps = maps;
            Top5Maps = GetTop5(PlayedMaps);
            Top5GameModes = GetTop5(PlayedGameModes);
        }

        public GameServerStats(string serverId, string name, double averageMatchesPerDay = 0)
        {
            Endpoint = serverId;
            Name = name;
            PlayedGameModes = new Dictionary<string, int>();
            PlayedMaps = new Dictionary<string, int>();
            AverageMatchesPerDay = averageMatchesPerDay;
        }

        public GameServerStats()
        {
            
        }

        public void CalculateAverageData(DateTime firstMatchDate, DateTime lastMatchDate)
        {
            AverageMatchesPerDay = Extensions.CalculateAverage(TotalMatchesPlayed, firstMatchDate, lastMatchDate);
            AveragePopulation = Extensions.CalculateAverage(TotalPopulation, firstMatchDate, lastMatchDate);
        }

        public void Update(GameMatchResult match, ConcurrentDictionary<DateTime, int> matchesPerDay)
        {
            TotalMatchesPlayed++;
            var date = match.Timestamp.Date;
            lock (matchesPerDay)
            {
                matchesPerDay[date] = matchesPerDay.ContainsKey(date) ? matchesPerDay[date] + 1 : 1;
                MaximumMatchesPerDay = matchesPerDay.Values.DefaultIfEmpty().Max();
            }
            var population = match.Results.Scoreboard.Length;
            MaximumPopulation = MaximumPopulation < population ? population : MaximumPopulation;
            var mode = match.Results.GameMode;
            var map = match.Results.Map;
            PlayedGameModes[mode] = PlayedGameModes.ContainsKey(mode) ? PlayedGameModes[mode] + 1 : 1;
            PlayedMaps[map] = PlayedMaps.ContainsKey(map) ? PlayedMaps[map] + 1 : 1;
            Top5GameModes = GetTop5(PlayedGameModes);
            Top5Maps = GetTop5(PlayedMaps);
            TotalPopulation += population;
        }

        private string[] GetTop5(Dictionary<string, int> played)
        {
            return played.Keys
                .OrderByDescending(key => played[key])
                .Take(5)
                .ToArray();
        }

        public string Serialize(params Field[] fields)
        {
            return Serialize(this, fields.Select(field => field.ToString()).ToArray());
        }

        public string SerializeForGetResponse()
        {
            return Serialize(Field.TotalMatchesPlayed, Field.MaximumMatchesPerDay, Field.AverageMatchesPerDay, 
                Field.MaximumPopulation, Field.AveragePopulation, Field.Top5GameModes, Field.Top5Maps);
        }
    }
}
