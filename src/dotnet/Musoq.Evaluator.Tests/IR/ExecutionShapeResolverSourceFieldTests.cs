using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionShapeResolverSourceFieldTests
{
    [TestMethod]
    public void ResolveSourceShape_WhenDynamicPhysicalIndicesRepeat_ShouldUseDenseSlotsAndPreserveNames()
    {
        const string alias = "p";
        var columns = new ISchemaColumn[]
        {
            new SchemaColumn("Who", 7, typeof(string)),
            new SchemaColumn("Age", 7, typeof(int))
        };
        var resolver = CreateResolver(alias, typeof(IReadOnlyDictionary<string, object>), columns);

        var shape = Assert.IsInstanceOfType<ExpandoAdapterShape>(resolver.ResolveSourceShape(CreateScan(alias)));

        CollectionAssert.AreEqual(new[] { 0, 1 }, shape.Fields.Select(static field => field.OutputIndex).ToArray());
        CollectionAssert.AreEqual(new[] { "Who", "Age" }, shape.Fields.Select(static field => field.Name).ToArray());
        CollectionAssert.AreEqual(
            new[] { "p.Who", "p.Age" },
            shape.Fields.Select(static field => field.QualifiedName).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Who", "Age" },
            shape.Fields
                .Select(static field => Assert.IsInstanceOfType<ExpandoDictionaryAccess>(field.AccessStrategy).Key)
                .ToArray());
    }

    [TestMethod]
    public void ResolveSourceShape_WhenPositionalMetadataIsSparseAndOutOfOrder_ShouldSeparateSlotsFromPhysicalIndices()
    {
        const string alias = "row";
        var columns = new ISchemaColumn[]
        {
            new SchemaColumn("Who", 4, typeof(string)),
            new SchemaColumn("Age", 1, typeof(int))
        };
        var resolver = CreateResolver(alias, typeof(object[]), columns);

        var shape = Assert.IsInstanceOfType<SourceEntityShape>(resolver.ResolveSourceShape(CreateScan(alias)));

        CollectionAssert.AreEqual(new[] { 0, 1 }, shape.Fields.Select(static field => field.OutputIndex).ToArray());
        CollectionAssert.AreEqual(
            new[] { 4, 1 },
            shape.Fields
                .Select(static field => Assert.IsInstanceOfType<PositionalAccess>(field.AccessStrategy).Index)
                .ToArray());
        CollectionAssert.AreEqual(
            new[] { "Who", "Age" },
            shape.Fields.Select(static field => field.Name).ToArray());
    }

    [TestMethod]
    public void ResolveSourceShape_WhenProjectedColumnsAreReordered_ShouldKeepDenseShapeOrderAndNames()
    {
        const string alias = "projected";
        var columns = new ISchemaColumn[]
        {
            new SchemaColumn("Age", 7, typeof(int)),
            new SchemaColumn("Who", 7, typeof(string))
        };
        var resolver = CreateResolver(alias, typeof(IReadOnlyDictionary<string, object>), columns);

        var shape = Assert.IsInstanceOfType<ExpandoAdapterShape>(resolver.ResolveSourceShape(CreateScan(alias, ["Age", "Who"])));

        CollectionAssert.AreEqual(new[] { 0, 1 }, shape.Fields.Select(static field => field.OutputIndex).ToArray());
        CollectionAssert.AreEqual(new[] { "Age", "Who" }, shape.Fields.Select(static field => field.Name).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Age", "Who" },
            shape.Fields
                .Select(static field => Assert.IsInstanceOfType<ExpandoDictionaryAccess>(field.AccessStrategy).Key)
                .ToArray());
    }

    private static ExecutionShapeResolver CreateResolver(
        string alias,
        Type entityType,
        IReadOnlyList<ISchemaColumn> columns)
    {
        return new ExecutionShapeResolver(
            inferredColumns: new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal)
            {
                [alias] = columns.ToArray()
            },
            entityTypesByAlias: new Dictionary<string, Type>(StringComparer.Ordinal)
            {
                [alias] = entityType
            });
    }

    private static PhysicalSchemaScanNode CreateScan(string alias, string[]? projectedColumns = null)
    {
        return new PhysicalSchemaScanNode("dynamic", "all", [], alias, [], projectedColumns ?? [], OutputSchema.Empty);
    }
}
