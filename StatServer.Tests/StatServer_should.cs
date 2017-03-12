using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace StatServer.Tests
{
    class StatServer_should
    {
        private StatServer server;
        private Client client;

        [SetUp]
        public void SetUp()
        {
            client = new Client("127.0.0.1:8080");
            server = new StatServer();
            server.Start("http://+:8080/");
        }

        
    }
}
