using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace StatServer.Tests
{
    class Test
    {
        [SetUp]
        public void SetUp()
        {
            var server = new StatServer();
            new Thread(prefix => server.Start((string)prefix)).Start();
            server.Stop();
        }

        [Test]
        public void Test1()
        {
            
        }
    }
}
