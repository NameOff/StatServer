using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        public string FragLimit { get; set; }
        public string TimeLimit { get; set; }
        public double TimeElapsed { get; set; }
        public PlayerInfo[] Scoreboard { get; set; }

        public string Serialize(params Field[] fields)
        {
            return Serialize(this, fields.Select(field => field.ToString()).ToArray());
        }
    }
}
