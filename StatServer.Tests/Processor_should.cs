using System.Linq;
using NUnit.Framework;
using FluentAssertions;

namespace StatServer.Tests
{
    class Processor_should
    {
        private Processor processor;
        private GameSession session;

        [OneTimeSetUp]
        public void SetUp()
        {
            processor = new Processor();
            processor.ClearDatabaseAndCache();
            session = Test.CreateGameSession1();
            PutGameMatches();
        }

        public void PutGameMatches()
        {
            foreach (var server in session.Servers)
                processor.PutGameServerInfo(server.Info, server.Endpoint);
            foreach (var gameMatchResult in session.Matches)
                processor.PutGameMatchResult(gameMatchResult);
        }

        private static PlayerStats RemoveExcessFields(PlayerStats stats)
        {
            return Test.CreatePlayerStatsWithArguments(stats.Name, stats.TotalMatchesPlayed, stats.TotalMatchesWon,
                stats.FavoriteServer, stats.UniqueServers, stats.FavoriteGameMode, stats.AverageScoreboardPercent,
                stats.MaximumMatchesPerDay, stats.AverageMatchesPerDay, stats.LastMatchPlayed, stats.KillToDeathRatio);
        }

        private static GameServerStats RemoveExcessFields(GameServerStats stats)
        {
            return Test.CreateGameServersStatsWithArguments(stats.Endpoint, stats.TotalMatchesPlayed,
                stats.MaximumMatchesPerDay, stats.AverageMatchesPerDay, stats.MaximumPopulation, stats.AveragePopulation,
                stats.Top5GameModes, stats.Top5Maps);
        }

        [Test]
        public void HaveThreeServers()
        {
            processor.GetAllServersInfo().ShouldAllBeEquivalentTo(session.Servers);
        }

        [TestCase(Test.Server1Endpoint)]
        [TestCase(Test.Server2Endpoint)]
        [TestCase(Test.Server3Endpoint)]
        public void HaveCorrectGameServersInfo(string endpoint)
        {
            var expected = session.Servers.First(server => server.Endpoint == endpoint).Info;
            processor.GetGameServerInfo(endpoint).ShouldBeEquivalentTo(expected);
        }

        [TestCase(Test.PlayerNameOff)]
        [TestCase(Test.PlayerQoter)]
        [TestCase(Test.PlayerSnoward)]
        [TestCase(Test.PlayerUmqra)]
        [TestCase(Test.PlayerApollon76)]
        public void HaveCorrectPlayerStats(string playerName)
        {
            var precision = 1e-6;
            var stats = processor.GetPlayerStats(playerName);
            stats = RemoveExcessFields(stats);
            var expected = session.PlayersStats.First(player => player.Name == playerName);
            stats.ShouldBeEquivalentTo(expected, option => option
            .Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, precision))
            .WhenTypeIs<double>());
        }

        [TestCase(Test.Server1Endpoint)]
        [TestCase(Test.Server2Endpoint)]
        [TestCase(Test.Server3Endpoint)]
        public void HaveCorrectGameServerStats(string endpoint)
        {
            var precision = 1e-6;
            var stats = processor.GetGameServerStats(endpoint);
            stats = RemoveExcessFields(stats);
            var expected = session.ServersStats.First(server => server.Endpoint == endpoint);
            stats.ShouldBeEquivalentTo(expected, option => option
            .Using<double>(ctx => ctx.Subject.Should().BeApproximately(ctx.Expectation, precision))
            .WhenTypeIs<double>());
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            processor.ClearDatabaseAndCache();
        }
    }
}
