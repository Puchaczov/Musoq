using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

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
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("Test", typeof(TestClass)).ToArray();

        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test" && pair.Type == typeof(TestClass)));
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.Test" && pair.Type == typeof(TestClass)));
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeInt" && pair.Type == typeof(int)));
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeString" && pair.Type == typeof(string)));
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeObject" && pair.Type == typeof(object)));
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SubClass" && pair.Type == typeof(TestSubClass)));
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SubClass.SomeInt" && pair.Type == typeof(int)));
    }

    [TestMethod]
    public void RemapPrimitiveTypesTest()
    {
        Assert.AreEqual("System.Int16", EvaluationHelper.RemapPrimitiveTypes("short"));
        Assert.AreEqual("System.Int32", EvaluationHelper.RemapPrimitiveTypes("int"));
        Assert.AreEqual("System.Int64", EvaluationHelper.RemapPrimitiveTypes("long"));

        Assert.AreEqual("System.UInt16", EvaluationHelper.RemapPrimitiveTypes("ushort"));
        Assert.AreEqual("System.UInt32", EvaluationHelper.RemapPrimitiveTypes("uint"));
        Assert.AreEqual("System.UInt64", EvaluationHelper.RemapPrimitiveTypes("ulong"));

        Assert.AreEqual("System.String", EvaluationHelper.RemapPrimitiveTypes("string"));

        Assert.AreEqual("System.Char", EvaluationHelper.RemapPrimitiveTypes("char"));

        Assert.AreEqual("System.Boolean", EvaluationHelper.RemapPrimitiveTypes("bool"));
        Assert.AreEqual("System.Boolean", EvaluationHelper.RemapPrimitiveTypes("boolean"));
        Assert.AreEqual("System.Boolean", EvaluationHelper.RemapPrimitiveTypes("bit"));

        Assert.AreEqual("System.Single", EvaluationHelper.RemapPrimitiveTypes("float"));
        Assert.AreEqual("System.Double", EvaluationHelper.RemapPrimitiveTypes("double"));

        Assert.AreEqual("System.Decimal", EvaluationHelper.RemapPrimitiveTypes("decimal"));
        Assert.AreEqual("System.Decimal", EvaluationHelper.RemapPrimitiveTypes("money"));

        Assert.AreEqual("System.Guid", EvaluationHelper.RemapPrimitiveTypes("guid"));

        Assert.AreEqual("System.SomeType", EvaluationHelper.RemapPrimitiveTypes("System.SomeType"));
    }
}