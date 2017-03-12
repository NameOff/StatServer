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

        [SetUp]
        public void SetUp()
        {
            client = new Client("http://127.0.0.1:8080/");
            server = new StatServer();
            new Thread(prefix => server.Start((string)prefix)).Start("http://+:8080/");
            var serverInfo = new GameServerInfo(Test.Server1Name, new[] { Test.GameModeDM, Test.GameModeSD });
            client.SendRequest().PutServerInfo(serverInfo, Test.Server1Endpoint);
        }

        [Test]
        public void SendCorrectResponse_AfterClientSendingGetServerInfoRequest()
        {
            var response = client.SendRequest().GetServerInfo(Test.Server1Endpoint);
            var info = JsonConvert.DeserializeObject<GameServerInfo>(response.Message);
            info.ShouldBeEquivalentTo(new GameServerInfo(Test.Server1Name, new[] { Test.GameModeDM, Test.GameModeSD }));
            server.Stop();
        }

        [Test]
        public void SendCorrectResponse_AfterClientSendingPutAndGetMatchStats()
        {
            var matchStats = Test.CreateGameMatchStats();
            var timestamp = "2017-01-22T15:17:00Z";
            client.SendRequest().PutMatchStats(matchStats, Test.Server1Endpoint, DateTime.Parse(timestamp));
            var response = client.SendRequest().GetMatchStats(Test.Server1Endpoint, DateTime.Parse(timestamp));
            var gameMatch = JsonConvert.DeserializeObject<GameMatchStats>(response.Message);
            gameMatch.ShouldBeEquivalentTo(matchStats);
            server.Stop();
            server.ClearDatabaseAndCache();
        }

        [Test]
        public void SendCorrectResponse_AfterClientSendingGetPlayerStats()
        {
            
        }
    }
}
