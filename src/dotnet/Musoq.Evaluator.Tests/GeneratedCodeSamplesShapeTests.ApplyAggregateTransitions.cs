using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void ChainedApplyQualifyWindowSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(ChainedApplyQualifyWindowSampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("CreateTable [apply_0_i_nTable: apply_0_i_nRow0]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ForEach [apply_0_i_n in apply_0_i_nTable.Rows]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("CreateRowBuffer [apply_0_i_n_mTable: List<apply_0_i_n_mRow0>]", result.ExecutionPlanText);
        Assert.Contains("Materialize [apply_0_i_n_mTable -> resultWindowRows]", result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("CreateTable [apply_0_i_n_mTable: apply_0_i_n_mRow0]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("Materialize [apply_0_i_n_mTable.Rows -> resultWindowRows]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("EnumerableSource [i.Numbers -> apply_0_i_n_mTable_nRows]", result.ExecutionPlanText);
        Assert.Contains("EnumerableSource [i.Numbers -> apply_0_i_n_mTable_mRows]", result.ExecutionPlanText);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        Assert.Contains("resultRowNumbers[windowIndex] <= 1", result.ExecutionPlanText);
    }

    [TestMethod]
    public void ChainedApplyQualifyWindowSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ChainedApplyQualifyWindowSampleFileName).Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual("right", table[1][0]);
        Assert.AreEqual(3, table[1][1]);
        Assert.AreEqual(3, table[1][2]);
    }

    [TestMethod]
    public void ChainedApplyQualifyWindowSample_WhenCheckedIn_ShouldUseExecutionIrQualifyWindow()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == ChainedApplyQualifyWindowSampleFileName);
        var failures = GetChainedApplyQualifyWindowShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{ChainedApplyQualifyWindowSampleFileName} has stale QUALIFY windowed chained-apply shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void ChainedApplyGroupedAggregateQualifyWindowSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(ChainedApplyGroupedAggregateQualifyWindowSampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        Assert.Contains("CreateSingleKeyAggregateContext [", result.ExecutionPlanText);
        AssertUsesTypedAggregateState(result, "SetAvg", "SetMin", "SetMax");
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
        Assert.Contains("resultRowNumbers[windowIndex] <= 1", result.ExecutionPlanText);
    }

    [TestMethod]
    public void ChainedApplyGroupedAggregateQualifyWindowSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ChainedApplyGroupedAggregateQualifyWindowSampleFileName).Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("right", table[0][0]);
        Assert.AreEqual(3, table[0][1]);
        Assert.AreEqual(3, table[0][2]);
        Assert.AreEqual(3, table[0][3]);
    }

    [TestMethod]
    public void ChainedApplyGroupedAggregateQualifyWindowSample_WhenCanceledDuringAggregateHelper_ShouldThrowOperationCanceledException()
    {
        var query = CompileSampleForExecution(ChainedApplyGroupedAggregateQualifyWindowSampleFileName);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => query.Run(cancellation.Token));
    }

    [TestMethod]
    public void ChainedApplyGroupedAggregateQualifyWindowSample_WhenCheckedIn_ShouldUseExecutionIrAggregateQualifyWindow()
    {
        var sample = ReadSamples()
            .Single(static sample => sample.FileName == ChainedApplyGroupedAggregateQualifyWindowSampleFileName);
        var failures = GetChainedApplyGroupedAggregateQualifyWindowShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{ChainedApplyGroupedAggregateQualifyWindowSampleFileName} has stale grouped aggregate QUALIFY windowed chained-apply shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void AggregateOverHashJoinSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(AggregateOverHashJoinSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("CreateHash [", result.ExecutionPlanText);
        Assert.Contains("string -> BasicEntity", result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ForEach [ab in _tableResults[0].Rows]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("HashProbe [", result.ExecutionPlanText);
        AssertUsesTypedAggregateState(result, "SetCount");
        Assert.Contains("Hash.TryGetValue", result.GeneratedCSharpCode);
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void AggregateOverHashJoinSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(AggregateOverHashJoinSampleFileName).Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void AggregateOverHashJoinSample_WhenCheckedIn_ShouldUseExecutionIrAggregateOverHashTransition()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == AggregateOverHashJoinSampleFileName);
        var failures = GetAggregateOverHashJoinShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{AggregateOverHashJoinSampleFileName} has stale aggregate-over-hash shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void CteBackedAggregateOverHashJoinSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(CteBackedAggregateOverHashJoinSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("ParallelBlock [cte-level-0, tasks 2, maxDegree 2]", result.ExecutionPlanText);
        Assert.Contains("StoreTable [__parallelCteLevel0Task0Result -> _cteRowResults.Slot0: List<Cte0Row0>]", result.ExecutionPlanText);
        Assert.Contains("StoreCteIndex [cte1HashSidecar0City -> _cteIndexResults.Slot0 Hash]", result.ExecutionPlanText);
        Assert.Contains("LoadCteIndex [rHash <- _cteIndexResults.Slot0 Hash: string]", result.ExecutionPlanText);
        Assert.Contains("HashPayload [Cte1HashPayload0]", result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("StoreTable [__parallelCteLevel0Task1Result -> _cteRowResults.Slot1", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[2]]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ForEach [lr in _tableResults[2].Rows]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("HashProbe [", result.ExecutionPlanText);
        AssertUsesTypedAggregateState(result, "SetCount");
        Assert.Contains("Hash.TryGetValue", result.GeneratedCSharpCode);
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void CteBackedAggregateOverHashJoinSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(CteBackedAggregateOverHashJoinSampleFileName).Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void CteBackedAggregateOverHashJoinSample_WhenCheckedIn_ShouldUseExecutionIrCteAggregateOverHashTransition()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == CteBackedAggregateOverHashJoinSampleFileName);
        var failures = GetCteBackedAggregateOverHashJoinShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{CteBackedAggregateOverHashJoinSampleFileName} has stale CTE aggregate-over-hash shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void AccessMethodApplySample_WhenCheckedIn_ShouldUseDirectTypedEnumerableRows()
    {
        var samples = ReadSamples()
            .Where(static sample => sample.FileName is AccessMethodApplySampleFileName or OuterAccessMethodApplySampleFileName)
            .ToArray();

        Assert.HasCount(2, samples);

        var failures = samples
            .SelectMany(static sample => GetAccessMethodApplyShapeFailures(sample.FileName, sample.Content))
            .ToArray();

        Assert.IsEmpty(failures, $"Access-method apply samples have stale enumerable shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void OuterAccessMethodApplySample_WhenCheckedIn_ShouldTrackUnmatchedRows()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == OuterAccessMethodApplySampleFileName);
        var failures = GetOuterAccessMethodApplyShapeFailures(sample.Content);

        Assert.IsEmpty(failures, $"{OuterAccessMethodApplySampleFileName} has stale outer-apply shape: {string.Join(", ", failures)}");
    }
}
