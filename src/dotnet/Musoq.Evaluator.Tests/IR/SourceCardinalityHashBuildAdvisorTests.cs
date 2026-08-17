using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning.Cardinality;
using Musoq.Evaluator.IR.Planning.SourcePlanning;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SourceCardinalityHashBuildAdvisorTests
{
    [TestMethod]
    public void ExactCardinality_WhenLeftClearlySmaller_ShouldChooseLeft()
    {
        var chosen = SourceCardinalityHashBuildAdvisor.TryChooseBuildSide(
            JoinKind.Inner,
            Scan("l", "left"),
            Scan("r", "right"),
            Facts(CardinalityEstimate.Exact(10), CardinalityEstimate.Exact(100)),
            out var buildOnLeft,
            out var reason);

        Assert.IsTrue(chosen);
        Assert.IsTrue(buildOnLeft);
        Assert.Contains("left source", reason);
    }

    [TestMethod]
    public void ExactCardinality_WhenRightClearlySmaller_ShouldChooseRight()
    {
        var chosen = SourceCardinalityHashBuildAdvisor.TryChooseBuildSide(
            JoinKind.Inner,
            Scan("l", "left"),
            Scan("r", "right"),
            Facts(CardinalityEstimate.Exact(100), CardinalityEstimate.Exact(10)),
            out var buildOnLeft,
            out var reason);

        Assert.IsTrue(chosen);
        Assert.IsFalse(buildOnLeft);
        Assert.Contains("right source", reason);
    }

    [TestMethod]
    public void BoundedHighConfidenceCardinality_ShouldBeComparable()
    {
        var chosen = SourceCardinalityHashBuildAdvisor.TryChooseBuildSide(
            JoinKind.Inner,
            Scan("l", "left"),
            Scan("r", "right"),
            Facts(CardinalityEstimate.Bounded(0, 10, 0.8d), CardinalityEstimate.Exact(100)),
            out var buildOnLeft,
            out _);

        Assert.IsTrue(chosen);
        Assert.IsTrue(buildOnLeft);
    }

    [TestMethod]
    public void UnknownOrLowConfidenceCardinality_ShouldBeIgnored()
    {
        var chosen = SourceCardinalityHashBuildAdvisor.TryChooseBuildSide(
            JoinKind.Inner,
            Scan("l", "left"),
            Scan("r", "right"),
            Facts(CardinalityEstimate.Bounded(0, 10, 0.25d), CardinalityEstimate.Unknown()),
            out _,
            out var reason);

        Assert.IsFalse(chosen);
        Assert.Contains("too low-confidence", reason);
    }

    [TestMethod]
    public void NonSimpleScope_ShouldBeIgnored()
    {
        var chosen = SourceCardinalityHashBuildAdvisor.TryChooseBuildSide(
            JoinKind.Inner,
            new PhysicalCteRefNode("items", "l", Schema()),
            Scan("r", "right"),
            Facts(CardinalityEstimate.Exact(10), CardinalityEstimate.Exact(100)),
            out _,
            out var reason);

        Assert.IsFalse(chosen);
        Assert.Contains("simple source-scan", reason);
    }

    [TestMethod]
    public void UnsupportedJoinKind_ShouldBeIgnored()
    {
        var chosen = SourceCardinalityHashBuildAdvisor.TryChooseBuildSide(
            JoinKind.LeftOuter,
            Scan("l", "left"),
            Scan("r", "right"),
            Facts(CardinalityEstimate.Exact(10), CardinalityEstimate.Exact(100)),
            out _,
            out var reason);

        Assert.IsFalse(chosen);
        Assert.Contains("inner joins", reason);
    }

    private static CardinalityFact[] Facts(
        CardinalityEstimate leftCardinality,
        CardinalityEstimate rightCardinality)
    {
        return
        [
            Fact("left", leftCardinality),
            Fact("right", rightCardinality)
        ];
    }

    private static CardinalityFact Fact(string sourceContextId, CardinalityEstimate cardinality)
    {
        return new CardinalityFact(
            $"source:{sourceContextId}",
            "SourceEstimate",
            cardinality.Kind,
            cardinality.ExactRows,
            cardinality.LowerBound,
            cardinality.UpperBound,
            cardinality.Confidence,
            cardinality.Reason ?? "test");
    }

    private static PhysicalSchemaScanNode Scan(string alias, string sourceContextId)
    {
        return new PhysicalSchemaScanNode("#sp", "items", [], alias, [], [], Schema(), sourceContextId);
    }

    private static OutputSchema Schema()
    {
        return new OutputSchema([new ColumnSchema("Id", typeof(int), 0)]);
    }
}
