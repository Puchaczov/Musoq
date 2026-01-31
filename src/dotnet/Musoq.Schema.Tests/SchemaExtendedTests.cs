using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

/// <summary>
///     Extended tests for Schema module to improve coverage
/// </summary>
[TestClass]
public class SchemaExtendedTests
{
    #region SourceNotFoundException Tests

    [TestMethod]
    public void SourceNotFoundException_Constructor_SetsMessage()
    {
        var ex = new SourceNotFoundException("TestSource");

        Assert.AreEqual("TestSource", ex.Message);
    }

    #endregion

    #region TableNotFoundException Tests

    [TestMethod]
    public void TableNotFoundException_Constructor_SetsMessage()
    {
        var ex = new TableNotFoundException("TestTable");

        Assert.AreEqual("TestTable", ex.Message);
    }

    #endregion

    #region SchemaArgumentException Tests

    [TestMethod]
    public void SchemaArgumentException_ForEmptyString_CreatesCorrectMessage()
    {
        var ex = SchemaArgumentException.ForEmptyString("paramName", "some operation");

        Assert.Contains("paramName", ex.Message);
        Assert.Contains("some operation", ex.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_ForNullArgument_CreatesCorrectMessage()
    {
        var ex = SchemaArgumentException.ForNullArgument("argName", "another operation");

        Assert.Contains("argName", ex.Message);
        Assert.Contains("another operation", ex.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_ForInvalidMethodName_CreatesCorrectMessage()
    {
        var availableTables = "table1, table2";
        var ex = SchemaArgumentException.ForInvalidMethodName("unknownMethod", availableTables);

        Assert.Contains("unknownMethod", ex.Message);
    }

    [TestMethod]
    public void SchemaArgumentException_Constructor_WithInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new SchemaArgumentException("arg", "message", inner);

        Assert.AreEqual(inner, ex.InnerException);
    }

    #endregion

    #region MethodResolutionException Tests

    [TestMethod]
    public void MethodResolutionException_ForUnresolvedMethod_CreatesCorrectMessage()
    {
        var providedTypes = new[] { "Int32", "String" };
        var availableSignatures = new[] { "Method(Int32)", "Method(String)" };

        var ex = MethodResolutionException.ForUnresolvedMethod("TestMethod", providedTypes, availableSignatures);

        Assert.Contains("TestMethod", ex.Message);
        Assert.AreEqual("TestMethod", ex.MethodName);
        Assert.HasCount(2, ex.ProvidedParameterTypes);
        Assert.HasCount(2, ex.AvailableSignatures);
    }

    [TestMethod]
    public void MethodResolutionException_ForUnresolvedMethod_NoParams()
    {
        var providedTypes = Array.Empty<string>();
        var availableSignatures = new[] { "Method()" };

        var ex = MethodResolutionException.ForUnresolvedMethod("TestMethod", providedTypes, availableSignatures);

        Assert.Contains("no parameters", ex.Message);
    }

    [TestMethod]
    public void MethodResolutionException_ForUnresolvedMethod_NoAvailableSignatures()
    {
        var providedTypes = new[] { "Int32" };
        var availableSignatures = Array.Empty<string>();

        var ex = MethodResolutionException.ForUnresolvedMethod("TestMethod", providedTypes, availableSignatures);

        Assert.Contains("No methods available", ex.Message);
    }

    [TestMethod]
    public void MethodResolutionException_ForAmbiguousMethod_CreatesCorrectMessage()
    {
        var providedTypes = new[] { "Int32" };
        var matchingSignatures = new[] { "Method(Int32)", "Method(Object)" };

        var ex = MethodResolutionException.ForAmbiguousMethod("TestMethod", providedTypes, matchingSignatures);

        Assert.Contains("ambiguous", ex.Message);
        Assert.AreEqual("TestMethod", ex.MethodName);
    }

    #endregion

    #region SchemaColumn Tests

    [TestMethod]
    public void SchemaColumn_Constructor_SetsProperties()
    {
        var col = new SchemaColumn("TestColumn", 0, typeof(int));

        Assert.AreEqual("TestColumn", col.ColumnName);
        Assert.AreEqual(0, col.ColumnIndex);
        Assert.AreEqual(typeof(int), col.ColumnType);
    }

    [TestMethod]
    public void SchemaColumn_ConstructorWithIntendedTypeName_SetsProperty()
    {
        var col = new SchemaColumn("TestColumn", 0, typeof(object), "MyNamespace.MyType");

        Assert.AreEqual("MyNamespace.MyType", col.IntendedTypeName);
    }

    [TestMethod]
    public void SchemaColumn_IntendedTypeName_IsNullByDefault()
    {
        var col = new SchemaColumn("TestColumn", 0, typeof(int));

        Assert.IsNull(col.IntendedTypeName);
    }

    #endregion

    #region DataSourceEventArgs Tests

    [TestMethod]
    public void DataSourceEventArgs_Begin_SetsProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source", DataSourcePhase.Begin);

        Assert.AreEqual("queryId", args.QueryId);
        Assert.AreEqual("source", args.DataSourceName);
        Assert.AreEqual(DataSourcePhase.Begin, args.Phase);
    }

    [TestMethod]
    public void DataSourceEventArgs_RowsKnown_SetsProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source", DataSourcePhase.RowsKnown, 100);

        Assert.AreEqual(100, args.TotalRows);
    }

    [TestMethod]
    public void DataSourceEventArgs_RowsRead_SetsProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source", DataSourcePhase.RowsRead, 100, 50);

        Assert.AreEqual(100, args.TotalRows);
        Assert.AreEqual(50, args.RowsProcessed);
    }

    [TestMethod]
    public void DataSourceEventArgs_End_SetsProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source", DataSourcePhase.End, 100, 100);

        Assert.AreEqual(DataSourcePhase.End, args.Phase);
    }

    #endregion

    #region RuntimeContext Tests

    [TestMethod]
    public void RuntimeContext_Constructor_SetsProperties()
    {
        ISchemaColumn[] columns = [new SchemaColumn("col1", 0, typeof(string))];
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null
        );

        Assert.AreEqual("queryId", ctx.QueryId);
        Assert.IsFalse(ctx.EndWorkToken.IsCancellationRequested);
    }

    [TestMethod]
    public void RuntimeContext_AllColumns_ReturnsColumns()
    {
        ISchemaColumn[] columns =
            [new SchemaColumn("Col1", 0, typeof(int)), new SchemaColumn("Col2", 1, typeof(string))];
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null
        );

        Assert.HasCount(2, ctx.AllColumns);
    }

    [TestMethod]
    public void RuntimeContext_EnvironmentVariables_ReturnsDict()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var envVars = new Dictionary<string, string> { { "KEY", "VALUE" } };
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            envVars,
            QuerySourceInfo.Empty,
            null
        );

        Assert.AreEqual("VALUE", ctx.EnvironmentVariables["KEY"]);
    }

    [TestMethod]
    public void RuntimeContext_ReportDataSourceBegin_WithNullCallback_DoesNotThrow()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null
        );


        ctx.ReportDataSourceBegin("testSource");
    }

    [TestMethod]
    public void RuntimeContext_ReportDataSourceRowsKnown_WithNullCallback_DoesNotThrow()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null
        );


        ctx.ReportDataSourceRowsKnown("testSource", 100);
    }

    [TestMethod]
    public void RuntimeContext_ReportDataSourceRowsRead_WithNullCallback_DoesNotThrow()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null
        );


        ctx.ReportDataSourceRowsRead("testSource", 50, 100);
    }

    [TestMethod]
    public void RuntimeContext_ReportDataSourceEnd_WithNullCallback_DoesNotThrow()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null
        );


        ctx.ReportDataSourceEnd("testSource", 100);
    }

    [TestMethod]
    public void RuntimeContext_ReportDataSourceBegin_WithCallback_InvokesCallback()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        DataSourceEventArgs? received = null;
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null,
            (sender, args) => received = args
        );

        ctx.ReportDataSourceBegin("testSource");

        Assert.IsNotNull(received);
        Assert.AreEqual(DataSourcePhase.Begin, received.Phase);
        Assert.AreEqual("testSource", received.DataSourceName);
    }

    [TestMethod]
    public void RuntimeContext_ReportDataSourceRowsKnown_WithCallback_InvokesCallback()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        DataSourceEventArgs? received = null;
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null,
            (sender, args) => received = args
        );

        ctx.ReportDataSourceRowsKnown("testSource", 100);

        Assert.IsNotNull(received);
        Assert.AreEqual(DataSourcePhase.RowsKnown, received.Phase);
        Assert.AreEqual(100L, received.TotalRows);
    }

    [TestMethod]
    public void RuntimeContext_ReportDataSourceRowsRead_WithCallback_InvokesCallback()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        DataSourceEventArgs? received = null;
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null,
            (sender, args) => received = args
        );

        ctx.ReportDataSourceRowsRead("testSource", 50, 100);

        Assert.IsNotNull(received);
        Assert.AreEqual(DataSourcePhase.RowsRead, received.Phase);
        Assert.AreEqual(50L, received.RowsProcessed);
    }

    [TestMethod]
    public void RuntimeContext_ReportDataSourceEnd_WithCallback_InvokesCallback()
    {
        ISchemaColumn[] columns = [new SchemaColumn("Col1", 0, typeof(int))];
        DataSourceEventArgs? received = null;
        var ctx = new RuntimeContext(
            "queryId",
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null,
            (sender, args) => received = args
        );

        ctx.ReportDataSourceEnd("testSource", 100);

        Assert.IsNotNull(received);
        Assert.AreEqual(DataSourcePhase.End, received.Phase);
    }

    #endregion

    #region SchemaColumn Additional Tests

    [TestMethod]
    public void SchemaColumn_Equality_SameValues_AreEqual()
    {
        var col1 = new SchemaColumn("Name", 0, typeof(string));
        var col2 = new SchemaColumn("Name", 0, typeof(string));

        Assert.AreEqual(col1.ColumnName, col2.ColumnName);
        Assert.AreEqual(col1.ColumnIndex, col2.ColumnIndex);
        Assert.AreEqual(col1.ColumnType, col2.ColumnType);
    }

    [TestMethod]
    public void SchemaColumn_DifferentIndex_AreNotEqual()
    {
        var col1 = new SchemaColumn("Name", 0, typeof(string));
        var col2 = new SchemaColumn("Name", 1, typeof(string));

        Assert.AreNotEqual(col1.ColumnIndex, col2.ColumnIndex);
    }

    [TestMethod]
    public void SchemaColumn_DifferentType_AreNotEqual()
    {
        var col1 = new SchemaColumn("Name", 0, typeof(string));
        var col2 = new SchemaColumn("Name", 0, typeof(int));

        Assert.AreNotEqual(col1.ColumnType, col2.ColumnType);
    }

    #endregion

    #region DataSourceEventArgs Tests

    [TestMethod]
    public void DataSourceEventArgs_Constructor_Begin_HasCorrectProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source1", DataSourcePhase.Begin);

        Assert.AreEqual(DataSourcePhase.Begin, args.Phase);
        Assert.AreEqual("source1", args.DataSourceName);
        Assert.AreEqual("queryId", args.QueryId);
        Assert.IsNull(args.RowsProcessed);
    }

    [TestMethod]
    public void DataSourceEventArgs_Constructor_RowsRead_HasCorrectProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source1", DataSourcePhase.RowsRead, 100, 50);

        Assert.AreEqual(DataSourcePhase.RowsRead, args.Phase);
        Assert.AreEqual(50, args.RowsProcessed);
        Assert.AreEqual(100, args.TotalRows);
    }

    [TestMethod]
    public void DataSourceEventArgs_Constructor_End_HasCorrectProperties()
    {
        var args = new DataSourceEventArgs("queryId", "source1", DataSourcePhase.End, rowsProcessed: 100);

        Assert.AreEqual(DataSourcePhase.End, args.Phase);
        Assert.AreEqual(100, args.RowsProcessed);
    }

    [TestMethod]
    public void DataSourceEventArgs_RowsKnown_Phase_Works()
    {
        var args = new DataSourceEventArgs("queryId", "source1", DataSourcePhase.RowsKnown, 500);

        Assert.AreEqual(DataSourcePhase.RowsKnown, args.Phase);
        Assert.AreEqual(500, args.TotalRows);
    }

    #endregion

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
            schema.GetRowSource("", ctx));

        Assert.Contains("empty", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_GetRowSource_ThrowsForNullContext()
    {
        var schema = new TestSchemaWithTable();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.GetRowSource("custom", null!));

        Assert.Contains("null", ex.Message);
    }

    [TestMethod]
    public void SchemaBase_GetRowSource_ThrowsForUnknownSource()
    {
        var schema = new TestSchemaWithTable();
        var ctx = CreateTestRuntimeContext();

        var ex = Assert.Throws<SchemaArgumentException>(() =>
            schema.GetRowSource("unknownsource", ctx));

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
            out var methodInfo);

        Assert.IsFalse(result);
    }

    #endregion

    #region Helper Methods and Classes

    private static RuntimeContext CreateTestRuntimeContext()
    {
        return new RuntimeContext(
            "testQueryId",
            CancellationToken.None,
            Array.Empty<ISchemaColumn>(),
            new Dictionary<string, string>(),
            QuerySourceInfo.Empty,
            null
        );
    }

    private class TestSchemaWithTable : SchemaBase
    {
        public TestSchemaWithTable()
            : base("test", new MethodsAggregator(new MethodsManager()))
        {
            AddTable<SingleRowSchemaTable>("custom");
            AddSource<SingleRowSource>("custom");
        }

        // Expose protected methods for testing
        public void AddTablePublic<T>(string name)
        {
            AddTable<T>(name);
        }

        public void AddSourcePublic<T>(string name, params object[] args)
        {
            AddSource<T>(name, args);
        }
    }

    private class TestSchemaWithEmptyName : SchemaBase
    {
        public TestSchemaWithEmptyName(string name)
            : base(name, new MethodsAggregator(new MethodsManager()))
        {
        }
    }

    private class TestSchemaWithNullAggregator : SchemaBase
    {
        public TestSchemaWithNullAggregator(string name, MethodsAggregator aggregator)
            : base(name, aggregator)
        {
        }
    }

    #endregion
}
