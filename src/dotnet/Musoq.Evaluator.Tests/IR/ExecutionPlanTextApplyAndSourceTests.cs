using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionPlanTextApplyAndSourceTests
{
    private readonly PlanTextBuildHarness _buildHarness = new();

    [TestMethod]
    public void Print_WhenCrossApplyAccessMethodQuery_ShouldUseExecutionBackend()
    {
        var buildItems = _buildHarness.BuildForThreeSources(
            "select b.Value from #schema.first() a cross apply a.JustReturnArrayOfString() b",
            [new CrossApplyUnusedAliasTests.CrossApplyClass1 { City = "City1", Country = "Country1", Population = 100 }],
            Array.Empty<CrossApplyUnusedAliasTests.CrossApplyClass2>(),
            Array.Empty<CrossApplyUnusedAliasTests.CrossApplyClass3>());

        Assert.Contains("ExecutionPlan [compiled]", buildItems.RequireExecutionPlanText());
        Assert.IsFalse(buildItems.RequireExecutionPlanText().Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
        Assert.Contains("EnumerableSource [JustReturnArrayOfString() ->", buildItems.RequireExecutionPlanText());
        Assert.Contains("ForEach [b in", buildItems.RequireExecutionPlanText());
    }

    [TestMethod]
    public void Print_WhenOuterApplyAccessMethodQuery_ShouldUseExecutionBackend()
    {
        var buildItems = _buildHarness.BuildForThreeSources(
            "select b.Value from #schema.first() a outer apply a.JustReturnArrayOfString() b",
            [new CrossApplyUnusedAliasTests.CrossApplyClass1 { City = "City1", Country = "Country1", Population = 100 }],
            Array.Empty<CrossApplyUnusedAliasTests.CrossApplyClass2>(),
            Array.Empty<CrossApplyUnusedAliasTests.CrossApplyClass3>());

        Assert.Contains("ExecutionPlan [compiled]", buildItems.RequireExecutionPlanText());
        Assert.IsFalse(buildItems.RequireExecutionPlanText().Contains("ExecutionPlanUnsupported", StringComparison.Ordinal));
        Assert.Contains("EnumerableSource [JustReturnArrayOfString() ->", buildItems.RequireExecutionPlanText());
        Assert.Contains("Assign [bHasMatch = TRUE]", buildItems.RequireExecutionPlanText());
        Assert.Contains("If [NOT bHasMatch]", buildItems.RequireExecutionPlanText());
        Assert.Contains("b.Value: NULL", buildItems.RequireExecutionPlanText());
    }
}
