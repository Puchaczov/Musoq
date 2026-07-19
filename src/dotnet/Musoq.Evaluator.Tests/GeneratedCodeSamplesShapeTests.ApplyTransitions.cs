using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void CrossApplySample_WhenCompiledForInspection_ShouldUseScopedRightSourceAndFuseProjection()
    {
        AssertTableApplyCompiledForInspection(CrossApplySampleFileName);
    }

    [TestMethod]
    public void OuterApplySample_WhenCompiledForInspection_ShouldUseScopedRightSourceAndFuseProjection()
    {
        AssertTableApplyCompiledForInspection(OuterApplySampleFileName);
    }

    [TestMethod]
    public void CrossApplySample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(CrossApplySampleFileName).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("ChildName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    [TestMethod]
    public void OuterApplySample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(OuterApplySampleFileName).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("OtherName", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table);
    }

    [TestMethod]
    public void CrossApplySample_WhenCheckedIn_ShouldUseScopedRightSourceAndFuseProjection()
    {
        AssertCheckedInTableApplySample(CrossApplySampleFileName);
    }

    [TestMethod]
    public void OuterApplySample_WhenCheckedIn_ShouldUseScopedRightSourceAndFuseProjection()
    {
        AssertCheckedInTableApplySample(OuterApplySampleFileName);
    }

    [TestMethod]
    public void ChainedApplyWindowSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(ChainedApplyWindowSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
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
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void ChainedApplyWindowSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ChainedApplyWindowSampleFileName).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("i.Name", typeof(string)),
            ("FirstValue", typeof(int)),
            ("SecondValue", typeof(int)),
            ("RowNo", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["left", 1, 1, 1L],
            ["left", 1, 2, 2L],
            ["left", 2, 1, 3L],
            ["left", 2, 2, 4L],
            ["right", 3, 3, 1L]);
    }

    [TestMethod]
    public void ChainedApplyWindowSample_WhenCheckedIn_ShouldUseExecutionIrWindowedChainedApply()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == ChainedApplyWindowSampleFileName);
        var failures = GetChainedApplyWindowShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{ChainedApplyWindowSampleFileName} has stale windowed chained-apply shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctAggregateSortSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(ChainedApplyMixedDistinctAggregateSortSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("CreateSingleKeyAggregateContext [", result.ExecutionPlanText);
        AssertUsesTypedAggregateState(result, "SetSum", "SetDistinctAggregate");
        Assert.Contains("inm.Sum(distinct n.Value)", result.ExecutionPlanText);
        Assert.Contains("SortShapeRows [", result.ExecutionPlanText);
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctAggregateSortSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ChainedApplyMixedDistinctAggregateSortSampleFileName).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RepeatedSum", typeof(int?)),
            ("DistinctSum", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["left", 6, 3],
            ["right", 3, 3]);
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctAggregateSortSample_WhenCheckedIn_ShouldUseExecutionIrDistinctAggregateSort()
    {
        var sample = ReadSamples()
            .Single(static sample => sample.FileName == ChainedApplyMixedDistinctAggregateSortSampleFileName);
        var failures = GetChainedApplyMixedDistinctAggregateSortShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{ChainedApplyMixedDistinctAggregateSortSampleFileName} has stale mixed distinct aggregate-sort shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctMinMaxAggregateSortSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(ChainedApplyMixedDistinctMinMaxAggregateSortSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("CreateSingleKeyAggregateContext [", result.ExecutionPlanText);
        AssertUsesTypedAggregateState(result, "SetMin", "SetMax", "SetDistinctAggregate");
        Assert.Contains("inm.Min(distinct n.Value)", result.ExecutionPlanText);
        Assert.Contains("inm.Max(distinct n.Value)", result.ExecutionPlanText);
        Assert.Contains("SortShapeRows [", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ComputeRowNumberWindow [", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctMinMaxAggregateSortSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ChainedApplyMixedDistinctMinMaxAggregateSortSampleFileName).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RepeatedMin", typeof(int?)),
            ("DistinctMin", typeof(int?)),
            ("RepeatedMax", typeof(int?)),
            ("DistinctMax", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["left", 1, 1, 4, 4],
            ["right", 3, 3, 3, 3]);
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctMinMaxAggregateSortSample_WhenCheckedIn_ShouldUseExecutionIrDistinctAggregateSort()
    {
        var sample = ReadSamples()
            .Single(static sample => sample.FileName == ChainedApplyMixedDistinctMinMaxAggregateSortSampleFileName);
        var failures = GetChainedApplyMixedDistinctMinMaxAggregateSortShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{ChainedApplyMixedDistinctMinMaxAggregateSortSampleFileName} has stale mixed distinct Min/Max aggregate-sort shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctAvgAggregateSortSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(ChainedApplyMixedDistinctAvgAggregateSortSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("CreateSingleKeyAggregateContext [", result.ExecutionPlanText);
        AssertUsesTypedAggregateState(result, "SetAvg", "SetDistinctAggregate");
        Assert.Contains("inm.Avg(distinct n.Value)", result.ExecutionPlanText);
        Assert.Contains("SortShapeRows [", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("ComputeRowNumberWindow [", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctAvgAggregateSortSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ChainedApplyMixedDistinctAvgAggregateSortSampleFileName).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RepeatedAvg", typeof(int?)),
            ("DistinctAvg", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["right", 3, 3],
            ["left", 2, 2]);
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctAvgAggregateSortSample_WhenCheckedIn_ShouldUseExecutionIrDistinctAggregateSort()
    {
        var sample = ReadSamples()
            .Single(static sample => sample.FileName == ChainedApplyMixedDistinctAvgAggregateSortSampleFileName);
        var failures = GetChainedApplyMixedDistinctAvgAggregateSortShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{ChainedApplyMixedDistinctAvgAggregateSortSampleFileName} has stale mixed distinct Avg aggregate-sort shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctMinMaxAggregateWindowSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        Assert.Contains("CreateSingleKeyAggregateContext [", result.ExecutionPlanText);
        AssertUsesTypedAggregateState(result, "SetMin", "SetMax", "SetDistinctAggregate");
        Assert.Contains("inm.Min(distinct n.Value)", result.ExecutionPlanText);
        Assert.Contains("inm.Max(distinct n.Value)", result.ExecutionPlanText);
        Assert.Contains("MinDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("MaxDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctMinMaxAggregateWindowSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RepeatedMin", typeof(int?)),
            ("DistinctMin", typeof(int?)),
            ("RepeatedMax", typeof(int?)),
            ("DistinctMax", typeof(int?)),
            ("MixedMinMaxRowNo", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["left", 1, 1, 4, 4, 1L],
            ["right", 3, 3, 3, 3, 2L]);
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctMinMaxAggregateWindowSample_WhenCheckedIn_ShouldUseExecutionIrDistinctAggregateWindow()
    {
        var sample = ReadSamples()
            .Single(static sample => sample.FileName == ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName);
        var failures = GetChainedApplyMixedDistinctMinMaxAggregateWindowShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName} has stale mixed distinct Min/Max aggregate-window shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctAvgAggregateWindowSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName);

        AssertUsesExecutionBackendWithoutRetiredHelperPatterns(result);
        Assert.Contains("CreateSingleKeyAggregateContext [", result.ExecutionPlanText);
        AssertUsesTypedAggregateState(result, "SetAvg", "SetDistinctAggregate");
        Assert.Contains("inm.Avg(distinct n.Value)", result.ExecutionPlanText);
        Assert.Contains("AvgDistinctAggregateKernel<int>.Set", result.GeneratedCSharpCode);
        Assert.Contains("ComputeRowNumberWindow [", result.ExecutionPlanText);
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctAvgAggregateWindowSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName).Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("RepeatedAvg", typeof(int?)),
            ("DistinctAvg", typeof(int?)),
            ("MixedAvgRowNo", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["right", 3, 3, 1L],
            ["left", 2, 2, 2L]);
    }

    [TestMethod]
    public void ChainedApplyMixedDistinctAvgAggregateWindowSample_WhenCheckedIn_ShouldUseExecutionIrDistinctAggregateWindow()
    {
        var sample = ReadSamples()
            .Single(static sample => sample.FileName == ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName);
        var failures = GetChainedApplyMixedDistinctAvgAggregateWindowShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName} has stale mixed distinct Avg aggregate-window shape: {string.Join(", ", failures)}");
    }

}
