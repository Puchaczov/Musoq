using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForInspection_WhenParallelizationModeIsFullForFilteredSingleKeyAggregate_ShouldWarnAndKeepSerialAggregateLoop()
    {
        var result = Inspect(
            "select d.Dummy as Dummy, Count(1) as Count from #system.dual() d where d.Dummy = 'single' group by d.Dummy",
            new CompilationOptions(parallelizationMode: ParallelizationMode.Full));

        var warning = FindFallbackWarning(result, "ParallelSingleKeyAggregate");

        AssertUsesExecutionBackend(result);
        Assert.Contains("ParallelEligibility [ParallelSingleKeyAggregate] PhysicalSingleKeyAggregateNode -> Skipped", result.PlanningText);
        Assert.Contains("Source filter is present", warning.Message);
        Assert.Contains("Serial single-key aggregate execution remains for the candidate.", warning.Message);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ParallelSingleKeyAggregateLoop", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenParallelizationModeIsFullForUnsafeFilterProject_ShouldWarnAndKeepSerialLoop()
    {
        var result = Inspect(
            "select Rand() as Value from #system.dual() d",
            new CompilationOptions(parallelizationMode: ParallelizationMode.Full));

        var warning = FindFallbackWarning(result, "ParallelFilterProject");

        Assert.Contains("ParallelEligibility [ParallelFilterProject] PhysicalProjectNode -> Skipped", result.PlanningText);
        Assert.Contains("non-deterministic method Rand", warning.Message);
        Assert.Contains("Serial filter/project execution remains for the candidate.", warning.Message);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ParallelFilterProjectLoop", StringComparison.Ordinal));
    }

    [TestMethod]
    public void CompileForInspection_WhenParallelizationModeIsNoneForUnsafeFilterProject_ShouldNotWarn()
    {
        var result = Inspect(
            "select Rand() as Value from #system.dual() d",
            new CompilationOptions(parallelizationMode: ParallelizationMode.None));

        Assert.Contains("ParallelEligibility [ParallelFilterProject] PhysicalProjectNode -> Disabled", result.PlanningText);
        AssertNoFallbackWarning(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenParallelizationModeIsFullForEligibleFilterProject_ShouldNotWarn()
    {
        var result = Inspect(
            "select ToUpper(d.Dummy) as Value from #system.dual() d",
            new CompilationOptions(parallelizationMode: ParallelizationMode.Full));

        Assert.Contains("ParallelEligibility [ParallelFilterProject] PhysicalProjectNode -> Enabled", result.PlanningText);
        Assert.Contains("ParallelFilterProjectLoop", result.ExecutionPlanText);
        AssertNoFallbackWarning(result);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteParallelizationIsRequestedForDependentCtes_ShouldWarnAndKeepSerialCtePhases()
    {
        var result = Inspect(
            "with p as (select d.Dummy as Dummy from #system.dual() d), q as (select Dummy from p where Dummy is not null) select Dummy from q",
            new CompilationOptions(useCteParallelization: true));

        var warning = FindFallbackWarning(result, "ParallelCte");

        AssertUsesExecutionBackend(result);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ParallelBlock", StringComparison.Ordinal));
        Assert.Contains("CtePhase [cte0]", result.ExecutionPlanText);
        Assert.Contains("CtePhase [cte1]", result.ExecutionPlanText);
        Assert.Contains("ParallelEligibility [ParallelCte] PhysicalCteNode -> Skipped", result.PlanningText);
        Assert.Contains("Serial CTE execution remains for the candidate.", warning.Message);
    }

    [TestMethod]
    public void CompileForInspection_WhenCteParallelizationIsEnabledForIndependentCtes_ShouldNotWarn()
    {
        var result = Inspect(
            CreateIndependentCteJoinQuery(),
            new CompilationOptions(useCteParallelization: true));

        Assert.Contains("ParallelEligibility [ParallelCte] PhysicalCteNode -> Candidate", result.PlanningText);
        AssertNoFallbackWarning(result, "ParallelCte");
    }

    private static Diagnostic FindFallbackWarning(QueryInspectionResult result, string optimization)
    {
        return result.Warnings.Single(item =>
            item.Code == DiagnosticCode.MQ5012_OptimizationFallback &&
            item.Message.Contains($"Optimization fallback in {optimization}", StringComparison.Ordinal));
    }

    private static void AssertNoFallbackWarning(QueryInspectionResult result)
    {
        Assert.IsFalse(result.Warnings.Any(static item => item.Code == DiagnosticCode.MQ5012_OptimizationFallback));
    }

    private static void AssertNoFallbackWarning(QueryInspectionResult result, string optimization)
    {
        Assert.IsFalse(result.Warnings.Any(item =>
            item.Code == DiagnosticCode.MQ5012_OptimizationFallback &&
            item.Message.Contains($"Optimization fallback in {optimization}", StringComparison.Ordinal)));
    }
}
