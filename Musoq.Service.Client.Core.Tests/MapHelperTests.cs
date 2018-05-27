using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Service.Client.Core.Helpers;

namespace Musoq.Service.Client.Core.Tests
{
    [TestClass]
    public class MapHelperTests
    {
        private ResultTable _table;

        [TestMethod]
        public void TryMapBasicTypesTest()
        {
            var columns = new[] {"TestInt", "TestBool", "TestString"};
            _table = new ResultTable(string.Empty, columns, new[]
            {
                new object[]
                {
                    1, true, "abc"
                },
                new object[]
                {
                    2, false, "xcd"
                }
            }, TimeSpan.Zero);

            var list = MapHelper.MapToType<TestClass>(_table);

            Assert.AreEqual(2, list.Count);

            Assert.AreEqual(typeof(TestClass), list[0].GetType());
            Assert.AreEqual(1, list[0].TestInt);
            Assert.AreEqual(true, list[0].TestBool);
            Assert.AreEqual("abc", list[0].TestString);


            Assert.AreEqual(typeof(TestClass), list[1].GetType());
            Assert.AreEqual(2, list[1].TestInt);
            Assert.AreEqual(false, list[1].TestBool);
            Assert.AreEqual("xcd", list[1].TestString);
        }

        [TestMethod]
        public void TryMapTypeWithConverterTest()
        {
            var columns = new[] {"TestInt", "TestBool", "TestArrayOfStrings"};
            _table = new ResultTable(string.Empty, columns, new[]
            {
                new object[]
                {
                    1, true, "abc,ff"
                }
            }, TimeSpan.Zero);

            var converters = new Dictionary<string, Func<object, object>>
            {
                {"TestArrayOfStrings", obj => ((string) obj).Split(',')}
            };

            var list = MapHelper.MapToType<TestClassComplex>(_table, converters);

            Assert.AreEqual(1, list.Count);

            Assert.AreEqual(typeof(TestClassComplex), list[0].GetType());
            Assert.AreEqual(1, list[0].TestInt);
            Assert.AreEqual(true, list[0].TestBool);
            Assert.AreEqual(2, list[0].TestArrayOfStrings.Length);
            Assert.AreEqual("abc", list[0].TestArrayOfStrings[0]);
            Assert.AreEqual("ff", list[0].TestArrayOfStrings[1]);
        }

        private class TestClass
        {
            public int TestInt { get; set; }

            public bool TestBool { get; set; }

            public string TestString { get; set; }
        }

        private class TestClassComplex
        {
            public int TestInt { get; set; }

            public bool TestBool { get; set; }

            public string[] TestArrayOfStrings { get; set; }
        }
    }
}