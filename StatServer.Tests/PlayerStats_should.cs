using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace StatServer.Tests
{
    class PlayerStats_should
    {
        private PlayerStats playerStats;
        private GameMatchResult matchResult;
        private Dictionary<DateTime, int> matchesPerDay;
        private double OldAverageScoreboardPercent;
        private DateTime firstMatch => new DateTime(2017, 3, 10, 17, 23, 59);
        private DateTime lastMatch => new DateTime(2017, 3, 13, 1, 0, 0);

        [SetUp]
        public void SetUp()
        {
            playerStats = CreatePlayerStats();
            matchResult = CreateGameMatchResult();
            matchesPerDay = new Dictionary<DateTime, int>
            {
                [new DateTime(2017, 3, 10)] = 5,
                [new DateTime(2017, 3, 11)] = 5,
            };
            OldAverageScoreboardPercent = playerStats.AverageScoreboardPercent;
            playerStats.UpdateStats(matchResult, matchesPerDay);
            playerStats.CalculateAverageData(firstMatch, lastMatch);
        }

        private GameMatchResult CreateGameMatchResult()
        {
            var scoreBoard = new[] { new PlayerInfo("Apollon76", 42, 42, 13), new PlayerInfo("NameOff", 13, 13, 42) };
            var matchStats = new GameMatchStats("Desert", "DM", 42, 30, 17.424, scoreBoard);
            return new GameMatchResult("Second", new DateTime(2017, 3, 12, 00, 31, 18)) { Results = matchStats };
        }

        private PlayerStats CreatePlayerStats()
        {
            var servers = new Dictionary<string, int> { ["First"] = 5, ["Second"] = 5 };
            var modes = new Dictionary<string, int> { ["DM"] = 5, ["SD"] = 1, ["TDM"] = 4 };
            var lastMatch = new DateTime(2017, 3, 11, 23, 45, 0);
            return new PlayerStats("NameOff", 10, 3, servers, modes, 72.123442, lastMatch, 78, 98);
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
            playerStats.FavoriteServer.Should().Be("Second");
        }

        [Test]
        public void HaveFavoriteGameModeDM_AfterUpdate()
        {
            playerStats.FavoriteGameMode.Should().Be("DM");
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
            var value = (double) (78 + 13) / (98 + 42);
            playerStats.KillToDeathRatio.Should().Be(value);
        }

        [Test]
        public void HaveZeroScoreBoardPercent_InMatch()
        {
            playerStats.CalculateScoreboardPercent(matchResult).Should().Be(0);
        }
    }
}
