using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void RecursiveNullableAndCastSamples_ShouldKeepAnchorDerivedTypedRows()
    {
        var nullableExecution = ReadExecutionPlan("Q215_RecursiveNullableColumns.cs");
        var nullableCode = ReadGeneratedCode("Q215_RecursiveNullableColumns.cs");
        var decimalExecution = ReadExecutionPlan("Q216_RecursiveExplicitDecimalCast.cs");
        var decimalCode = ReadGeneratedCode("Q216_RecursiveExplicitDecimalCast.cs");

        Assert.Contains("ParentId: int? <- field ParentId", nullableExecution);
        Assert.Contains("var cte0Seen = new HashSet<int>();", nullableCode);
        Assert.Contains("Total: decimal? <- field Total", decimalExecution);
        Assert.Contains("StrictCastRuntime.ToDecimal(0)", decimalCode);
        Assert.Contains("(decimal?)(decimal)(t.Total + 1)", decimalCode);
        Assert.IsFalse(nullableCode.Contains("HashSet<object", StringComparison.Ordinal));
        Assert.IsFalse(decimalCode.Contains("HashSet<object", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RecursiveCaseAndWideSamples_ShouldEmitDirectTypedCandidates()
    {
        var scalarExecution = ReadExecutionPlan("Q217_RecursiveCaseAndScalarExpressions.cs");
        var scalarCode = ReadGeneratedCode("Q217_RecursiveCaseAndScalarExpressions.cs");
        var wideExecution = ReadExecutionPlan("Q218_RecursiveWidePayload.cs");
        var wideCode = ReadGeneratedCode("Q218_RecursiveWidePayload.cs");

        Assert.Contains("Label: CASE WHEN", scalarExecution);
        Assert.Contains("(l.Value == 1) ? (string)\"even\" : (string)\"odd\"", scalarCode);
        Assert.Contains("Amount: decimal? <- field Amount", wideExecution);
        Assert.AreEqual(12, CountText(wideCode, "var __cte0CurrentFrontierCandidate"));
        Assert.AreEqual(12, CountText(wideCode, "var __cte0NextFrontierCandidate"));
        Assert.Contains("var cte0Seen = new HashSet<int>();", wideCode);
        Assert.IsFalse(wideCode.Contains("cte0Seen.Add(new object", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RecursiveLimitSamples_ShouldEmbedEffectiveLimitsInPlanAndGuards()
    {
        AssertRecursiveLimits("Q219_RecursiveLimitDefaultCodeShape.cs", 1_000, 10_000_000, 10_000_000);
        AssertRecursiveLimits("Q220_RecursiveLimitOverrideCodeShape.cs", 7, 25, 10_000_000);
    }

    [TestMethod]
    public void RecursiveSidecarDisabledSample_ShouldHoistTypedHashWithoutSidecars()
    {
        var execution = ReadExecutionPlan("Q221_RecursiveSidecarDisabled.cs");
        var code = ReadGeneratedCode("Q221_RecursiveSidecarDisabled.cs");
        var setupIndex = execution.IndexOf("InvariantSetup", StringComparison.Ordinal);
        var hashPlanIndex = execution.IndexOf("CreateHash [cte1Invariant0Hash", StringComparison.Ordinal);
        var memberIndex = execution.IndexOf("RecursiveMember", setupIndex, StringComparison.Ordinal);
        var hashCodeIndex = code.IndexOf(
            "var cte1Invariant0Hash = new Dictionary<int, HashJoinBucket<Cte1Invariant0Row0>>",
            StringComparison.Ordinal);
        var loopIndex = code.IndexOf("while (cte1CurrentFrontier.Count > 0)", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, setupIndex);
        Assert.IsGreaterThan(setupIndex, hashPlanIndex);
        Assert.IsGreaterThan(hashPlanIndex, memberIndex);
        Assert.IsGreaterThanOrEqualTo(0, hashCodeIndex);
        Assert.IsGreaterThan(hashCodeIndex, loopIndex);
        Assert.AreEqual(1, CountText(code, "var cte1Invariant0Hash = new Dictionary<"));
        Assert.IsFalse(execution.Contains("StoreCteIndex", StringComparison.Ordinal));
        Assert.IsFalse(execution.Contains("LoadCteIndex", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("_cteIndexResults", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("_tableResults[", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RecursiveParallelSiblingSample_ShouldParallelizeOnlyOrdinaryLevel()
    {
        var execution = ReadExecutionPlan("Q222_RecursiveCteParallelSiblings.cs");
        var code = ReadGeneratedCode("Q222_RecursiveCteParallelSiblings.cs");
        var parallelIndex = execution.IndexOf("ParallelBlock [cte-level-0, tasks 2, maxDegree 2]", StringComparison.Ordinal);
        var recursiveIndex = execution.IndexOf("RecursiveCte [reachable;", StringComparison.Ordinal);
        var invokeIndex = code.IndexOf("Parallel.Invoke(", StringComparison.Ordinal);
        var loopIndex = code.IndexOf("while (cte2CurrentFrontier.Count > 0)", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, parallelIndex);
        Assert.IsGreaterThan(parallelIndex, recursiveIndex);
        Assert.IsGreaterThanOrEqualTo(0, invokeIndex);
        Assert.IsGreaterThan(invokeIndex, loopIndex);
        Assert.AreEqual(1, CountText(code, "while (cte2CurrentFrontier.Count > 0)"));
        Assert.IsFalse(
            execution[parallelIndex..recursiveIndex].Contains("RecursiveCte [", StringComparison.Ordinal));
    }

    private static void AssertRecursiveLimits(
        string fileName,
        int maxIterations,
        int maxRows,
        int maxSnapshotRows)
    {
        var execution = ReadExecutionPlan(fileName);
        var code = ReadGeneratedCode(fileName);

        Assert.Contains(
            $"max iterations {maxIterations}; max rows {maxRows}; max snapshot rows {maxSnapshotRows}",
            execution);
        Assert.Contains($"if (__cte0Iteration >= {maxIterations})", code);
        Assert.AreEqual(2, CountText(code, $">= {maxRows}"), fileName);
    }
}
