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

        // The root field should always be present
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test" && pair.Type == typeof(TestClass)));
        
        // Complex types should be present
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.Test" && pair.Type == typeof(TestClass)));
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SubClass" && pair.Type == typeof(TestSubClass)));
        
        // Primitive types and strings should NOT be present as nested properties
        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeInt" && pair.Type == typeof(int)));
        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeString" && pair.Type == typeof(string)));
        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeObject" && pair.Type == typeof(object)));
        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName == "Test.SubClass.SomeInt" && pair.Type == typeof(int)));
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

    [TestMethod]
    public void CreateComplexTypeDescription_WithPrimitiveTypeAtRoot_ShouldNotExplorePrimitiveProperties()
    {
        // Test that primitive types (like int) don't have their properties explored
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("IntValue", typeof(int)).ToArray();
        
        // Should only have the root entry
        Assert.AreEqual(1, typeDescriptions.Length);
        Assert.AreEqual("IntValue", typeDescriptions[0].FieldName);
        Assert.AreEqual(typeof(int), typeDescriptions[0].Type);
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithStringAtRoot_ShouldNotExploreStringProperties()
    {
        // Test that string type doesn't have its properties explored
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("StringValue", typeof(string)).ToArray();
        
        // Should only have the root entry, no Chars or Length properties
        Assert.AreEqual(1, typeDescriptions.Length);
        Assert.AreEqual("StringValue", typeDescriptions[0].FieldName);
        Assert.AreEqual(typeof(string), typeDescriptions[0].Type);
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithObjectAtRoot_ShouldIncludeRootColumn()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("ObjectValue", typeof(object)).ToArray();
        
        Assert.AreEqual(1, typeDescriptions.Length);
        Assert.AreEqual("ObjectValue", typeDescriptions[0].FieldName);
        Assert.AreEqual(typeof(object), typeDescriptions[0].Type);
    }
}
