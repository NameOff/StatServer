using NUnit.Framework;
using FluentAssertions;

namespace StatServer.Tests
{
    [TestFixture]
    class RegExp_should_correctMatch
    {
        [TestCase("/servers/192.123.123.1-8080/info", true)]
        [TestCase("/servers/abcdefg/info", true)]
        [TestCase("/servers/hostname-91223/info", true)]
        [TestCase("/servers/ipaddress-1010/info", true)]
        [TestCase("/servers/abcde/info", true)]
        [TestCase("servers/192.123.123.1-8080/info", false)]
        [TestCase("/servers/192.123.123.1-8080/info/", false)]
        [TestCase("abracadabra", false)]
        [TestCase("server/abcde/info", false)]
        [TestCase("servers/abcd/nfo", false)]
        public void WithGameServerInfoPath(string path, bool isMatched)
        {
            // /servers/<endpoint>/info
            Processor.GameServerInfoPath.IsMatch(path).Should().Be(isMatched);
        }

        [TestCase("/servers/0.0.0.1-8080/matches/2017-01-22T15:17:00Z", true)]
        [TestCase("/servers/hostname-920/matches/2017-01-22T15:17:00Z", true)]
        [TestCase("/servers/randomText/matches/2017-01-22T15:17:00Z", true)]
        [TestCase("/servers/abcd/matches/2017-a-22T15:17:00Z", false)]
        [TestCase("/servers/0.0.0.1-8080/matches/2017-01-2215:17:00Z", false)]
        [TestCase("/servers/0.0.0.1-8080/matches/2017-01-22 15:17:00", false)]
        [TestCase("/servers/0.0.0.1-8080/matches/2017-01-222T15:17:00Z", false)]
        [TestCase("/servers/0.0.0.1-8080/matches/2017.01.22T15:17:00Z", false)]
        [TestCase("/servers/0.0.0.1-8080/2017-01-22T15:17:00Z", false)]
        [TestCase("/servers/0.0.0.1-8080/matches/2017-01-22T15-17-00Z", false)]
        [TestCase("servers/0.0.0.1-8080/matches/2017-01-22T15:17:00Z", false)]
        [TestCase("/servers/0.0.0.1-8080/matches/2017-01-T15:17:00Z", false)]
        public void WithGameMatchStatsPath(string path, bool isMatched)
        {
            // /servers/<endpoint>/matches/<timestamp>
            // correct timestamp example 2017-01-22T15:17:00Z
            Processor.GameMatchStatsPath.IsMatch(path).Should().Be(isMatched);
        }

        [TestCase("/servers/info", true)]
        [TestCase("servers/info", false)]
        [TestCase("/servers/info/", false)]
        [TestCase("/servers//info", false)]
        public void WithAllGameServersInfoPath(string path, bool isMatched)
        {
            // /servers/info
            Processor.AllGameServersInfoPath.IsMatch(path).Should().Be(isMatched);
        }

        [TestCase("/servers/192.168.0.1-9090/stats", true)]
        [TestCase("/servers/hostname-ip/stats", true)]
        [TestCase("/servers/hostname--9090/stats", true)]
        [TestCase("/servers//id/stats", true)]
        [TestCase("/servers/text/stats", true)]
        [TestCase("/servers/192.168.0.1-9090/stats/", false)]
        [TestCase("servers/192.168.0.1-9090/stats", false)]
        [TestCase("/servers/id/stat", false)]
        [TestCase("/server/id/stats", false)]
        [TestCase("//servers/id/stats", false)]
        public void WithGameServerStatsPath(string path, bool isMatched)
        {
            // /servers/<endpoint>/stats
            Processor.GameServerStatsPath.IsMatch(path).Should().Be(isMatched);
        }

        [TestCase("/players/player/stats", true)]
        [TestCase("/players/player%abcd/stats", true)]
        [TestCase("/players//stats", true)]
        [TestCase("/player/12/stats", false)]
        [TestCase("/players/player/stats/", false)]
        [TestCase("players/player/stats", false)]
        [TestCase("/players/player/stat", false)]
        public void WithPlayerStatsPath(string path, bool isMatched)
        {
            // /players/<name>/stats
            Processor.PlayerStatsPath.IsMatch(path).Should().Be(isMatched);
        }

        [TestCase("/reports/recent-matches/10", true)]
        [TestCase("/reports/recent-matches", true)]
        [TestCase("/reports/recent-matches/-10", true)]
        [TestCase("/reports/recent-matches/0", true)]
        [TestCase("/reports/recent-matches/1000", true)]
        [TestCase("/reports/recent-matches/10/", false)]
        [TestCase("reports/recent-matches", false)]
        [TestCase("/reports/recent-matches/--10", false)]
        [TestCase("/reports/recent-matches/-10/", false)]
        [TestCase("/reports/recent-matches/(10)", false)]
        public void WithRecentMatchesPath(string path, bool isMatched)
        {
            // /reports/recent-matches[/<count>]
            Processor.RecentMatchesPath.IsMatch(path).Should().Be(isMatched);
        }

        [TestCase("/reports/best-players/10", true)]
        [TestCase("/reports/best-players", true)]
        [TestCase("/reports/best-players/-10", true)]
        [TestCase("/reports/best-players/0", true)]
        [TestCase("/reports/best-players/1000", true)]
        [TestCase("/reports/best-players/10/", false)]
        [TestCase("reports/best-players", false)]
        [TestCase("/reports/best-players/--10", false)]
        [TestCase("/reports/best-players/-10/", false)]
        [TestCase("/reports/best-players/(10)", false)]
        public void WithBestPlayersPath(string path, bool isMatched)
        {
            // /reports/best-players[/<count>]
            Processor.BestPlayersPath.IsMatch(path).Should().Be(isMatched);
        }

        [TestCase("/reports/popular-servers/10", true)]
        [TestCase("/reports/popular-servers", true)]
        [TestCase("/reports/popular-servers/-10", true)]
        [TestCase("/reports/popular-servers/0", true)]
        [TestCase("/reports/popular-servers/1000", true)]
        [TestCase("/reports/popular-servers/10/", false)]
        [TestCase("reports/popular-servers", false)]
        [TestCase("/reports/popular-servers/--10", false)]
        [TestCase("/reports/popular-servers/-10/", false)]
        [TestCase("/reports/popular-servers/(10)", false)]
        public void WithPopularServersPath(string path, bool isMatched)
        {
            // /reports/popular-servers[/<count>]
            Processor.PopularServersPath.IsMatch(path).Should().Be(isMatched);
        }
        [TestCase("http://+:8080/", true)]
        [TestCase("https://+:8080/", true)]
        [TestCase("http://*:8080/", true)]
        [TestCase("https://*:8080/", true)]
        [TestCase("http://+:808099/", false)]
        [TestCase("http://+:8080", false)]
        [TestCase("http://+:/", false)]
        [TestCase("http://:8080/", false)]
        [TestCase("httpss://+:8080/", false)]
        public void WithPrefix(string prefix, bool isMatched)
        {
            
        }
    }
}
