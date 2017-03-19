using System;

namespace StatServer
{
    public class GameMatchResult
    {
        public string Server { get; set; }
        public DateTime Timestamp { get; set; }
        public GameMatchStats Results { get; set; }

        public GameMatchResult(string server, DateTime timestamp)
        {
            Server = server;
            Timestamp = timestamp;
        }

        public GameMatchResult(string server, DateTime timestamp, GameMatchStats match)
        {
            Server = server;
            Timestamp = timestamp;
            Results = match;
        }

        public GameMatchResult()
        {
            
        }

        public override int GetHashCode()
        {
            return Server.GetHashCode() + Timestamp.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var info =  obj as GameMatchResult;
            return info?.Server == Server && info?.Timestamp == Timestamp;
        }
    }
}
