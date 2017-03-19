using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using FluentAssertions;
using Newtonsoft.Json;

namespace StatServer.Tests
{
    class StatServer_should_sendCorrectResponse
    {
        private StatServer server;
        private Client.Client client;

        private void SendServer1Info()
        {
            var info = Test.CreateGameServer1Info();
            client.SendRequest().PutServerInfo(info, Test.Server1Endpoint);
        }

        private void SendServer2Info()
        {
            var info = Test.CreateGameServer2Info();
            client.SendRequest().PutServerInfo(info, Test.Server2Endpoint);
        }

        private void SendServer3Info()
        {
            var info = Test.CreateGameServer3Info();
            client.SendRequest().PutServerInfo(info, Test.Server3Endpoint);
        }

        private void SendGameMatch()
        {
            var matchStats = Test.CreateGameMatchStats();
            client.SendRequest().PutMatchStats(matchStats, Test.Server1Endpoint, Test.Timestamp1);
        }

        [OneTimeSetUp]
        public void SetUp()
        {
            client = new Client.Client("http://127.0.0.1:8080/");
            server = new StatServer();
            server.ClearDatabaseAndCache();
            var prefix = "http://+:8080/";
            new Thread(() => server.Start(prefix)).Start();
        }

        [SetUp]
        public void SendServerInfo()
        {
            SendServer1Info();
        }

        [Test]
        public void AfterClientSendingGetServerInfoRequest()
        {
            var response = client.SendRequest().GetServerInfo(Test.Server1Endpoint);
            server.ClearDatabaseAndCache();
            var info = JsonConvert.DeserializeObject<GameServerInfo>(response.Message);
            info.ShouldBeEquivalentTo(Test.CreateGameServer1Info());
        }

        [Test]
        public void AfterClientSendingPutAndGetMatchStatsRequest()
        {
            SendGameMatch();
            var response = client.SendRequest().GetMatchStats(Test.Server1Endpoint, Test.Timestamp1);
            server.ClearDatabaseAndCache();
            var gameMatch = JsonConvert.DeserializeObject<GameMatchStats>(response.Message);
            gameMatch.ShouldBeEquivalentTo(Test.CreateGameMatchStats());
        }

        [Test]
        public void AfterClientSendingGetPlayerStatsRequest()
        {
            SendGameMatch();
            var stats = Test.CreatePlayerStats();
            var json = stats.SerializeForGetResponse();
            stats = JsonConvert.DeserializeObject<PlayerStats>(json);
            var response = client.SendRequest().GetPlayerStats(Test.PlayerNameOff);
            server.ClearDatabaseAndCache();
            var result = JsonConvert.DeserializeObject<PlayerStats>(response.Message);
            result.ShouldBeEquivalentTo(stats);
        }

        [Test]
        public void AfterClientSendingGetServerStatsRequest()
        {
            SendGameMatch();
            var stats = Test.CreateGameServerStats();
            var json = stats.SerializeForGetResponse();
            stats = JsonConvert.DeserializeObject<GameServerStats>(json);
            var response = client.GetServerStats(Test.Server1Endpoint);
            server.ClearDatabaseAndCache();
            var result = JsonConvert.DeserializeObject<GameServerStats>(response.Message);
            result.ShouldBeEquivalentTo(stats);
        }

        [Test]
        public void AfterClientSendingGetAllServersInfoRequest()
        {
            SendServer2Info();
            SendServer3Info();
            var response = client.SendRequest().GetAllServersInfo();
            server.ClearDatabaseAndCache();
            var result = JsonConvert.DeserializeObject<GameServerInfoResponse[]>(response.Message);
            var servers = new[] { new GameServerInfoResponse(Test.Server1Endpoint, Test.CreateGameServer1Info()),
                new GameServerInfoResponse(Test.Server2Endpoint, Test.CreateGameServer2Info()),
                new GameServerInfoResponse(Test.Server3Endpoint, Test.CreateGameServer3Info()) };
            new HashSet<GameServerInfoResponse>(result).ShouldBeEquivalentTo(new HashSet<GameServerInfoResponse>(servers));
        }

        [Test]
        public void AfterClientSendingGetRecentMatchesRequest()
        {
            var match = Test.CreateGameMatchStats();
            var count = 3;
            var timestamps = new DateTime[count];
            for (var i = 0; i < count; i++)
            {
                timestamps[i] = new DateTime(2017, 3, i + 1, 10, 10, 0);
                client.SendRequest().PutMatchStats(match, Test.Server1Endpoint, timestamps[i]);
            }
            var neededCount = 2;
            var matches = new GameMatchResult[neededCount];
            for (var i = 0; i < neededCount; i++)
                matches[i] = new GameMatchResult(Test.Server1Endpoint, timestamps[i + neededCount - 1]) {Results = match};
            var response = client.GetRecentMatches(neededCount);
            server.ClearDatabaseAndCache();
            var result = JsonConvert.DeserializeObject<GameMatchResult[]>(response.Message);
            result.ShouldAllBeEquivalentTo(matches);
        }

        [Test]
        public void AfterClientSendingGetBestPlayersRequest()
        {
            var match = Test.CreateGameMatchStats();
            var count = 10;
            var timestamps = new DateTime[count];
            for (var i = 0; i < count; i++)
            {
                timestamps[i] = new DateTime(2017, 3, i + 1, 10, 10, 0);
                client.SendRequest().PutMatchStats(match, Test.Server1Endpoint, timestamps[i]);
            }
            var response = client.SendRequest().GetBestPlayers(3);
            server.ClearDatabaseAndCache();
            var result = JsonConvert.DeserializeObject<PlayerStats[]>(response.Message);
            var playerQoter = new PlayerStats(Test.PlayerQoter, match.Scoreboard[0].Kills / (double)match.Scoreboard[0].Deaths);
            var playerSnoward = new PlayerStats(Test.PlayerSnoward, match.Scoreboard[2].Kills / (double)match.Scoreboard[2].Deaths);
            var playerNameOff = new PlayerStats(Test.PlayerNameOff, match.Scoreboard[1].Kills / (double)match.Scoreboard[1].Deaths);
            var playersStats = new[] {playerQoter, playerSnoward, playerNameOff };
            var json = Extensions.SerializeTopPlayers(playersStats);
            var bestPlayers = JsonConvert.DeserializeObject<PlayerStats[]>(json);
            result.ShouldBeEquivalentTo(bestPlayers);
        }

        [Test]
        public void AfterClientSendingGetPopularServersRequest()
        {
            SendServer2Info();
            SendServer3Info();
            var match = Test.CreateGameMatchStats();
            for (var i = 0; i < 5; i++)
                client.SendRequest().PutMatchStats(match, Test.Server1Endpoint, Test.Timestamp1);
            for (var i = 0; i < 3; i++)
                client.SendRequest().PutMatchStats(match, Test.Server2Endpoint, Test.Timestamp1);
            for (var i = 0; i < 2; i++)
                client.SendRequest().PutMatchStats(match, Test.Server3Endpoint, Test.Timestamp1);
            var response = client.GetPopularServers(3);
            server.ClearDatabaseAndCache();
            var server1 = new GameServerStats(Test.Server1Endpoint, Test.Server1Name, 5);
            var server2 = new GameServerStats(Test.Server2Endpoint, Test.Server2Name, 3);
            var server3 = new GameServerStats(Test.Server3Endpoint, Test.Server3Name, 2);
            var servers = new[] {server1, server2, server3};
            var json = Extensions.SerializePopularServers(servers);
            var popularServers = JsonConvert.DeserializeObject<GameServerStats[]>(json);
            var result = JsonConvert.DeserializeObject<GameServerStats[]>(response.Message);
            result.ShouldBeEquivalentTo(popularServers);
        }

        [TearDown]
        public void ClearDatabase()
        {
            server.ClearDatabaseAndCache();
        }

        [OneTimeTearDown]
        public void StopServer()
        {
            server.Stop();
        }
    }
}
