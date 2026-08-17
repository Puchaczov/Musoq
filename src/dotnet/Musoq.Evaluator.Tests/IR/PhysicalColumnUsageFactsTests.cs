using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class PhysicalColumnUsageFactsTests
{
    [TestMethod]
    public void ContainsColumn_WhenColumnIsAliasQualified_ShouldMatchExactNameAndUnqualifiedTail()
    {
        var columns = new[] { "person.Name" };

        Assert.IsTrue(PhysicalColumnUsageFacts.ContainsColumn(columns, "person.Name"));
        Assert.IsTrue(PhysicalColumnUsageFacts.ContainsColumn(columns, "Name"));
    }

    [TestMethod]
    public void HasAmbiguousOutputNames_WhenNamesDifferOnlyByCase_ShouldReportAmbiguity()
    {
        var fields = new[]
        {
            new ProjectedField("Name", new ColumnRef("person", "Name", typeof(string)), 0),
            new ProjectedField("name", new ColumnRef("person", "Name", typeof(string)), 1)
        };

        Assert.IsTrue(PhysicalColumnUsageFacts.HasAmbiguousOutputNames(fields));
    }

    [TestMethod]
    public void TrySelectSetOperationRetainedIndexes_WhenUnionComparerUsesUnselectedColumn_ShouldKeepComparerIndex()
    {
        var left = CreateValuesScan(
            "left",
            ("City", typeof(string)),
            ("Comparer", typeof(int)),
            ("Payload", typeof(string)));
        var right = CreateValuesScan(
            "right",
            ("City", typeof(string)),
            ("Comparer", typeof(int)),
            ("Payload", typeof(string)));
        var setOperation = new PhysicalSetOperationNode(
            SetOpKind.Union,
            left,
            right,
            [1],
            [typeof(int)]);
        var requiredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "City" };

        var selected = PhysicalColumnUsageFacts.TrySelectSetOperationRetainedIndexes(
            setOperation,
            requiredNames,
            out var retainedIndexes);

        Assert.IsTrue(selected);
        CollectionAssert.AreEqual(new[] { 0, 1 }, retainedIndexes);
    }

    [TestMethod]
    public void ResolveAvailableColumnNames_WhenInputIsSchemaScan_ShouldReturnAliasQualifiedColumns()
    {
        var scan = new PhysicalSchemaScanNode(
            "test",
            "items",
            [],
            "person",
            [],
            [],
            CreateSchema(("Name", typeof(string)), ("Age", typeof(int))));

        var columns = PhysicalColumnUsageFacts.ResolveAvailableColumnNames(scan);

        CollectionAssert.AreEqual(new[] { "person.Name", "person.Age" }, columns);
    }

    private static PhysicalValuesScanNode CreateValuesScan(
        string alias,
        params (string Name, Type Type)[] columns)
    {
        return new PhysicalValuesScanNode(alias, [], CreateSchema(columns));
    }

    private static OutputSchema CreateSchema(params (string Name, Type Type)[] columns)
    {
        var schemaColumns = new ColumnSchema[columns.Length];

        for (var index = 0; index < columns.Length; index++)
        {
            schemaColumns[index] = new ColumnSchema(
                columns[index].Name,
                columns[index].Type,
                index);
        }

        return new OutputSchema(schemaColumns);
    }
}
