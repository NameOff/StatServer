using FluentAssertions;
using NUnit.Framework;

namespace StatServer.Tests
{
    class Cache_should_haveCorrect
    {
        private Cache cache;
        private Processor processor;

        [OneTimeSetUp]
        public void SetUp()
        {
            var session = Test.CreateGameSession1();
            processor = new Processor();
            processor.ClearDatabaseAndCache();
            PutGameMatches(session);
            cache = new Cache(new Database());
        }

        public void PutGameMatches(GameSession session)
        {
            foreach (var server in session.Servers)
                processor.PutGameServerInfo(server.Info, server.Endpoint);
            foreach (var gameMatchResult in session.Matches)
                processor.PutGameMatchResult(gameMatchResult);
        }

        [Test]
        public void LastMatchDate_AfterRestart()
        {
            cache.LastMatchDate.Should().Be(processor.Cache.LastMatchDate);
        }

        [Test]
        public void Players_AfterRestart()
        {
            cache.Players.ShouldBeEquivalentTo(processor.Cache.Players);
        }

        [Test]
        public void GameServersFirstMatchDate_AfterRestart()
        {
            cache.GameServersFirstMatchDate.ShouldBeEquivalentTo(processor.Cache.GameServersFirstMatchDate);
        }

        [Test]
        public void PlayersFirstMatchDate_AfterRestart()
        {
            cache.PlayersFirstMatchDate.ShouldBeEquivalentTo(processor.Cache.PlayersFirstMatchDate);
        }

        [Test]
        public void GameServersStats_AfterReload()
        {
            cache.GameServersStats.ShouldBeEquivalentTo(processor.Cache.GameServersStats);
        }

        [Test]
        public void GameServersInformation_AfterRestart()
        {
            cache.GameServersInformation.ShouldBeEquivalentTo(processor.Cache.GameServersInformation);
        }

        [Test]
        public void GameMatches_AfterRestart()
        {
            cache.GameMatches.ShouldBeEquivalentTo(processor.Cache.GameMatches);
        }

        [Test]
        public void PlayersStats_AfterRestart()
        {
            cache.PlayersStats.ShouldBeEquivalentTo(processor.Cache.PlayersStats);
        }

        [Test]
        public void RecentMatches_AfterRestart()
        {
            cache.RecentMatches.ShouldBeEquivalentTo(processor.Cache.RecentMatches);
        }

        [Test]
        public void GameServersMatchesPerDay_AfterRestart()
        {
            cache.GameServersMatchesPerDay.ShouldBeEquivalentTo(processor.Cache.GameServersMatchesPerDay);
        }

        [Test]
        public void PlayersMatchesPerDay_AfterRestart()
        {
            cache.PlayersMatchesPerDay.ShouldBeEquivalentTo(processor.Cache.PlayersMatchesPerDay);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            processor.ClearDatabaseAndCache();
        }
    }
}
