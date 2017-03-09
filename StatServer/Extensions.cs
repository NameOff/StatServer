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
    }
}
