using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ExecutionArtifactFreezeTests
{
    [TestMethod]
    public void ExecutionPlanAndNodes_ShouldFreezeInputCollections()
    {
        var nodes = new List<ExecutionNode> { new ExecutionBlockNode() };
        var block = new ExecutionBlock(nodes);
        var shapes = new List<RowShape> { new GeneratedRowShape("row", []) };
        var plan = new ExecutionPlan("test", shapes, block);

        nodes.Clear();
        shapes.Clear();

        Assert.HasCount(1, block.Nodes);
        Assert.HasCount(1, plan.Shapes);
        Assert.Throws<NotSupportedException>(() => ((IList<ExecutionNode>)block.Nodes).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<RowShape>)plan.Shapes).Clear());
    }

    [TestMethod]
    public void SourceExecutionPlan_ShouldDeepCopyMetadataBoundaries()
    {
        var columns = new List<SourceColumnRef> { new("id") };
        var orderBy = new List<OrderByExpression> { new(new SourceColumnRef("id"), OrderDirection.Ascending) };
        var nested = new Dictionary<string, object?> { ["values"] = new[] { "one" } };
        var properties = new Dictionary<string, object?> { ["nested"] = nested };
        var plan = new SourceExecutionPlan
        {
            Identity = new SourceIdentity("schema", "method", "context", "alias"),
            AcceptedColumns = columns,
            AcceptedOrderBy = orderBy,
            Properties = properties
        };

        columns.Clear();
        orderBy.Clear();
        nested["values"] = new[] { "changed" };
        properties["new"] = true;

        Assert.HasCount(1, plan.AcceptedColumns);
        Assert.HasCount(1, plan.AcceptedOrderBy);
        var copiedNested = (IReadOnlyDictionary<string, object?>)plan.Properties["nested"]!;
        CollectionAssert.AreEqual(new[] { "one" }, (string[])copiedNested["values"]!);
        Assert.IsFalse(plan.Properties.ContainsKey("new"));
    }

    [TestMethod]
    public void SourcePredicateIn_ShouldFreezeValues()
    {
        var values = new List<SourcePredicateExpression> { new SourcePredicateLiteral(1) };
        var predicate = new SourcePredicateIn(new SourcePredicateColumn(new SourceColumnRef("id")), values);

        values.Clear();

        Assert.HasCount(1, predicate.Values);
    }

    private sealed record ExecutionBlockNode : ExecutionNode;
}
