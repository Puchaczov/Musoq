using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests.Core
{
    [TestClass]
    public class EvaluationHelperTests
    {
        public class TestSubClass
        {
            public int SomeInt { get; set; }
        }

        public class TestClass
        {
            public TestClass Test { get; set; }

            public int SomeInt { get; set; }

            public string SomeString { get; set; }

            public object SomeObject { get; set; }

            public TestSubClass SubClass { get; set; }

            public int SomeMethod()
            {
                return 0;
            }
        }

        [TestMethod]
        public void CreateComplexTypeDescriptionArrayTest()
        {
            var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("Test", typeof(TestClass));

            Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test" && pair.Type == typeof(TestClass)));
            Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.Test" && pair.Type == typeof(TestClass)));
            Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeInt" && pair.Type == typeof(int)));
            Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeString" && pair.Type == typeof(string)));
            Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeObject" && pair.Type == typeof(object)));
            Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SubClass" && pair.Type == typeof(TestSubClass)));
            Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SubClass.SomeInt" && pair.Type == typeof(int)));
        }
    }
}
