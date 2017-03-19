using System;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;

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

        [JsonIgnore]
        public int TotalPopulation { get; set; }
        [JsonIgnore]
        public ConcurrentDictionary<string, int> PlayedGameModes { get; set; }
        [JsonIgnore]
        public ConcurrentDictionary<string, int> PlayedMaps { get; set; }

        public GameServerStats(string endpoint, string name, double averageMatchesPerDay = 0)
        {
            Endpoint = endpoint;
            Name = name;
            PlayedGameModes = new ConcurrentDictionary<string, int>();
            PlayedMaps = new ConcurrentDictionary<string, int>();
            AverageMatchesPerDay = averageMatchesPerDay;
            Top5Maps = new string[0];
            Top5GameModes = new string[0];
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
            MaximumMatchesPerDay = matchesPerDay.Values.DefaultIfEmpty().Max();
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

        private string[] GetTop5(ConcurrentDictionary<string, int> played)
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
