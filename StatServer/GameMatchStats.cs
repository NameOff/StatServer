using System.Linq;

namespace StatServer
{
    public class GameMatchStats : Serializable
    {
        public enum Field
        {
            Map, GameMode, FragLimit, TimeLimit,
            TimeElapsed, Scoreboard
        }

        public string Map { get; set; }
        public string GameMode { get; set; }
        public int FragLimit { get; set; }
        public int TimeLimit { get; set; }
        public double TimeElapsed { get; set; }
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

        public string Serialize(params Field[] fields)
        {
            return Serialize(this, fields.Select(field => field.ToString()).ToArray());
        }
    }
}
