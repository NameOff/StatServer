using System.Runtime.Serialization;

namespace StatServer
{
    [DataContract]
    public class GameMatchStats
    {
        [DataMember(IsRequired = true)]
        public string Map { get; set; }
        [DataMember(IsRequired = true)]
        public string GameMode { get; set; }
        [DataMember(IsRequired = true)]
        public int FragLimit { get; set; }
        [DataMember(IsRequired = true)]
        public int TimeLimit { get; set; }
        [DataMember(IsRequired = true)]
        public double TimeElapsed { get; set; }
        [DataMember(IsRequired = true)]
        public PlayerInfo[] Scoreboard { get; set; }

        public GameMatchStats()
        {
            
        }

        public GameMatchStats(string map, string gameMode, int fragLimit, int timeLimit, double timeElapsed,
            PlayerInfo[] scoreboard)
        {
            Map = map;
            GameMode = gameMode;
            FragLimit = fragLimit;
            TimeLimit = timeLimit;
            TimeElapsed = timeElapsed;
            Scoreboard = scoreboard;
        }
    }
}
