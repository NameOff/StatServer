using System.Runtime.Serialization;

namespace StatServer
{
    [DataContract]
    public class PlayerInfo
    {
        [DataMember(IsRequired = true)]
        public string Name { get; set; }
        [DataMember(IsRequired = true)]
        public int Frags { get; set; }
        [DataMember(IsRequired = true)]
        public int Kills { get; set; }
        [DataMember(IsRequired = true)]
        public int Deaths { get; set; }

        public PlayerInfo(string name, int frags, int kills, int deaths)
        {
            Name = name.ToLower();
            Frags = frags;
            Kills = kills;
            Deaths = deaths;
        }
    }
}