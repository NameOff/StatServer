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
            PlayedGameModes = DecodeElements(encodedGameModes);
            PlayedMaps = DecodeElements(encodedMaps);
            Top5Maps = GetTop5(PlayedMaps);
            Top5GameModes = GetTop5(PlayedGameModes);
        }

        private static double CalculateAverage(int count, DateTime firstMatchDate, DateTime lastMatchDate)
        {
            return (double)count / (Math.Abs((firstMatchDate.Date - lastMatchDate.Date).Days) + 1);
        }

        public void CalculateAverageData(DateTime firstMatchDate, DateTime lastMatchDate)
        {
            AverageMatchesPerDay = CalculateAverage(MaximumMatchesPerDay, firstMatchDate, lastMatchDate);
            AveragePopulation = CalculateAverage(MaximumPopulation, firstMatchDate, lastMatchDate);
        }

        private Dictionary<string, int> DecodeElements(string encoded)
        {
            var elements = new Dictionary<string, int>();
            foreach (var data in encoded.Split(','))
            {
                var splitted = data.Split(':');
                var elem = splitted[0];
                var count = int.Parse(splitted[1]);
                elements[elem] = count;
            }
            return elements;
        }

        private string[] GetTop5(Dictionary<string, int> played)
        {
            return played.Keys
                .OrderByDescending(key => played[key])
                .Take(5)
                .ToArray();
        }

        private static string EncodePlayedElements(Dictionary<string, int> played)
        {
            var data = played.Keys
                .Select(key => $"{key}:{played[key]}");
            return string.Join("", data);
        }

        public string EncodeGameModes()
        {
            return EncodePlayedElements(PlayedGameModes);
        }

        public string EncodeMaps()
        {
            return EncodePlayedElements(PlayedMaps);
        }

        public string Serialize(params Field[] fields)
        {
            return Serialize(this, fields.Select(field => field.ToString()).ToArray());
        }
    }
}
