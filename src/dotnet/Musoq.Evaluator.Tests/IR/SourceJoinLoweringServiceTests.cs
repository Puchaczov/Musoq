using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SourceJoinLoweringServiceTests
{
    [TestMethod]
    public void ApplyChainSourceCollector_WhenCrossApplyChainContainsOrdinality_ShouldCollectSourcesInExecutionOrder()
    {
        var left = CreateScan("left");
        var middle = CreateScan("middle");
        var right = CreateScan("right");
        var nested = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, left, middle);
        var root = new PhysicalNestedLoopApplyNode(ApplyKind.Cross, nested, right, WithOrdinality: true);

        var collector = new ApplyChainSourceCollector();
        var supported = collector.TryCollectCrossApplySources(root, out var sources);

        Assert.IsTrue(supported);
        Assert.AreEqual(3, sources.Count);
        Assert.AreSame(left, sources[0].Source);
        Assert.IsFalse(sources[0].WithOrdinality);
        Assert.AreSame(middle, sources[1].Source);
        Assert.IsFalse(sources[1].WithOrdinality);
        Assert.AreSame(right, sources[2].Source);
        Assert.IsTrue(sources[2].WithOrdinality);
    }

    [TestMethod]
    public void ApplyChainSourceCollector_WhenChainContainsOuterApply_ShouldRejectChain()
    {
        var left = CreateScan("left");
        var right = CreateScan("right");
        var root = new PhysicalNestedLoopApplyNode(ApplyKind.Outer, left, right);

        var collector = new ApplyChainSourceCollector();
        var supported = collector.TryCollectCrossApplySources(root, out var sources);

        Assert.IsFalse(supported);
        Assert.AreEqual(0, sources.Count);
    }

    [TestMethod]
    public void JoinSourceLookupBuilder_ShouldCloneExtendAndRejectDuplicateAliasesCaseInsensitively()
    {
        var source = new SourceEntityShape("s", typeof(object), []);
        var duplicate = new SourceEntityShape("S", typeof(object), []);
        var next = new SourceEntityShape("next", typeof(object), []);
        var lookup = new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(JoinSourceLookupBuilder.TryAdd(lookup, source));
        Assert.IsFalse(JoinSourceLookupBuilder.TryAdd(lookup, duplicate));

        var clone = JoinSourceLookupBuilder.Clone(lookup);
        var extended = JoinSourceLookupBuilder.Extend(clone, next);

        Assert.AreEqual(1, lookup.Count);
        Assert.AreEqual(1, clone.Count);
        Assert.AreEqual(2, extended.Count);
        Assert.AreSame(source, lookup["s"]);
        Assert.AreSame(source, clone["s"]);
        Assert.AreSame(next, extended["next"]);
    }

    private static PhysicalSchemaScanNode CreateScan(string alias)
    {
        return new PhysicalSchemaScanNode(
            "schema",
            "items",
            [],
            alias,
            [],
            [],
            new OutputSchema([new ColumnSchema("Id", typeof(int), 0)]));
    }
}
