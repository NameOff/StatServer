using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatServer
{
    public static class Extensions
    {
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
            return string.Join("", data);
        }

        public static Dictionary<string, int> DecodeElements(string encoded)
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
    }
}
