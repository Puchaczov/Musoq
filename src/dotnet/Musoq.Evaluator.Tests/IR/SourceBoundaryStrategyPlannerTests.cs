using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SourceBoundaryStrategyPlannerTests
{
    [TestMethod]
    public void Plan_WhenBoundaryIsCorrelated_ShouldRequirePerRow()
    {
        var boundary = CreateBoundary(SourceBoundaryKind.PropertySource, ApplyKind.Outer, SourceBoundaryInputMode.Correlated);

        var result = SourceBoundaryStrategyPlanner.Plan([boundary]);

        var plan = result.Plans.Single();
        Assert.AreEqual(SourceBoundaryStrategyKind.PerRowRequired, plan.Strategy);
        Assert.AreEqual(SourceBoundaryCachingDecision.NotApplied, plan.CachingDecision);
        Assert.Contains("must stay per-row", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenBoundaryIsIndependent_ShouldMarkPerQueryCandidateWithoutCaching()
    {
        var boundary = CreateBoundary(SourceBoundaryKind.InterpretSource, ApplyKind.Cross, SourceBoundaryInputMode.Independent);

        var result = SourceBoundaryStrategyPlanner.Plan([boundary]);

        var plan = result.Plans.Single();
        Assert.AreEqual(SourceBoundaryStrategyKind.PerQueryCandidateNotApplied, plan.Strategy);
        Assert.AreEqual(SourceBoundaryCachingDecision.NotApplied, plan.CachingDecision);
        Assert.Contains("only a per-query candidate", plan.Reason);
    }

    [TestMethod]
    public void Plan_WhenBoundaryShapeIsUnknown_ShouldKeepConservativeStrategy()
    {
        var boundary = CreateBoundary(SourceBoundaryKind.AccessMethodSource, ApplyKind.Cross, SourceBoundaryInputMode.Unknown);

        var result = SourceBoundaryStrategyPlanner.Plan([boundary]);

        var plan = result.Plans.Single();
        Assert.AreEqual(SourceBoundaryStrategyKind.UnknownBoundary, plan.Strategy);
        Assert.AreEqual(SourceBoundaryCachingDecision.NotApplied, plan.CachingDecision);
        Assert.Contains("applies no caching", plan.Reason);
    }

    private static SourceBoundaryPlan CreateBoundary(
        SourceBoundaryKind kind,
        ApplyKind applyKind,
        SourceBoundaryInputMode inputMode)
    {
        return new SourceBoundaryPlan(
            $"{kind}:test",
            kind,
            applyKind,
            inputMode,
            ResolveInvocationShape(inputMode),
            SourceRowBehavior.RowMultiplying,
            SourceResultShape.Declared,
            ResolveCacheability(inputMode),
            PlanningConfidence.High,
            "target",
            ["input"],
            ["output"],
            PlanningConfidence.High,
            "reason");
    }

    private static SourceInvocationShape ResolveInvocationShape(SourceBoundaryInputMode inputMode)
    {
        return inputMode switch
        {
            SourceBoundaryInputMode.Independent => SourceInvocationShape.PerQuery,
            SourceBoundaryInputMode.Correlated => SourceInvocationShape.PerRow,
            _ => SourceInvocationShape.Unknown
        };
    }

    private static SourceCacheability ResolveCacheability(SourceBoundaryInputMode inputMode)
    {
        return inputMode switch
        {
            SourceBoundaryInputMode.Independent => SourceCacheability.CacheCandidate,
            SourceBoundaryInputMode.Correlated => SourceCacheability.NotCacheable,
            _ => SourceCacheability.Unknown
        };
    }
}
