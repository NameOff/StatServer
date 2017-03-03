using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatServer
{
    public class GameServerInfoResponse
    {
        public string Endpoint { get; set; }
        public GameServerInfo Info { get; set; }

        public GameServerInfoResponse(string endpoint, GameServerInfo info)
        {
            Endpoint = endpoint;
            Info = info;
        }
    }
}
