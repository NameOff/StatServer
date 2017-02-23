using System;
using System.Linq;

namespace StatServer
{
    public class GameServerInfo
    {
        public string Name { get; set; }
        public string[] GameModes { get; set; }

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
