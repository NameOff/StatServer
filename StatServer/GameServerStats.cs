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

        private Dictionary<string, int> PlayedGameModes { get; }
        private Dictionary<string, int> PlayedMaps { get; }

        public GameServerStats(string endpoint, string name, int totalMatchesPlayed,
            int maximumMatchesPerDay, int maximumPopulation, string encodedGameModes, string encodedMaps)
        {
            Endpoint = endpoint;
            Name = name;
            TotalMatchesPlayed = totalMatchesPlayed;
            MaximumMatchesPerDay = maximumMatchesPerDay;
            MaximumPopulation = maximumPopulation;
            PlayedGameModes = Extensions.DecodeElements(encodedGameModes);
            PlayedMaps = Extensions.DecodeElements(encodedMaps);
            Top5Maps = GetTop5(PlayedMaps);
            Top5GameModes = GetTop5(PlayedGameModes);
        }

        public void CalculateAverageData(DateTime firstMatchDate, DateTime lastMatchDate)
        {
            AverageMatchesPerDay = Extensions.CalculateAverage(MaximumMatchesPerDay, firstMatchDate, lastMatchDate);
            AveragePopulation = Extensions.CalculateAverage(MaximumPopulation, firstMatchDate, lastMatchDate);
        }

        private string[] GetTop5(Dictionary<string, int> played)
        {
            return played.Keys
                .OrderByDescending(key => played[key])
                .Take(5)
                .ToArray();
        }

        public string EncodeGameModes()
        {
            return Extensions.EncodeElements(PlayedGameModes);
        }

        public string EncodeMaps()
        {
            return Extensions.EncodeElements(PlayedMaps);
        }

        public string Serialize(params Field[] fields)
        {
            return Serialize(this, fields.Select(field => field.ToString()).ToArray());
        }
    }
}
