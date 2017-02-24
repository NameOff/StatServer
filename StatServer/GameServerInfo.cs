using System;
using System.Linq;

namespace StatServer
{
    public class GameServerInfo
    {
        public string Name { get; set; }
        public string[] GameModes { get; set; }

        public GameServerInfo()
        {
            
        }

        public GameServerInfo(string name, string[] gameModes)
        {
            Name = name;
            GameModes = gameModes;
        }

        public GameServerInfo(string name, string gameModes)
        {
            Name = name;
            GameModes = DecodeGameModes(gameModes);
        }

        public string EncodeGameModes()
        {
            return string.Join(", ", GameModes);
        }

        public string[] DecodeGameModes(string modes)
        {
            return modes
                .Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries)
                .ToArray();
        }
    }
}
