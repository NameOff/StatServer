using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatServer
{
    public class GameMatchResult
    {
        public GameMatchResult(string server, string timestamp)
        {
            Server = server;
            Timestamp = Extensions.ParseTimestamp(timestamp);
        }

        public GameMatchResult()
        {
            
        }

        public string Server { get; set; }
        public DateTime Timestamp { get; set; }
        public GameMatchStats Results { get; set; }

        public override int GetHashCode()
        {
            return Server.GetHashCode() + Timestamp.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var info = (GameMatchResult) obj;
            return info?.Server == Server && info?.Timestamp == Timestamp;
        }
    }
}
