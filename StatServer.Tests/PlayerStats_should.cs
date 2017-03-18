using System;
using System.Collections.Concurrent;
using NUnit.Framework;
using FluentAssertions;

namespace StatServer.Tests
{
    class PlayerStats_should
    {
        private PlayerStats playerStats;
        private GameMatchResult matchResult;
        private ConcurrentDictionary<DateTime, int> matchesPerDay;
        private double OldAverageScoreboardPercent;
        private DateTime firstMatch => new DateTime(2017, 3, 10, 17, 23, 59);
        private DateTime lastMatch => new DateTime(2017, 3, 13, 1, 0, 0);

        [SetUp]
        public void SetUp()
        {
            playerStats = CreatePlayerStats();
            matchResult = CreateGameMatchResult();
            matchesPerDay = new ConcurrentDictionary<DateTime, int>
            {
                [new DateTime(2017, 3, 10)] = 5,
                [new DateTime(2017, 3, 11)] = 5
            };

            OldAverageScoreboardPercent = playerStats.AverageScoreboardPercent;
            playerStats.UpdateStats(matchResult, matchesPerDay);
            playerStats.CalculateAverageData(firstMatch, lastMatch);
        }

        private static GameMatchResult CreateGameMatchResult()
        {
            var scoreBoard = new[] { new PlayerInfo(Test.PlayerApollon76, 42, 42, 13),
                new PlayerInfo(Test.PlayerNameOff, 13, 13, 42) };
            var matchStats = new GameMatchStats(Test.Map1, Test.GameModeDM, 42, 30, 17.424, scoreBoard);
            return new GameMatchResult(Test.Server2Endpoint, new DateTime(2017, 3, 12, 00, 31, 18), matchStats);
        }

        private static PlayerStats CreatePlayerStats()
        {
            var servers = new ConcurrentDictionary<string, int> { [Test.Server1Endpoint] = 5, [Test.Server2Endpoint] = 5 };
            var modes = new ConcurrentDictionary<string, int> { [Test.GameModeDM] = 5, [Test.GameModeSD] = 1, [Test.GameModeTDM] = 4 };
            var lastMatch = new DateTime(2017, 3, 11, 23, 45, 0);
            return new PlayerStats(Test.PlayerNameOff, 10, 3, servers, modes, 72.123442, lastMatch, 10, 78, 98);
        }

        [Test]
        public void IncrementTotalMatchesPlayed_AfterUpdate()
        {
            playerStats.TotalMatchesPlayed.Should().Be(11);
        }

        [Test]
        public void DoNotChangeTotalMatchesWon_AfterUpdate()
        {
            playerStats.TotalMatchesWon.Should().Be(3);
        }

        [Test]
        public void HaveFavoriteSecondServer_AfterUpdate()
        {
            playerStats.FavoriteServer.Should().Be(Test.Server2Endpoint);
        }

        [Test]
        public void HaveFavoriteGameModeDM_AfterUpdate()
        {
            playerStats.FavoriteGameMode.Should().Be(Test.GameModeDM);
        }

        [Test]
        public void ChangeAverageScoreboardPercent_AfterUpdate()
        {
            var newAveragePercent = OldAverageScoreboardPercent * (playerStats.TotalMatchesPlayed - 1) /
                                    playerStats.TotalMatchesPlayed;
            playerStats.AverageScoreboardPercent.Should().Be(newAveragePercent);
        }

        [Test]
        public void Have5MaximumMatchesPerDay_AfterUpdate()
        {
            playerStats.MaximumMatchesPerDay.Should().Be(5);
        }

        [Test]
        public void HaveAverageMatchesPerDayValue_AfterUpdate()
        {
            var value = 11.0 / 4;
            playerStats.AverageMatchesPerDay.Should().Be(value);
        }

        [Test]
        public void HaveKillToDeathRatioValue_AfterUpdate()
        {
            var value = (double)(78 + 13) / (98 + 42);
            playerStats.KillToDeathRatio.Should().Be(value);
        }

        [Test]
        public void HaveZeroScoreBoardPercent_InMatch()
        {
            playerStats.CalculateScoreboardPercent(matchResult.Results.Scoreboard).Should().Be(0);
        }
    }
}
