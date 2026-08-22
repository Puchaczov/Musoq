using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ApplyPredicateMovementPlanningTextTests
{
    [TestMethod]
    public void PlanningText_WhenCrossApplyHasLeftPredicate_ShouldPlanPreApplyRight()
    {
        var result = Inspect(
            "select b.X from #counting.parents() a cross apply a.Children b where a.Name = 'keep'");

        Assert.Contains(
            "PredicateMovement [ApplyPredicateMovementPlan] Where:PreApplyRight:Apply",
            result.PlanningText);
        Assert.Contains("-> PreApplyRight (High)", result.PlanningText);
        Assert.Contains("before the right side of the CROSS APPLY boundary", result.PlanningText);
    }

    [TestMethod]
    public void PlanningText_WhenChainedCrossApplyHasScopedPredicates_ShouldPlanEachEarliestBoundary()
    {
        var result = Inspect(
            "select c.Value from #counting.parents() a cross apply a.Children b cross apply b.Other c where a.Name = 'keep' and b.X = 1");

        var movementCount = CountOccurrences(result.PlanningText, "[ApplyPredicateMovementPlan]");
        Assert.AreEqual(2, movementCount, result.PlanningText);
        Assert.Contains("a.Name = 'keep'", result.PlanningText);
        Assert.Contains("b.X = 1", result.PlanningText);
        Assert.Contains("-> PreApplyRight (High)", result.PlanningText);
    }

    [TestMethod]
    public void PlanningText_WhenApplyPredicateIsOr_ShouldRetainTheOrResidual()
    {
        var result = Inspect(
            "select c.Value from #counting.parents() a cross apply a.Children b cross apply b.Other c where a.Name = 'keep' and (a.Name = 'other' or b.X = 1)");

        Assert.Contains("OR predicates are kept intact", result.PlanningText);
        Assert.Contains("-> RetainedResidual", result.PlanningText);
        Assert.Contains("a.Name = 'keep'", result.PlanningText);
    }

    [TestMethod]
    public void PlanningText_WhenPredicateReferencesFutureApplyAlias_ShouldRetainItAsResidual()
    {
        var result = Inspect(
            "select c.Value from #counting.parents() a cross apply a.Children b cross apply b.Other c where c.Value = 10");

        Assert.Contains("right-side or future APPLY alias", result.PlanningText);
        Assert.Contains("-> RetainedResidual", result.PlanningText);
    }

    [TestMethod]
    public void PlanningText_WhenOuterApplyPredicateReferencesLeftScope_ShouldPlanOnlyLeftGuard()
    {
        var result = Inspect(
            "select b.X from #counting.parents() a outer apply a.Children b where a.Name = 'empty' and b.X = 1");

        Assert.Contains("before the right side of the OUTER APPLY boundary", result.PlanningText);
        Assert.Contains("right-side or future APPLY alias", result.PlanningText);
    }

    [TestMethod]
    public void PlanningText_WhenPredicateIsNondeterministic_ShouldRetainItAsResidual()
    {
        var result = Inspect(
            "select b.X from #counting.parents() a cross apply a.Children b where a.Name = ToString(Rand())");

        Assert.Contains("non-deterministic", result.PlanningText);
        Assert.Contains("-> RetainedResidual", result.PlanningText);
    }

    private static QueryInspectionResult Inspect(string query)
    {
        var fixture = ApplyTraversalFixture.Create();
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            new GenericSchemaProvider(new Dictionary<string, ISchema>
            {
                ["#counting"] = fixture.Schema
            }),
            new TestsLoggerResolver());
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }
}
