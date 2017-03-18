using System;
using System.Collections.Concurrent;

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

        public static DateTime Timestamp1 = Extensions.ParseTimestamp("2017-01-22T15:00:00Z");
        public static DateTime Timestamp2 = Extensions.ParseTimestamp("2017-01-22T23:59:59Z");
        public static DateTime Timestamp3 = Extensions.ParseTimestamp("2017-01-23T10:00:00Z");

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
            var modes = new ConcurrentDictionary<string, int> { [match.GameMode] = 1 };
            var maps = new ConcurrentDictionary<string, int> { [match.Map] = 1 };
            var stats = new GameServerStats
            {
                Endpoint = Server1Endpoint,
                Name = Server1Name,
                TotalMatchesPlayed = 1,
                MaximumPopulation = 5,
                TotalPopulation = 5,
                PlayedGameModes = modes,
                PlayedMaps = maps,
                MaximumMatchesPerDay = 1,
                Top5Maps = new[] { Map1 },
                Top5GameModes = new[] { GameModeDM }
            };
            stats.CalculateAverageData(Timestamp1, Timestamp1);
            return stats;
        }

        public static PlayerStats CreatePlayerStats()
        {

            var playedServers = new ConcurrentDictionary<string, int> { [Server1Endpoint] = 1 };
            var playedModes = new ConcurrentDictionary<string, int> { [GameModeDM] = 1 };
            var scoreboardPercent = 3.0 / 4 * 100;
            var stats = new PlayerStats(PlayerNameOff, 1, 0, playedServers, playedModes, scoreboardPercent, Timestamp1,
                1, 39, 23) {UniqueServers = 1};
            stats.CalculateAverageData(Timestamp1, Timestamp1);
            return stats;
        }

        public static PlayerStats CreatePlayerStatsWithArguments(string name, int totalMatchesPlayed, int totalMatchesWon, string favoriteServer,
            int uniqueServers, string favoriteGameMode, double averageScoreboardPercent, int maximumMatchesPerDay,
            double averageMatchesPerDay, DateTime lastMatchPlayed, double killtoDeathRatio)
        {
            return new PlayerStats
            {
                Name = name,
                TotalMatchesPlayed = totalMatchesPlayed,
                TotalMatchesWon = totalMatchesWon,
                FavoriteServer = favoriteServer,
                UniqueServers = uniqueServers,
                FavoriteGameMode = favoriteGameMode,
                AverageScoreboardPercent = averageScoreboardPercent,
                AverageMatchesPerDay = averageMatchesPerDay,
                MaximumMatchesPerDay = maximumMatchesPerDay,
                KillToDeathRatio = killtoDeathRatio,
                LastMatchPlayed = lastMatchPlayed
            };
        }

        public static GameServerStats CreateGameServersStatsWithArguments(string endpoint, int totalMatchesPlayed,
            int maximumMatchesPerDay, double averageMatchesPerDay, int maximumPopulation,
            double averagePopulation, string[] top5GameModes, string[] top5Maps)
        {
            return new GameServerStats
            {
                Endpoint = endpoint,
                TotalMatchesPlayed = totalMatchesPlayed,
                MaximumMatchesPerDay = maximumMatchesPerDay,
                AverageMatchesPerDay = averageMatchesPerDay,
                AveragePopulation = averagePopulation,
                Top5GameModes = top5GameModes,
                Top5Maps = top5Maps,
                MaximumPopulation = maximumPopulation
            };
        }

        public static GameSession CreateGameSession1()
        {
            var timestamp1 = Extensions.ParseTimestamp("2017.03.15T10:00:00Z");
            var timestamp2 = Extensions.ParseTimestamp("2017.03.16T00:00:00Z");
            var timestamp3 = Extensions.ParseTimestamp("2017.03.16T23:59:59Z");
            var timestamp4 = Extensions.ParseTimestamp("2017.03.18T13:00:00Z");
            var session = new GameSession();

            session.Servers.Add(new GameServerInfoResponse(Server1Endpoint, CreateGameServer1Info()));
            session.Servers.Add(new GameServerInfoResponse(Server2Endpoint, CreateGameServer2Info()));
            session.Servers.Add(new GameServerInfoResponse(Server3Endpoint, CreateGameServer3Info()));

            session.Matches.Add(new GameMatchResult(Server1Endpoint, timestamp1, new GameMatchStats(Map1, GameModeDM, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerNameOff, 15, 15, 3),
                new PlayerInfo(PlayerUmqra, 10, 10, 5),
                new PlayerInfo(PlayerSnoward, 7, 7, 12),
                new PlayerInfo(PlayerQoter, 5, 5, 10),
                new PlayerInfo(PlayerApollon76, 2, 2, 10)
            })));

            session.Matches.Add(new GameMatchResult(Server1Endpoint, timestamp2, new GameMatchStats(Map2, GameModeSD, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerUmqra, 15, 15, 10),
                new PlayerInfo(PlayerSnoward, 10, 10, 7),
                new PlayerInfo(PlayerNameOff, 7, 7, 18),
                new PlayerInfo(PlayerApollon76, 6, 6, 11),
                new PlayerInfo(PlayerQoter, 4, 4, 13)
            })));

            session.Matches.Add(new GameMatchResult(Server1Endpoint, timestamp3, new GameMatchStats(Map1, GameModeDM, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerNameOff, 15, 15, 3),
                new PlayerInfo(PlayerQoter, 14, 14, 4),
                new PlayerInfo(PlayerSnoward, 7, 7, 10),
                new PlayerInfo(PlayerUmqra, 6, 6, 15)
            })));

            session.Matches.Add(new GameMatchResult(Server2Endpoint, timestamp1, new GameMatchStats(Map2, GameModeDM, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerSnoward, 15, 15, 3),
                new PlayerInfo(PlayerApollon76, 10, 10, 11),
                new PlayerInfo(PlayerUmqra, 4, 4, 5)
            })));

            session.Matches.Add(new GameMatchResult(Server2Endpoint, timestamp2, new GameMatchStats(Map3, GameModeDM, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerNameOff, 15, 15, 4),
                new PlayerInfo(PlayerUmqra, 13, 13, 6),
                new PlayerInfo(PlayerQoter, 10, 10, 5),
                new PlayerInfo(PlayerApollon76, 1, 1, 13),
                new PlayerInfo(PlayerSnoward, 0, 0, 15)
            })));

            session.Matches.Add(new GameMatchResult(Server2Endpoint, timestamp3, new GameMatchStats(Map1, GameModeSD, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerQoter, 15, 15, 3),
                new PlayerInfo(PlayerApollon76, 10, 10, 5),
                new PlayerInfo(PlayerUmqra, 5, 5, 17)
            })));

            session.Matches.Add(new GameMatchResult(Server3Endpoint, timestamp1, new GameMatchStats(Map1, GameModeTDM, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerNameOff, 15, 15, 3),
                new PlayerInfo(PlayerApollon76, 14, 14, 4),
                new PlayerInfo(PlayerUmqra, 11, 11, 5),
                new PlayerInfo(PlayerSnoward, 10, 10, 11),
                new PlayerInfo(PlayerQoter, 3, 3, 20)
            })));

            session.Matches.Add(new GameMatchResult(Server3Endpoint, timestamp2, new GameMatchStats(Map2, GameModeTDM, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerSnoward, 15, 15, 10),
                new PlayerInfo(PlayerUmqra, 14, 14, 3),
                new PlayerInfo(PlayerQoter, 10, 10, 11),
                new PlayerInfo(PlayerNameOff, 3, 3, 7)
            })));

            session.Matches.Add(new GameMatchResult(Server3Endpoint, timestamp3, new GameMatchStats(Map3, GameModeSD, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerNameOff, 15, 15, 3),
                new PlayerInfo(PlayerUmqra, 13, 13, 8),
                new PlayerInfo(PlayerSnoward, 10, 10, 11),
                new PlayerInfo(PlayerApollon76, 9,9,13),
                new PlayerInfo(PlayerQoter, 4, 4,17)
            })));

            session.Matches.Add(new GameMatchResult(Server3Endpoint, timestamp4, new GameMatchStats(Map1, GameModeDM, 15, 10, 5, new[]
            {
                new PlayerInfo(PlayerQoter, 15, 15, 5),
                new PlayerInfo(PlayerApollon76, 14, 14, 10),
                new PlayerInfo(PlayerNameOff, 13, 13, 11),
                new PlayerInfo(PlayerSnoward, 12, 12, 10),
                new PlayerInfo(PlayerUmqra, 11, 11, 20)
            })));

            session.PlayersStats.Add(CreatePlayerStatsWithArguments(PlayerNameOff, 8, 5, Server3Endpoint, 3, GameModeDM, 75.0, 5, 2, timestamp4, 98 / 52.0));
            session.PlayersStats.Add(CreatePlayerStatsWithArguments(PlayerQoter, 9, 2, Server3Endpoint, 3, GameModeDM, 375 / 9.0, 6, 9 / 4.0, timestamp4, 80 / 88.0));
            session.PlayersStats.Add(CreatePlayerStatsWithArguments(PlayerSnoward, 9, 2, Server3Endpoint, 3, GameModeDM, (425 + 1 / 3.0 * 100) / 9.0, 5, 9 / 4.0, timestamp4, 86 / 89.0));
            session.PlayersStats.Add(CreatePlayerStatsWithArguments(PlayerUmqra, 10, 1, Server3Endpoint, 3, GameModeDM, (375 + 2 / 3.0 * 100) / 10, 6, 2.5, timestamp4, 102 / 94.0));
            session.PlayersStats.Add(CreatePlayerStatsWithArguments(PlayerApollon76, 8, 0, Server3Endpoint, 3, GameModeDM, 325 / 8.0, 4, 2, timestamp4, 66 / 77.0));

            session.ServersStats.Add(CreateGameServersStatsWithArguments(Server1Endpoint, 3, 2, 3 / 4.0, 5, 14 / 4.0, new[] { GameModeDM, GameModeSD }, new[] { Map1, Map2 }));
            session.ServersStats.Add(CreateGameServersStatsWithArguments(Server2Endpoint, 3, 2, 3 / 4.0, 5, 11 / 4.0, new[] { GameModeDM, GameModeSD }, new[] { Map1, Map2, Map3 }));
            session.ServersStats.Add(CreateGameServersStatsWithArguments(Server3Endpoint, 4, 2, 1, 5, 19 / 4.0, new[] { GameModeTDM, GameModeDM, GameModeSD }, new[] { Map1, Map2, Map3 }));

            return session;
        }
    }
}