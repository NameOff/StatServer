using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StatServer.Tests
{
    static class Test
    {
        public const string Server1Endpoint = "193.124.12.11-6274";
        public const string Server2Endpoint = "123.12.11.11-8090";
        public const string Server3Endpoint = "109.123.123.123-8017";

        public const string Server1Name = "JOIN US!!";
        public const string Server2Name = "Ekb Russia server";
        public const string Server3Name = "www.server-snipers.com";

        public const string GameModeDM = "DM";
        public const string GameModeTDM = "TDM";
        public const string GameModeSD = "SD";

        public const string Map1 = "Desert";
        public const string Map2 = "Snow valley";
        public const string Map3 = "Blood pool";

        public const string PlayerNameOff = "NameOff";
        public const string PlayerSnoward = "Snoward";
        public const string PlayerQoter = "Qoter";
        public const string PlayerUmqra = "Umqra";
        public const string PlayerApollon76 = "Apollon76";

        public static DateTime Timestamp1 = Extensions.ParseTimestamp("2017-01-22T15:17:00Z");


        public enum Player { NameOff, Snoward, Qoter, Apollon76, Umqra }

        public static GameMatchStats CreateGameMatchStats()
        {
            var scoreboard = new[]
            {
                new PlayerInfo(PlayerQoter, 42, 42, 3),
                new PlayerInfo(PlayerNameOff, 39, 39, 23),
                new PlayerInfo(PlayerSnoward, 22, 22, 10),
                new PlayerInfo(PlayerUmqra, 21, 21, 29),
                new PlayerInfo(PlayerApollon76, 17, 17, 34)
            };
            return new GameMatchStats(Map1, GameModeDM, 42, 80, 10.123213, scoreboard);
        }

        public static GameServerInfo CreateGameServer1Info()
        {
            return new GameServerInfo(Server1Name, new[] { GameModeDM, GameModeSD });
        }

        public static GameServerInfo CreateGameServer2Info()
        {
            return new GameServerInfo(Server2Name, new[] { GameModeTDM, GameModeDM });
        }

        public static GameServerInfo CreateGameServer3Info()
        {
            return new GameServerInfo(Server3Name, new[] { GameModeSD, GameModeTDM });
        }

        public static GameServerStats CreateGameServerStats()
        {
            var match = CreateGameMatchStats();
            var modes = new Dictionary<string, int> { [match.GameMode] = 1 };
            var maps = new Dictionary<string, int> { [match.Map] = 1 };
            var stats = new GameServerStats(Server1Endpoint, Server1Name, 1, 5, 5, modes, maps) { MaximumMatchesPerDay = 1 };
            stats.CalculateAverageData(Timestamp1, Timestamp1);
            return stats;
        }

        public static PlayerStats CreatePlayerStats()
        {

            var playedServers = new Dictionary<string, int> { [Server1Endpoint] = 1 };
            var playedModes = new Dictionary<string, int> { [GameModeDM] = 1 };
            var scoreboardPercent = 3.0 / 4 * 100;
            var stats = new PlayerStats(PlayerNameOff, 1, 0, playedServers, playedModes, scoreboardPercent, Timestamp1, 39, 23);
            stats.CalculateAverageData(Timestamp1, Timestamp1);
            return stats;
        }
    }
}
