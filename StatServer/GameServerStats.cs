using System;
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
        public Dictionary<string, int> PlayedGameModes { get; }
        public Dictionary<string, int> PlayedMaps { get; }

        public GameServerStats(string endpoint, string name, int totalMatchesPlayed,
            int maximumPopulation, int totalPopulation, string encodedGameModes, string encodedMaps)
        {
            Endpoint = endpoint;
            Name = name;
            TotalMatchesPlayed = totalMatchesPlayed;
            MaximumPopulation = maximumPopulation;
            TotalPopulation = totalPopulation;
            PlayedGameModes = Extensions.DecodeElements(encodedGameModes);
            PlayedMaps = Extensions.DecodeElements(encodedMaps);
            Top5Maps = GetTop5(PlayedMaps);
            Top5GameModes = GetTop5(PlayedGameModes);
        }

        public GameServerStats(string serverId, string name)
        {
            Endpoint = serverId;
            Name = name;
            PlayedGameModes = new Dictionary<string, int>();
            PlayedMaps = new Dictionary<string, int>();
        }

        public void CalculateAverageData(DateTime firstMatchDate, DateTime lastMatchDate)
        {
            AverageMatchesPerDay = Extensions.CalculateAverage(TotalMatchesPlayed, firstMatchDate, lastMatchDate);
            AveragePopulation = Extensions.CalculateAverage(TotalPopulation, firstMatchDate, lastMatchDate);
        }

        public void Update(GameMatchResult match, Dictionary<DateTime, int> matchesPerDay)
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
    }
}
