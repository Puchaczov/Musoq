using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticExceptionTaxonomyTests
{
    [TestMethod]
    public void QueryAnalyzer_WhenProviderFails_ShouldPreserveProviderException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new QueryAnalyzer(new ThrowingSchemaProvider("provider failed")).Analyze(
                "SELECT 1 FROM #A.Entities()"));

        Assert.AreEqual("provider failed", exception.Message);
    }

    [TestMethod]
    public void QueryAnalyzer_WhenProviderCancels_ShouldRethrowCancellation()
    {
        Assert.Throws<OperationCanceledException>(() =>
            new QueryAnalyzer(new CancellingSchemaProvider()).Analyze(
                "SELECT 1 FROM #A.Entities()"));
    }

    [TestMethod]
    public void QueryAnalyzer_WhenProviderTableLookupFailsWithNotSupported_ShouldPreserveProviderException()
    {
        var exception = Assert.Throws<NotSupportedException>(() =>
            new QueryAnalyzer(new ThrowingTableSchemaProvider(new NotSupportedException("provider table lookup failed"))).Analyze(
                "SELECT 1 FROM #A.Entities()"));

        Assert.AreEqual("provider table lookup failed", exception.Message);
    }

    [TestMethod]
    public void QueryAnalyzer_WhenProviderTableLookupFailsWithKeyNotFound_ShouldPreserveProviderException()
    {
        var exception = Assert.Throws<KeyNotFoundException>(() =>
            new QueryAnalyzer(new ThrowingTableSchemaProvider(new KeyNotFoundException("provider table lookup failed"))).Analyze(
                "SELECT 1 FROM #A.Entities()"));

        Assert.AreEqual("provider table lookup failed", exception.Message);
    }

    [TestMethod]
    public void QueryAnalyzer_WhenProviderColumnMetadataFails_ShouldPreserveProviderException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new QueryAnalyzer(new ThrowingColumnSchemaProvider()).Analyze(
                "SELECT a.Value FROM #A.Entities() a"));

        Assert.AreEqual("provider column metadata failed", exception.Message);
    }

    [TestMethod]
    public void SemanticAnalysisException_ShouldExposeItsPrimaryDiagnostic()
    {
        var expected = Musoq.Parser.Diagnostics.Diagnostic.Error(
            Musoq.Parser.Diagnostics.DiagnosticCode.MQ3001_UnknownColumn,
            "missing",
            Musoq.Parser.TextSpan.Empty);
        var exception = new SemanticAnalysisException("semantic failed", expected);

        Assert.AreEqual(expected.Code, exception.Code);
        Assert.AreEqual(expected, exception.ToDiagnostic());
    }

    [TestMethod]
    public void DynamicAccess_WhenGetterThrowsUnexpectedException_ShouldPreserveIt()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            EvaluationHelper.GetNestedValue(new ThrowingDynamicObject(), "Value"));

        Assert.AreEqual("getter failed", exception.Message);
    }

    private sealed class ThrowingSchemaProvider(string message) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class CancellingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new OperationCanceledException("cancelled");
        }
    }

    private sealed class ThrowingTableSchemaProvider(Exception exception) : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new ThrowingTableSchema(exception);
    }

    private sealed class ThrowingTableSchema(Exception exception)
        : SchemaBase("A", new MethodsAggregator(new MethodsManager()))
    {
        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters) => throw exception;

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters) => throw new NotSupportedException();
    }

    private sealed class ThrowingColumnSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => new ThrowingColumnSchema();
    }

    private sealed class ThrowingColumnSchema : SchemaBase
    {
        public ThrowingColumnSchema()
            : base("A", new MethodsAggregator(new MethodsManager()))
        {
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters) => new ThrowingColumnTable();

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters) => throw new NotSupportedException();
    }

    private sealed class ThrowingColumnTable : ISchemaTable
    {
        public ISchemaColumn[] Columns => [new SchemaColumn("Value", 0, typeof(int))];

        public ISchemaColumn? GetColumnByName(string name) =>
            throw new InvalidOperationException("provider column metadata failed");

        public ISchemaColumn[] GetColumnsByName(string name) => [];

        public SchemaTableMetadata Metadata { get; } = new(typeof(object));
    }

    private sealed class ThrowingDynamicObject : DynamicObject
    {
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            throw new InvalidOperationException("getter failed");
        }
    }
}
