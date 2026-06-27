using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    #region SchemaBase Tests

    [TestMethod]
    public void SchemaBase_GetConstructors_ReturnsAllConstructors()
    {
        var schema = new TestSchemaWithTable();

        var constructors = schema.GetConstructors();

        Assert.IsNotEmpty(constructors);
    }

    [TestMethod]
    public void SchemaBase_GetConstructors_ByMethodName_ReturnsMatchingConstructors()
    {
        var schema = new TestSchemaWithTable();

        var constructors = schema.GetConstructors("custom_table");

        Assert.HasCount(1, constructors);
    }

    [TestMethod]
    public void SchemaBase_GetConstructors_ByMethodName_NoMatch_ReturnsEmpty()
    {
        var schema = new TestSchemaWithTable();

        var constructors = schema.GetConstructors("nonexistent_table");

        Assert.IsEmpty(constructors);
    }

    [TestMethod]
    public void SchemaBase_GetTableByName_ThrowsForEmptyName()
    {
        var schema = new TestSchemaWithTable();
        var ctx = CreateTestRuntimeContext();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.GetTableByName("", ctx));

        Assert.Contains("empty", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_GetTableByName_ThrowsForWhitespaceName()
    {
        var schema = new TestSchemaWithTable();
        var ctx = CreateTestRuntimeContext();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.GetTableByName("   ", ctx));

        Assert.Contains("empty", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_GetTableByName_ThrowsForNullContext()
    {
        var schema = new TestSchemaWithTable();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.GetTableByName("custom", null!));

        Assert.Contains("null", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_GetTableByName_ThrowsForUnknownTable()
    {
        var schema = new TestSchemaWithTable();
        var ctx = CreateTestRuntimeContext();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.GetTableByName("unknowntable", ctx));

        Assert.IsTrue(ex.Message.Contains("unknowntable") || ex.Message.Contains("Invalid"));
    }

    [TestMethod]
    public void SchemaBase_GetRowSource_ThrowsForEmptyName()
    {
        var schema = new TestSchemaWithTable();
        var ctx = CreateTestRuntimeContext();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.GetRowSource<string>("", ctx));

        Assert.Contains("empty", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_GetRowSource_ThrowsForNullContext()
    {
        var schema = new TestSchemaWithTable();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.GetRowSource<string>("custom", null!));

        Assert.Contains("null", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_GetRowSource_ThrowsForUnknownSource()
    {
        var schema = new TestSchemaWithTable();
        var ctx = CreateTestRuntimeContext();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.GetRowSource<string>("unknownsource", ctx));

        Assert.IsTrue(ex.Message.Contains("unknownsource") || ex.Message.Contains("Invalid"));
    }

    [TestMethod]
    public void SchemaBase_AddTable_ThrowsForEmptyName()
    {
        var schema = new TestSchemaWithTable();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.AddTablePublic<SingleRowSchemaTable>(""));

        Assert.Contains("empty", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_AddSource_ThrowsForEmptyName()
    {
        var schema = new TestSchemaWithTable();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.AddSourcePublic<SingleRowSource>(""));

        Assert.Contains("empty", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_GetRawConstructors_ReturnsTableConstructors()
    {
        var schema = new TestSchemaWithTable();
        var ctx = CreateTestRuntimeContext();

        var rawConstructors = schema.GetRawConstructors(ctx);

        Assert.IsGreaterThanOrEqualTo(1, rawConstructors.Length);
    }

    [TestMethod]
    public void SchemaBase_GetRawConstructors_WithMethodName_ReturnsFiltered()
    {
        var schema = new TestSchemaWithTable();
        var ctx = CreateTestRuntimeContext();

        var rawConstructors = schema.GetRawConstructors("custom", ctx);

        Assert.HasCount(1, rawConstructors);
        Assert.AreEqual("custom", rawConstructors[0].MethodName);
    }

    [TestMethod]
    public void SchemaBase_TryResolveMethod_ReturnsFalseForUnknownMethod()
    {
        var schema = new TestSchemaWithTable();

        var result = schema.TryResolveMethod("UnknownMethod", Array.Empty<Type>(), typeof(object), out var methodInfo);

        Assert.IsFalse(result);
        Assert.IsNull(methodInfo);
    }

    [TestMethod]
    public void SchemaBase_TryResolveRawMethod_ReturnsFalseForUnknownMethod()
    {
        var schema = new TestSchemaWithTable();

        var result = schema.TryResolveRawMethod("UnknownMethod", Array.Empty<Type>(), out var methodInfo);

        Assert.IsFalse(result);
        Assert.IsNull(methodInfo);
    }

    [TestMethod]
    public void SchemaBase_GetAllLibraryMethods_ReturnsMethodsDictionary()
    {
        var schema = new TestSchemaWithTable();

        var methods = schema.GetAllLibraryMethods();

        Assert.IsNotNull(methods);
    }

    [TestMethod]
    public void SchemaBase_Name_ReturnsCorrectName()
    {
        var schema = new TestSchemaWithTable();

        Assert.AreEqual("test", schema.Name);
    }

    [TestMethod]
    public void SchemaBase_Constructor_ThrowsForEmptyName()
    {
        var ex = Assert.Throws<SchemaArgumentException>(() =>
            new TestSchemaWithEmptyName(""));

        Assert.IsTrue(ex.Message.Contains("empty") || ex.Message.Contains("name"));
    }

    [TestMethod]
    public void SchemaBase_Constructor_ThrowsForNullAggregator()
    {
        var ex = Assert.Throws<SchemaArgumentException>(() =>
            new TestSchemaWithNullAggregator("valid", null!));

        Assert.Contains("null", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_TryResolveAggregationMethod_ReturnsFalseForNonAggregation()
    {
        var schema = new TestSchemaWithTable();

        var result = schema.TryResolveAggregationMethod("NonAggregation", Array.Empty<Type>(), typeof(object),
            out _);

        Assert.IsFalse(result);
    }

    #endregion

}
