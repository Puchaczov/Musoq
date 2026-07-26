using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DescSourceDispatchTests
{
    [TestMethod]
    public void DescSchema_UsesSchemaWideConstructorOverload()
    {
        var schema = new DispatchProbeSchema();

        var table = Execute("desc #dispatch", schema);

        Assert.AreEqual(1, schema.SchemaWideCalls);
        Assert.IsEmpty(schema.SpecificMethodCalls);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("items", table[0][0]);
    }

    [TestMethod]
    public void DescFunctionsSchema_DoesNotResolveAnEmptySourceMethod()
    {
        var schema = new DispatchProbeSchema();

        var table = Execute("desc functions #dispatch", schema);

        Assert.IsEmpty(schema.SpecificMethodCalls);
        Assert.AreEqual(4, table.Columns.Count());
    }

    [TestMethod]
    public void DescConstructorListing_UsesTheRequestedMethodOnly()
    {
        var schema = new DispatchProbeSchema();

        var table = Execute("desc #dispatch.items", schema);

        CollectionAssert.AreEqual(new[] { "items" }, schema.SpecificMethodCalls);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("items", table[0][0]);
    }

    [TestMethod]
    public void DescTableDescription_RetainsMethodSpecificBinding()
    {
        var schema = new DispatchProbeSchema();

        var table = Execute("desc #dispatch.items()", schema);

        CollectionAssert.AreEqual(new[] { "items" }, schema.SpecificMethodCalls);
        var columnNames = table.Select(row => (string)row[0]).ToArray();
        CollectionAssert.Contains(columnNames, "Name");
        CollectionAssert.Contains(columnNames, "Children");
    }

    [TestMethod]
    public void DescColumnDescription_RetainsMethodSpecificBinding()
    {
        var schema = new DispatchProbeSchema();

        var table = Execute("desc #dispatch.items() column Children", schema);

        CollectionAssert.AreEqual(new[] { "items" }, schema.SpecificMethodCalls);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Children", table[0][0]);
    }

    [TestMethod]
    public void DescSettings_RetainsMethodSpecificBinding()
    {
        var schema = new DispatchProbeSchema();

        var table = Execute("desc settings #dispatch.items()", schema);

        CollectionAssert.AreEqual(new[] { "items" }, schema.SpecificMethodCalls);
        Assert.IsNotNull(table);
    }

    [TestMethod]
    public void DescQuery_RetainsMethodSpecificBindingForInnerSources()
    {
        var schema = new DispatchProbeSchema();

        var table = Execute("desc query (select Name from #dispatch.items())", schema);

        CollectionAssert.AreEqual(new[] { "items" }, schema.SpecificMethodCalls);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("Name", table[0][0]);
    }

    private static Table Execute(string query, DispatchProbeSchema schema)
    {
        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new DispatchProbeSchemaProvider(schema),
            new TestsLoggerResolver());

        return compiled.Run();
    }

    private sealed class DispatchProbeSchemaProvider(DispatchProbeSchema schema) : ISchemaProvider
    {
        public ISchema GetSchema(string name) => schema;
    }

    private sealed class DispatchProbeSchema : SchemaBase
    {
        private static readonly SchemaMethodInfo[] Constructors =
        [
            new("items", ConstructorInfo.Empty())
        ];

        private static readonly ISchemaColumn[] Columns =
        [
            new Musoq.Schema.DataSources.SchemaColumn("Name", 0, typeof(string)),
            new Musoq.Schema.DataSources.SchemaColumn("Children", 1, typeof(int[]))
        ];

        public DispatchProbeSchema()
            : base("dispatch", new MethodsAggregator(new MethodsManager()))
        {
        }

        public int SchemaWideCalls { get; private set; }

        public List<string> SpecificMethodCalls { get; } = [];

        public override SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext)
        {
            SchemaWideCalls += 1;
            return Constructors;
        }

        public override SchemaMethodInfo[] GetRawConstructors(
            string methodName,
            SourceMetadataContext metadataContext)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new InvalidOperationException("The filtered constructor overload must not receive an empty method name.");

            SpecificMethodCalls.Add(methodName);
            return Constructors.Where(constructor => constructor.MethodName == methodName).ToArray();
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters) => new DispatchProbeTable();

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters) => throw new NotSupportedException();

        private sealed class DispatchProbeTable : ISchemaTable
        {
            public ISchemaColumn[] Columns => DispatchProbeSchema.Columns;

            public ISchemaColumn? GetColumnByName(string name) =>
                Columns.SingleOrDefault(column => column.ColumnName == name);

            public ISchemaColumn[] GetColumnsByName(string name) =>
                Columns.Where(column => column.ColumnName == name).ToArray();

            public SchemaTableMetadata Metadata { get; } = new(typeof(object));
        }
    }
}
