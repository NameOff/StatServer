using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatServer
{
    public class GameServerStats : Serializable
    {
        public enum Field
        {
            Endpoint, Name, TotalMatchesPlayed, MaximumMatchesPerDay,
            AverageMatchesPerDay, MaximumPopulation, AveragePopulation,
            Top5GameModes, Top5Maps, Info
        }

        public string Endpoint { get; set; }
        public string Name { get; set; }
        public int TotalMatchesPlayed { get; set; } 
        public int MaximumMatchesPerDay { get; set; }
        public double AverageMatchesPerDay { get; set; }
        public int MaximumPopulation { get; set; }
        public double AveragePopulation { get; set; }
        public string[] Top5GameModes { get; set; }
        public string[] Top5Maps { get; set; }
        public GameServerInfo Info { get; set; }

        public string Serialize(params Field[] fields)
        {
            return Serialize(this, fields.Select(field => field.ToString()).ToArray());
        }
    }
}
