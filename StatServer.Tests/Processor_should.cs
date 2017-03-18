using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace StatServer.Tests
{
    class Processor_should
    {
        private Processor processor;

        [SetUp]
        public void SetUp()
        {
            processor = new Processor();
            processor.ClearDatabaseAndCache();
        }

        //public 

        [Test]
        public void Test1()
        {
            
        }
    }
}
