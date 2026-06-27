using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Managers;

namespace Musoq.Schema.Tests;

/// <summary>
///     Extended tests for Schema module to improve coverage
/// </summary>
[TestClass]
public partial class SchemaExtendedTests
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
    #region Helper Methods and Classes

    private static SourceExecutionContext CreateTestRuntimeContext()
    {
        return new SourceExecutionContext(
            "testQueryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            Array.Empty<ISchemaColumn>(),
            new Dictionary<string, string>(),
            NullLogger.Instance
        );
    }

    private sealed class TestSchemaWithTable : SchemaBase
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

    private sealed class TestSchemaWithEmptyName(string name) : SchemaBase(name, new MethodsAggregator(new MethodsManager()));

    private sealed class TestSchemaWithNullAggregator(string name, MethodsAggregator aggregator) : SchemaBase(name, aggregator);

    #endregion
}
