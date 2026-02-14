using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class EvaluationHelperTests
{
    [TestMethod]
    public void CreateComplexTypeDescriptionArrayTest()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("Test", typeof(TestClass)).ToArray();


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test" && pair.Type == typeof(TestClass)));


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.Test" && pair.Type == typeof(TestClass)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Test.SubClass" && pair.Type == typeof(TestSubClass)));


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Test.SomeInt" && pair.Type == typeof(int)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Test.SomeString" && pair.Type == typeof(string)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Test.SomeObject" && pair.Type == typeof(object)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Test.SubClass.SomeInt" && pair.Type == typeof(int)));


        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Test.SomeInt.")));
        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Test.SomeString.")));
    }

    [TestMethod]
    public void RemapPrimitiveTypesTest()
    {
        Assert.AreEqual("System.Byte", EvaluationHelper.RemapPrimitiveTypes("byte"));
        Assert.AreEqual("System.SByte", EvaluationHelper.RemapPrimitiveTypes("sbyte"));

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

        Assert.AreEqual("System.Object", EvaluationHelper.RemapPrimitiveTypes("object"));

        Assert.AreEqual("System.DateTime", EvaluationHelper.RemapPrimitiveTypes("datetime"));
        Assert.AreEqual("System.DateTimeOffset", EvaluationHelper.RemapPrimitiveTypes("datetimeoffset"));
        Assert.AreEqual("System.TimeSpan", EvaluationHelper.RemapPrimitiveTypes("timespan"));

        Assert.AreEqual("System.Guid", EvaluationHelper.RemapPrimitiveTypes("guid"));

        Assert.AreEqual("System.SomeType", EvaluationHelper.RemapPrimitiveTypes("System.SomeType"));
    }

    [TestMethod]
    public void RemapPrimitiveTypeAsNullableTest()
    {
        Assert.AreEqual(typeof(byte?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Byte"));
        Assert.AreEqual(typeof(sbyte?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.SByte"));
        Assert.AreEqual(typeof(short?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Int16"));
        Assert.AreEqual(typeof(int?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Int32"));
        Assert.AreEqual(typeof(long?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Int64"));
        Assert.AreEqual(typeof(ushort?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.UInt16"));
        Assert.AreEqual(typeof(uint?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.UInt32"));
        Assert.AreEqual(typeof(ulong?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.UInt64"));
        Assert.AreEqual(typeof(string), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.String"));
        Assert.AreEqual(typeof(char?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Char"));
        Assert.AreEqual(typeof(bool?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Boolean"));
        Assert.AreEqual(typeof(float?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Single"));
        Assert.AreEqual(typeof(double?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Double"));
        Assert.AreEqual(typeof(decimal?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Decimal"));
        Assert.AreEqual(typeof(object), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Object"));
        Assert.AreEqual(typeof(DateTime?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.DateTime"));
        Assert.AreEqual(typeof(DateTimeOffset?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.DateTimeOffset"));
        Assert.AreEqual(typeof(TimeSpan?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.TimeSpan"));
        Assert.AreEqual(typeof(Guid?), EvaluationHelper.RemapPrimitiveTypeAsNullable("System.Guid"));
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithPrimitiveTypeAtRoot_ShouldNotExplorePrimitiveProperties()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("IntValue", typeof(int)).ToArray();

        Assert.HasCount(1, typeDescriptions);
        Assert.AreEqual("IntValue", typeDescriptions[0].FieldName);
        Assert.AreEqual(typeof(int), typeDescriptions[0].Type);
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithStringAtRoot_ShouldNotExploreStringProperties()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("StringValue", typeof(string)).ToArray();

        Assert.HasCount(1, typeDescriptions);
        Assert.AreEqual("StringValue", typeDescriptions[0].FieldName);
        Assert.AreEqual(typeof(string), typeDescriptions[0].Type);
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithObjectAtRoot_ShouldIncludeRootColumn()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("ObjectValue", typeof(object)).ToArray();

        Assert.HasCount(1, typeDescriptions);
        Assert.AreEqual("ObjectValue", typeDescriptions[0].FieldName);
        Assert.AreEqual(typeof(object), typeDescriptions[0].Type);
    }

    [TestMethod]
    public void GetSpecificColumnDescription_WithArrayColumn_ShouldReturnElementTypeInfo()
    {
        var table = new BasicEntityTable();

        var result = EvaluationHelper.GetSpecificColumnDescription(table, "Array");

        Assert.IsGreaterThan(0, result.Count, "Should return at least one row for the 'Array' column");
        Assert.AreEqual(3, result.Columns.Count(), "Should have 3 columns: Name, Index, Type");
        Assert.AreEqual("Array", result[0][0], "First row should contain 'Array' column name");
    }

    [TestMethod]
    public void GetSpecificColumnDescription_WithNonExistentColumn_ShouldThrowException()
    {
        var table = new BasicEntityTable();

        var exception = Assert.Throws<UnknownColumnOrAliasException>(() =>
            EvaluationHelper.GetSpecificColumnDescription(table, "NonExistent"));

        Assert.Contains("NonExistent", exception.Message, "Exception message should contain the column name");
    }

    [TestMethod]
    public void GetSpecificColumnDescription_WithCaseInsensitiveMatch_ShouldReturnColumnInfo()
    {
        var table = new BasicEntityTable();

        var result = EvaluationHelper.GetSpecificColumnDescription(table, "array");

        Assert.IsGreaterThan(0, result.Count, "Should find column with case-insensitive match");
        Assert.AreEqual("Array", result[0][0], "Should return the actual column name (Array)");
    }

    [TestMethod]
    public void GetSpecificColumnDescription_WithNonArrayColumn_ShouldDescribeType()
    {
        var table = new BasicEntityTable();


        Assert.Throws<ColumnMustBeAnArrayOrImplementIEnumerableException>(() =>
            EvaluationHelper.GetSpecificColumnDescription(table, "Name"));
    }

    [TestMethod]
    public void CreateComplexTypeDescription_WithArrayProperty_ShouldNotExplodeArray()
    {
        var typeDescriptions = EvaluationHelper.CreateTypeComplexDescription("Entity", typeof(TestClassWithArray))
            .ToArray();


        Assert.IsTrue(
            typeDescriptions.Any(pair => pair.FieldName == "Entity" && pair.Type == typeof(TestClassWithArray)));


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Entity.Id" && pair.Type == typeof(int)));
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Entity.Name" && pair.Type == typeof(string)));


        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Entity.Numbers" && pair.Type == typeof(int[])));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Entity.Items" && pair.Type == typeof(TestSubClass[])));


        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Entity.Numbers.")));
        Assert.IsFalse(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Entity.Items.")));


        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Entity.SubClass" && pair.Type == typeof(TestSubClass)));
        Assert.IsTrue(typeDescriptions.Any(pair =>
            pair.FieldName == "Entity.SubClass.SomeInt" && pair.Type == typeof(int)));


        // Note: This is different from arrays - Lists have Count, Capacity, etc.
        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName == "Entity.StringList"));

        Assert.IsTrue(typeDescriptions.Any(pair => pair.FieldName.StartsWith("Entity.StringList.")));
    }

    public class TestClassWithArray
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int[] Numbers { get; set; }
        public TestSubClass[] Items { get; set; }
        public List<string> StringList { get; set; }
        public TestSubClass SubClass { get; set; }
    }

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
}
