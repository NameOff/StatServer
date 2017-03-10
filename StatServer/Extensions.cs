using System;
using System.Collections.Generic;
using System.Globalization;
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
            if (obj is string || obj is DateTime)
                return $"'{obj}'";
            if (obj is double)
                return ((double)obj).ToString(nfi);
            return obj.ToString();
        }
    }
}
