using System;
using System.Linq;
using System.Runtime.Serialization;

namespace StatServer
{
    [DataContract]
    public class GameServerInfo
    {
        [DataMember(IsRequired = true)]
        public string Name { get; set; }
        [DataMember(IsRequired = true)]
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
