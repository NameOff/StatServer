using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;
using Newtonsoft.Json;

namespace StatServer.Tests
{
    class StatServer_should
    {
        private StatServer server;
        private Client client;

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

        [SetUp]
        public void SetUp()
        {
            client = new Client("http://127.0.0.1:8080/");
            server = new StatServer();
            server.ClearDatabaseAndCache();
            new Thread(prefix => server.Start((string)prefix)).Start("http://+:8080/");
        }

        [Test]
        public void SendCorrectResponse_AfterClientSendingGetServerInfoRequest()
        {
            SendServer1Info();
            var response = client.SendRequest().GetServerInfo(Test.Server1Endpoint);
            server.ClearDatabaseAndCache();
            var info = JsonConvert.DeserializeObject<GameServerInfo>(response.Message);
            info.ShouldBeEquivalentTo(Test.CreateGameServer1Info());
        }

        [Test]
        public void SendCorrectResponse_AfterClientSendingPutAndGetMatchStatsRequest()
        {
            SendServer1Info();
            SendGameMatch();
            var response = client.SendRequest().GetMatchStats(Test.Server1Endpoint, Test.Timestamp1);
            server.ClearDatabaseAndCache();
            var gameMatch = JsonConvert.DeserializeObject<GameMatchStats>(response.Message);
            gameMatch.ShouldBeEquivalentTo(Test.CreateGameMatchStats());
        }

        [Test]
        public void SendCorrectResponse_AfterClientSendingGetPlayerStatsRequest()
        {
            SendServer1Info();
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
        public void SendCorrectResponse_AfterClientSendingGetServerStatsRequest()
        {
            SendServer1Info();
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
        public void SendCorrectResponse_AfterClientSendingGetAllServersInfoRequest()
        {
            SendServer1Info();
            SendServer2Info();
            SendServer3Info();
            var response = client.SendRequest().GetAllServersInfo();
            server.ClearDatabaseAndCache();
            var result = JsonConvert.DeserializeObject<GameServerInfoResponse[]>(response.Message);
            var servers = new[] { new GameServerInfoResponse(Test.Server1Endpoint, Test.CreateGameServer1Info()),
                new GameServerInfoResponse(Test.Server2Endpoint, Test.CreateGameServer2Info()),
                new GameServerInfoResponse(Test.Server3Endpoint, Test.CreateGameServer3Info()) };
            result.ShouldBeEquivalentTo(servers);
        }

        [Test]
        public void SendCorrectResponse_AfterClientSendingGetRecentMatches()
        {
            var match = Test.CreateGameMatchStats();
            SendServer1Info();
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

        [TearDown]
        public void StopServer()
        {
            server.Stop();
        }
    }
}
