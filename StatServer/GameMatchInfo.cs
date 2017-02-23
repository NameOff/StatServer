using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatServer
{
    public class GameMatchInfo
    {
        public string Server { get; set; }
        public string Timestamp { get; set; }
        public GameMatchStats Results { get; set; }
    }
}
