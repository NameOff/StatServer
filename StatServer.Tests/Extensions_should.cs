using System;
using System.Collections.Concurrent;
using NUnit.Framework;
using FluentAssertions;

namespace StatServer.Tests
{
    [TestFixture]
    class Extensions_should
    {
        [Test]
        public void CorrectlyParseTimestamp()
        {
            Extensions.ParseTimestamp("2017.03.15T13:00:00Z").Should().Be(new DateTime(2017, 3, 15, 13, 0, 0));
        }

        [TestCase("10", 10)]
        [TestCase("-1", 0)]
        [TestCase("100", 50)]
        [TestCase("70", 50)]
        [TestCase("-1000", 0)]
        [TestCase("1", 1)]
        [TestCase("0", 0)]
        public void CorrectlyParseStringCount(string count, int expected)
        {
            Extensions.StringCountToInt(count).Should().Be(expected);
        }

        [Test]
        public void CorrectlyCalculateAverage1()
        {
            Extensions.CalculateAverage(10, new DateTime(2017, 03, 15, 23, 00, 00),
                new DateTime(2017, 03, 18, 00, 00, 00)).Should().Be(10 / 4.0);
        }

        [Test]
        public void CorrectlyCalculateAverage2()
        {
            Extensions.CalculateAverage(15, new DateTime(2017, 03, 15, 23, 00, 00),
                new DateTime(2017, 03, 15, 23, 00, 00)).Should().Be(15.0);
        }

        [Test]
        public void CorrectlyDecodeElements()
        {
            var elements = "A:10,B:15,C:20,D:10";
            var expected = new ConcurrentDictionary<string, int>
            {
                ["A"] = 10,
                ["B"] = 15,
                ["C"] = 20,
                ["D"] = 10
            };
            Extensions.DecodeElements(elements).ShouldBeEquivalentTo(expected);
        }

        [TestCase(1, "1")]
        [TestCase(12.123, "12.123")]
        [TestCase("string", "'string'")]
        public void CorrectlyConvertToString(object obj, string expected)
        {
            Extensions.ObjectToString(obj).Should().Be(expected);
        }
    }
}
