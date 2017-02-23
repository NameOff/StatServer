using System;
using System.Linq;

namespace StatServer
{
    public class PlayerInfo
    {
        public string Name { get; set; }
        public int Frags { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }

        public PlayerInfo(string name, int frags, int kills, int deaths)
        {
            Name = name;
            Frags = frags;
            Kills = kills;
            Deaths = deaths;
        }
    }
}