using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void AggregateWindowSamples_WhenCheckedIn_ShouldUseTypedWindowSourceBuffers()
    {
        var samples = ReadNamedSamples(
                ChainedApplyGroupedAggregateWindowSampleFileName,
                ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName,
                ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName)
            .ToDictionary(static sample => sample.FileName, StringComparer.Ordinal);

        AssertTypedWindowSourceBufferSample(samples, ChainedApplyGroupedAggregateWindowSampleFileName);
        AssertTypedWindowSourceBufferSample(samples, ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName);
        AssertTypedWindowSourceBufferSample(samples, ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName);
    }

    [TestMethod]
    public void ChainedApplyWindowSamples_WhenCheckedIn_ShouldUseTypedApplyWindowRows()
    {
        var samples = ReadNamedSamples(ChainedApplyWindowSampleFileName, ChainedApplyQualifyWindowSampleFileName)
            .ToDictionary(static sample => sample.FileName, StringComparer.Ordinal);

        AssertTypedApplyWindowRowsSample(samples[ChainedApplyWindowSampleFileName]);
        AssertTypedApplyWindowRowsSample(samples[ChainedApplyQualifyWindowSampleFileName]);
    }

    [TestMethod]
    public void GroupedWindowSamples_WhenCheckedIn_ShouldUseReadableHelperCalls()
    {
        var samples = ReadNamedSamples(
                ChainedApplyGroupedAggregateWindowSampleFileName,
                ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName,
                ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName,
                ChainedApplyGroupedAggregateQualifyWindowSampleFileName)
            .ToDictionary(static sample => sample.FileName, StringComparer.Ordinal);

        AssertGroupedWindowHelperShape(samples[ChainedApplyGroupedAggregateWindowSampleFileName], expectAggregateHelpers: true);
        AssertGroupedWindowHelperShape(samples[ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName], expectAggregateHelpers: true);
        AssertGroupedWindowHelperShape(samples[ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName], expectAggregateHelpers: true);
        AssertGroupedWindowHelperShape(samples[ChainedApplyGroupedAggregateQualifyWindowSampleFileName], expectAggregateHelpers: false);
    }

    [TestMethod]
    public void CteStoredRowSamples_WhenCheckedIn_ShouldUseTypedRowsWhenSafe()
    {
        var samples = ReadNamedSamples(
                CteWithJoinSampleFileName,
                CteBackedAsOfJoinSampleFileName,
                CteBackedAggregateOverHashJoinSampleFileName,
                RepeatedCteSelfJoinSampleFileName,
                DynamicCteBackedAsOfJoinSampleFileName)
            .ToDictionary(static sample => sample.FileName, StringComparer.Ordinal);

        AssertTypedCteSidecarHashJoinSample(samples[CteWithJoinSampleFileName], "Cte0HashPayload0");
        AssertTypedCteAsOfSample(samples[CteBackedAsOfJoinSampleFileName]);
        AssertTypedCteAggregateHashJoinSample(samples[CteBackedAggregateOverHashJoinSampleFileName]);
        AssertTypedRepeatedCteSelfJoinSample(samples[RepeatedCteSelfJoinSampleFileName]);
        AssertDynamicCteAsOfTypedSample(samples[DynamicCteBackedAsOfJoinSampleFileName]);
    }

    [TestMethod]
    public void SampleCorpus_WhenCheckedIn_ShouldUsePascalCaseAggregateGroupTypes()
    {
        var failures = ReadAllSamples()
            .Select(sample => new
            {
                sample.FileName,
                Matches = LowercaseAggregateGroupTypePattern
                    .Matches(sample.Content)
                    .Cast<Match>()
                    .Select(static match => match.Value)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray()
            })
            .Where(static sample => sample.Matches.Length > 0)
            .Select(static sample => $"{sample.FileName}: {string.Join(", ", sample.Matches)}")
            .ToArray();

        Assert.IsEmpty(
            failures,
            $"Generated aggregate group types should be PascalCase: {string.Join("; ", failures)}");
    }

    [TestMethod]
    public void MultipleWindowSample_WhenCheckedIn_ShouldReuseSharedPartitionKeys()
    {
        var sample = ReadSample(MultipleWindowsSampleFileName).Content;

        Assert.AreEqual(1, CountOccurrences(sample, "PartitionKeys = new string[resultWindowRows.Count];"));
        Assert.Contains(
            "var resultRowNumbers0Partitions = WindowFunctionHelpers.ResolvePartitionSet(resultWindowRows.Count, resultRowNumbers0PartitionKeys);",
            sample);
        Assert.Contains(
            "var resultRowNumbers0SortedPartitions = WindowFunctionHelpers.SortStructPartitionSet(resultRowNumbers0Partitions, resultRowNumbers0OrderKeys, false);",
            sample);
        Assert.Contains(
            "for (int resultSums1PartitionSetIndex = 0; resultSums1PartitionSetIndex < resultRowNumbers0Partitions.PartitionCount; ++resultSums1PartitionSetIndex)",
            sample);
        Assert.Contains(
            "var resultSums1PartitionIndices = resultRowNumbers0Partitions.Indices;",
            sample);
        Assert.Contains(
            "var resultSums1CurrentIndex = resultSums1PartitionIndices[resultSums1PartitionStart + resultSums1PartitionIndex];",
            sample);
        Assert.Contains(
            "ComputeSumWindowKernel[WholePartition]",
            sample);
        Assert.Contains(
            "resultSums1Sum += (decimal)ko3iko.Population;",
            sample);
        Assert.IsFalse(sample.Contains("resultSums1Function", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultSums1Values", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputeTypedPluginWindowFunction", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultSums1PartitionKeys", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("resultSums1Partitions", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WindowRankDenseRankSample_WhenCheckedIn_ShouldUseGeneratedRankingKernels()
    {
        var sample = ReadSample(WindowRankDenseRankSampleFileName).Content;

        Assert.Contains("var resultRanks0OrderKeys = new WindowResultRanks0OrderKeysKey[resultWindowRows.Count];", sample);
        Assert.Contains("WindowFunctionHelpers.SortStructPartitionSetInPlace(resultRanks0Partitions, resultRanks0OrderKeys, false);", sample);
        Assert.Contains("WindowKernelPlan [hash partition/per-partition sort; kernels 2;", sample);
        Assert.Contains("long resultRanks0WindowPlanRank = 1L;", sample);
        Assert.Contains("long resultRanks0WindowPlanDenseRank = 1L;", sample);
        Assert.Contains("resultRanks0[resultRanks0WindowPlanCurrentIndex] = resultRanks0WindowPlanRank;", sample);
        Assert.Contains("resultDenseRanks1[resultRanks0WindowPlanCurrentIndex] = resultRanks0WindowPlanDenseRank;", sample);
        Assert.Contains("public bool PeerEquals(WindowResultRanks0OrderKeysKey other)", sample);
        Assert.Contains("resultRanks0OrderKeys[resultRanks0WindowPlanCurrentIndex].PeerEquals(resultRanks0OrderKeys[resultRanks0WindowPlanPreviousIndex])", sample);
        Assert.IsFalse(sample.Contains("System.Collections.Generic.EqualityComparer<WindowResultRanks0OrderKeysKey>.Default.Equals", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("for (int resultDenseRanks1PartitionSetIndex", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputeRank", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("WindowFunctionHelpers.ComputeDenseRank", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RankingWindowSamples_WhenCheckedIn_ShouldNotUseBoxedWindowFallbacks()
    {
        var failures = ReadAllSamples()
            .Where(static sample =>
                sample.Content.Contains("ComputeRowNumberWindow [", StringComparison.Ordinal) ||
                sample.Content.Contains("ComputeRankWindow [", StringComparison.Ordinal) ||
                sample.Content.Contains("ComputeDenseRankWindow [", StringComparison.Ordinal))
            .SelectMany(static sample => CreateRankingWindowFallbackFailures(sample))
            .ToArray();

        Assert.IsEmpty(
            failures,
            $"Ranking windows should stay on generated no-boxing kernels: {string.Join("; ", failures)}");
    }

    private static IEnumerable<string> CreateRankingWindowFallbackFailures(GeneratedCodeSampleFile sample)
    {
        foreach (var forbiddenPattern in new[]
                 {
                     "WindowFunctionHelpers.CompositeKey",
                     "WindowFunctionHelpers.ComputeRowNumber",
                     "WindowFunctionHelpers.ComputeRank",
                     "WindowFunctionHelpers.ComputeDenseRank",
                     "BoxedTypedPartitionSetComparer",
                     "new bool[]"
                 })
        {
            if (sample.Content.Contains(forbiddenPattern, StringComparison.Ordinal))
                yield return $"{sample.FileName}: {forbiddenPattern}";
        }

        foreach (var line in Regex.Split(sample.Content, "\r?\n"))
        {
            if (!IsWindowKeyLine(line))
                continue;

            if (line.Contains("object[]", StringComparison.Ordinal))
                yield return $"{sample.FileName}: object[] window key line: {line.Trim()}";

            if (line.Contains("(object)", StringComparison.Ordinal))
                yield return $"{sample.FileName}: boxed window key line: {line.Trim()}";
        }
    }

    private static bool IsWindowKeyLine(string line)
    {
        return line.Contains("OrderKeys", StringComparison.Ordinal) ||
               line.Contains("PartitionKeys", StringComparison.Ordinal);
    }

    [TestMethod]
    public void OffsetWindowSamples_WhenCheckedIn_ShouldUseGeneratedNoBoxingKernels()
    {
        var samples = ReadNamedSamples(WindowLagSampleFileName, WindowLeadSampleFileName)
            .ToDictionary(static sample => sample.FileName, static sample => sample.Content);
        var lag = samples[WindowLagSampleFileName];
        var lead = samples[WindowLeadSampleFileName];

        Assert.Contains("var resultLagsOrderKeys = new WindowResultLagsOrderKeysKey[resultWindowRows.Count];", lag);
        Assert.Contains("WindowFunctionHelpers.SortStructPartitionSetInPlace(resultLagsPartitions, resultLagsOrderKeys, false);", lag);
        Assert.Contains("var resultLags = new decimal? [resultWindowRows.Count];", lag);
        Assert.Contains("resultLagsSourcePartitionIndex >= 0", lag);
        Assert.IsFalse(lag.Contains("WindowFunctionHelpers.ComputeLag", StringComparison.Ordinal));
        Assert.IsFalse(lag.Contains("new bool[]", StringComparison.Ordinal));
        Assert.IsFalse(lag.Contains("object[] resultLags", StringComparison.Ordinal));

        Assert.Contains("var resultLeadsOrderKeys = new WindowResultLeadsOrderKeysKey[resultWindowRows.Count];", lead);
        Assert.Contains("WindowFunctionHelpers.SortStructPartitionSetInPlace(resultLeadsPartitions, resultLeadsOrderKeys, false);", lead);
        Assert.Contains("var resultLeads = new decimal? [resultWindowRows.Count];", lead);
        Assert.Contains("resultLeadsSourcePartitionIndex < resultLeadsPartitionCount", lead);
        Assert.IsFalse(lead.Contains("WindowFunctionHelpers.ComputeLead", StringComparison.Ordinal));
        Assert.IsFalse(lead.Contains("new bool[]", StringComparison.Ordinal));
        Assert.IsFalse(lead.Contains("object[] resultLeads", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DecimalWindowAggregateSamples_WhenCheckedIn_ShouldUseKernelAndDirectNumericConversion()
    {
        var samples = ReadNamedSamples(
                WindowSumWholePartitionDecimalSampleFileName,
                WindowSumRunningDecimalSampleFileName,
                WindowAvgRunningDecimalSampleFileName,
                WindowRunningProductPluginSampleFileName)
            .ToDictionary(static sample => sample.FileName, static sample => sample.Content);

        AssertWindowAggregateKernelUsesDirectNumericConversion(
            WindowSumWholePartitionDecimalSampleFileName,
            samples[WindowSumWholePartitionDecimalSampleFileName],
            "resultSums",
            "ComputeSumWindowKernel[WholePartition]");
        AssertWindowAggregateKernelUsesDirectNumericConversion(
            WindowSumRunningDecimalSampleFileName,
            samples[WindowSumRunningDecimalSampleFileName],
            "resultSums",
            "ComputeSumWindowKernel[BoundedRows]");
        AssertWindowAggregateKernelUsesDirectNumericConversion(
            WindowAvgRunningDecimalSampleFileName,
            samples[WindowAvgRunningDecimalSampleFileName],
            "resultAvgs",
            "ComputeAvgWindowKernel[BoundedRows]");
        AssertStreamingWindowUsesDirectNumericConversion(
            WindowRunningProductPluginSampleFileName,
            samples[WindowRunningProductPluginSampleFileName],
            "resultRunningProducts");

        Assert.Contains(
            "new Musoq.Evaluator.Tests.Schema.Basic.Library().WindowRunningProduct()",
            samples[WindowRunningProductPluginSampleFileName]);
    }

    [TestMethod]
    public void ParallelIndependentCtesSample_WhenCheckedIn_ShouldUseExplicitParallelExecutionIr()
    {
        var sample = ReadSample(ParallelIndependentCtesSampleFileName).Content;

        Assert.Contains("ParallelBlock [cte-level-0, tasks 2, maxDegree 2]", sample);
        Assert.Contains("ParallelTask [p -> __parallelCteLevel0Task0Result]", sample);
        Assert.Contains("ParallelTask [q -> __parallelCteLevel0Task1Result]", sample);
        Assert.Contains("ParallelMerge", sample);
        Assert.Contains("Parallel.Invoke(new ParallelOptions", sample);
        Assert.Contains("private static List<Cte0Row0> BuildCteLevel0Task0", sample);
        Assert.Contains("private static object BuildCteLevel0Task1", sample);
        Assert.Contains("private sealed class CteLevel0Runner", sample);
        Assert.Contains("cteLevel0Runner.RunCteLevel0Task0", sample);
        Assert.Contains("cteLevel0Runner.RunCteLevel0Task1", sample);
        Assert.Contains("CancellationToken = token", sample);
        Assert.Contains("MaxDegreeOfParallelism = 2", sample);
        Assert.Contains("List<Cte0Row0> cte0 = null!;", sample);
        Assert.Contains("var cte1HashSidecar0Name = new Dictionary<string, HashJoinBucket<Cte1HashPayload0>>();", sample);
        Assert.Contains("_cteRowResults.Slot0 = __parallelCteLevel0Task0Result", sample);
        Assert.Contains("_cteIndexResults.Slot0 = cte1HashSidecar0Name", sample);
        Assert.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", sample);
        Assert.Contains("var qHash = _cteIndexResults.Slot0;", sample);
        Assert.Contains("HashJoinBucket<Cte1HashPayload0>", sample);
        Assert.Contains("LoadCteIndex [qHash <- _cteIndexResults.Slot0 Hash: string]", sample);
        Assert.IsFalse(sample.Contains("var __storedTable1Rows = _cteRowResults.Slot1;", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Dictionary<string, HashJoinBucket<Cte1Row0>>(_cteRowResults.Slot1.Count)", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("private static Musoq.Evaluator.Tables.Table BuildCteLevel0Task0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("private static Musoq.Evaluator.Tables.Table BuildCteLevel0Task1", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Table(\"cte0\"", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("new Table(\"cte1\"", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("_tableResults[0]", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("_tableResults[1]", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("CastGeneratedRows<Cte0Row0>(_tableResults[0].Rows)", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("CastGeneratedRows<Cte1Row0>(_tableResults[1].Rows)", StringComparison.Ordinal));
        Assert.Contains("Parallel.Invoke(new ParallelOptions", sample);
    }

    [TestMethod]
    public void CompositeHashJoinSample_WhenCheckedIn_ShouldUseValueTupleHashJoinBucket()
    {
        var sample = ReadSample(CompositeHashJoinSampleFileName).Content;

        Assert.Contains("CreateHash [bHash: ValueTuple<int, decimal> -> BasicEntity]", sample);
        Assert.Contains("new Dictionary<ValueTuple<int, decimal>, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>", sample);
        Assert.Contains("var key = (b.Id, b.Population);", sample);
        Assert.Contains("bHash.TryGetValue(key, out var bHashMatches)", sample);
        Assert.IsFalse(sample.Contains("Dictionary<ValueTuple<int, decimal>, List<", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("CreateNullableHashJoinKey", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RepeatedCteSelfJoinSample_WhenCheckedIn_ShouldCacheStoredRowsLocal()
    {
        var sample = ReadSample(RepeatedCteSelfJoinSampleFileName).Content;

        Assert.Contains("var __storedTable0Rows = _cteRowResults.Slot0;", sample);
        Assert.Contains("ForEach [l in _cteRowResults.Slot0]", sample);
        Assert.Contains("ForEach [r in rHashMatches]", sample);
        Assert.Contains("Cte0Row0 l = __storedTable0Rows[__storedTable0Index];", sample);
        Assert.Contains("LoadCteIndex [rHash <- _cteIndexResults.Slot0 Hash: string]", sample);
        Assert.Contains("private static List<Cte0Row0> BuildCte0", sample);
        Assert.Contains("HashJoinBucket<Cte0HashPayload0>", sample);
        Assert.IsFalse(sample.Contains("var __storedTable0Rows = _tableResults[0].Rows;", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.CastGeneratedRows<Cte0Row0>(_tableResults[0].Rows)", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("foreach (var l in _tableResults[0].Rows)", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("foreach (var r in _tableResults[0].Rows)", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("Musoq.Evaluator.Tables.Table BuildCte0()\r\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OrderByTakeSample_WhenCheckedIn_ShouldUseTopNTable()
    {
        var sample = ReadSample(OrderByTakeSampleFileName).Content;

        Assert.Contains("CreateBoundedRecordList [resultOrderRecords: ResultRow0WithSortKeys by Population DESC, take 5]", sample);
        Assert.Contains(
            "var resultOrderRecords = new EvaluationHelper.BoundedTopRecordList<ResultRow0WithSortKeys>(5, ResultRow0WithSortKeysComparer.Instance);",
            sample);
        Assert.Contains(
            "resultOrderRecords.Add(new ResultRow0WithSortKeys(ko3iko.Name, ko3iko.Population, resultOrderRecords.Count));",
            sample);
        Assert.Contains("private readonly struct ResultRow0WithSortKeys", sample);
        Assert.IsFalse(sample.Contains("OrderRecordList [resultOrderRecords", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("EvaluationHelper.SelectTopRecords(resultOrderRecords", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TakeTable [result ->", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("TopNTable [", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("result.Rows.OrderBy", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("RowOrderKey", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("var resultSorted =", StringComparison.Ordinal));
    }

}
