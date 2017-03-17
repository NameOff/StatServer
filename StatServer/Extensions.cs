using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace StatServer
{
    public static class Extensions
    {
        private static int AdjustCount(int count)
        {
            if (count > StatServer.ReportStatsMaxCount)
                return StatServer.ReportStatsMaxCount;
            if (count < StatServer.ReportStatsMinCount)
                return StatServer.ReportStatsMinCount;
            return count;
        }

        public static string SerializeTopPlayers(PlayerStats[] players)
        {
            var array = players.Select(player => new Dictionary<string, object>
            {
                ["name"] = player.Name,
                ["killToDeathRatio"] = player.KillToDeathRatio
            })
            .ToArray();
            return JsonConvert.SerializeObject(array, Formatting.Indented);
        }

        public static string SerializePopularServers(GameServerStats[] servers)
        {
            var array = servers.Select(server => new Dictionary<string, object>
            {
                [GameServerStats.Field.Endpoint.ToString()] = server.Endpoint,
                [GameServerStats.Field.Name.ToString()] = server.Name,
                [GameServerStats.Field.AverageMatchesPerDay.ToString()] = server.AverageMatchesPerDay
            })
            .ToArray();
            return JsonConvert.SerializeObject(array, Formatting.Indented, Serializable.Settings);
        }

        public static int StringCountToInt(string count)
        {
            int result;
            var isParsed = int.TryParse(count, out result);
            return isParsed ? AdjustCount(result) : StatServer.ReportStatsDefaultCount;
        }

        public static DateTime ParseTimestamp(string timestamp)
        {
            return DateTime.Parse(timestamp).ToUniversalTime();
        }

        public static double CalculateAverage(int count, DateTime start, DateTime end)
        {
            return (double)count / (Math.Abs((end.Date - start.Date).Days) + 1);
        }

        public static string EncodeElements(Dictionary<string, int> played)
        {
            var data = played.Keys
                .Select(key => $"{key}:{played[key]}");
            return string.Join(", ", data);
        }

        public static Dictionary<string, int> DecodeElements(string encoded)
        {
            if (encoded.Length == 0)
                return new Dictionary<string, int>();
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

        public static string ObjectToString(object obj)
        {
            var nfi = new NumberFormatInfo { NumberDecimalSeparator = "." };
            if (obj is string)
                return $"'{obj}'";
            if (obj is DateTime)
                return $"'{(DateTime)obj:s}Z'";
            if (obj is double)
                return ((double)obj).ToString(nfi);
            return obj.ToString();
        }
    }
}
