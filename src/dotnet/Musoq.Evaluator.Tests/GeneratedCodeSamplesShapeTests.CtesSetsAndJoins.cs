using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void MultipleCteChainedSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(MultipleCteChainedSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(SmartForEachPattern, StringComparison.Ordinal));
    }

    [TestMethod]
    public void CteDownstreamSample_WhenCheckedIn_ShouldFuseReadOnceProjectionCte()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == CteDownstreamSampleFileName);

        Assert.Contains("CtePhase [cte0]", sample.Content);
        Assert.Contains("ForEach [ko3iko in ko3ikoRows]", sample.Content);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: ko3iko.Name, Population: population)]", sample.Content);
        Assert.Contains("OnPhaseChanged(\"compiled:cte0\", QueryPhase.Begin);", sample.Content);
        Assert.IsFalse(sample.Content.Contains("private Table[] _tableResults", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("private sealed class Cte0Row0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("BuildCte0(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("_tableResults[0]", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(sample.Content, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ConvertTableToSourceWithDiscardedContextsPattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ContextsAccessPattern));
    }

    [TestMethod]
    public void MultipleCteChainedSample_WhenCheckedIn_ShouldFuseLinearReadOnceCteChain()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == MultipleCteChainedSampleFileName);

        Assert.Contains("private sealed class ResultRow0", sample.Content);
        Assert.Contains("CtePhase [cte0]", sample.Content);
        Assert.Contains("CtePhase [cte1]", sample.Content);
        Assert.Contains("ForEach [ko3iko in ko3ikoRows]", sample.Content);
        Assert.Contains("If [((ko3iko.Population > 0) AND city IS NOT NULL)]", sample.Content);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: ko3iko.Name, City: city)]", sample.Content);
        Assert.Contains("OnPhaseChanged(\"compiled:cte0\", QueryPhase.Begin);", sample.Content);
        Assert.Contains("OnPhaseChanged(\"compiled:cte1\", QueryPhase.Begin);", sample.Content);
        Assert.IsFalse(sample.Content.Contains("private Table[] _tableResults", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("private sealed class Cte0Row0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("private sealed class Cte1Row0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("BuildCte0(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("BuildCte1(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("StoreTable [cte0 -> _tableResults[0]]", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("StoreTable [cte1Table -> _tableResults[1]]", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("_tableResults[0]", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("_tableResults[1]", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(sample.Content, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ConvertTableToSourceWithDiscardedContextsPattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ContextsAccessPattern));
    }

    [TestMethod]
    public void CteDistinctJoinByCountrySample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(CteDistinctJoinByCountrySampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("CreateKeySet [cte0DistinctKeys: string]", result.ExecutionPlanText);
        Assert.Contains("If [Add(country)]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("DistinctTable", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(SmartForEachPattern, StringComparison.Ordinal));
    }

    [TestMethod]
    public void CteDistinctJoinByCountrySample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(CteDistinctJoinByCountrySampleFileName).Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void CteDistinctJoinByCountrySample_WhenCheckedIn_ShouldUseExecutionIrCteDistinctJoin()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == CteDistinctJoinByCountrySampleFileName);

        Assert.Contains("private sealed class Cte0Row0", sample.Content);
        Assert.IsFalse(sample.Content.Contains("private sealed class Cte0Statement0Row0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("private sealed class Cte0Statement1Row0", StringComparison.Ordinal));
        Assert.Contains("HashProbe [cte0BHash[a.Country] -> cte0BHashMatches]", sample.Content);
        Assert.Contains("CreateKeySet [cte0DistinctKeys: string]", sample.Content);
        Assert.Contains("var cte0DistinctKeys = new HashSet<string>();", sample.Content);
        Assert.Contains("If [Add(country)]", sample.Content);
        Assert.Contains("AppendRow [cte0 <- Cte0Row0(Country: country)]", sample.Content);
        Assert.IsFalse(sample.Content.Contains("AggregateGroup [Cte0AggregateGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("GetOrAddSingleKeyAggregateGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("cte0RootGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains(".GetValue<", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("EvaluationHelper.ToDistinctTable(", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(sample.Content, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ConvertTableToSourceWithDiscardedContextsPattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ContextsAccessPattern));
    }

    [TestMethod]
    public void InSubqueryBasicSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(InSubqueryBasicSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("PhysicalHashJoin [LeftSemi]", result.PhysicalPlanText);
        Assert.Contains("CtePhase [cte0]", result.ExecutionPlanText);
        Assert.Contains("CreateKeySet [_sq_1Keys: string]", result.ExecutionPlanText);
        Assert.Contains("ForEach [b in cte0_bRows]", result.ExecutionPlanText);
        Assert.Contains("AppendShape [result <- ResultShape0(a.City: a.City)]", result.ExecutionPlanText);
        Assert.IsFalse(result.ExecutionPlanText.Contains("StoreTable [cte0 -> _tableResults[0]]", StringComparison.Ordinal));
        Assert.IsFalse(result.ExecutionPlanText.Contains("AggregateGroup", StringComparison.Ordinal));
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[1]]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains(SmartForEachPattern, StringComparison.Ordinal));
    }

    [TestMethod]
    public void InSubqueryBasicSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(InSubqueryBasicSampleFileName).Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void InSubqueryBasicSample_WhenCheckedIn_ShouldUseExecutionIrSubqueryJoin()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == InSubqueryBasicSampleFileName);

        Assert.Contains("PhysicalHashJoin [LeftSemi]", sample.Content);
        Assert.Contains("private sealed class ResultRow0", sample.Content);
        Assert.Contains("var _sq_1Keys = new HashSet<string>();", sample.Content);
        Assert.Contains("foreach (var bChunk in cte0_bRows)", sample.Content);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a.City));", sample.Content);
        Assert.IsFalse(sample.Content.Contains("AppendHashJoinRows(aRows, _sq_1Keys, result, token);", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("private static void BuildSq1Keys(", StringComparison.Ordinal));
        Assert.Contains("token.ThrowIfCancellationRequested();", sample.Content);
        Assert.Contains("string key = b.City;", sample.Content);
        Assert.Contains("_sq_1Keys.Add(key);", sample.Content);
        Assert.IsFalse(sample.Content.Contains("result.AddDirect(new ResultRow0(a.City));", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("HashJoinBucket<Cte0Row0>", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("private sealed class Cte0Row0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("BuildCte0(", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("_tableResults[0]", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("EvaluationHelper.CastGeneratedRows<Cte0Row0>(_tableResults[0].Rows)", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("private sealed class Statement0Row0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("AggregateGroup [Cte0AggregateGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("cte0RootGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains(".GetValue<", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(sample.Content, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ConvertTableToSourceWithDiscardedContextsPattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ContextsAccessPattern));
    }

    [TestMethod]
    [DataRow(ExceptWithGroupBySidesSampleFileName)]
    [DataRow(Union3WithGroupBySidesSampleFileName)]
    [DataRow(UnionWithGroupBySidesSampleFileName)]
    public void GroupedSetOperationSample_WhenCompiledForInspection_ShouldUseExecutionBackend(string fileName)
    {
        var result = CompileSampleForInspection(fileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("SetOperation [", result.ExecutionPlanText);
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
    }

    [TestMethod]
    [DataRow(ExceptWithGroupBySidesSampleFileName)]
    [DataRow(Union3WithGroupBySidesSampleFileName)]
    [DataRow(UnionWithGroupBySidesSampleFileName)]
    public void GroupedSetOperationSample_WhenCompiledForExecution_ShouldRunExecutableQuery(string fileName)
    {
        var table = CompileSampleForExecution(fileName).Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void GroupedSetOperationSamples_WhenCheckedIn_ShouldUseExecutionIrAggregateArms()
    {
        var samples = ReadSamples()
            .Where(static sample => GroupedSetOperationSampleFileNames.Contains(sample.FileName))
            .ToArray();

        Assert.HasCount(GroupedSetOperationSampleFileNames.Length, samples);

        var failures = samples
            .SelectMany(static sample => GetGroupedSetOperationShapeFailures(sample.FileName, sample.Content))
            .ToArray();

        Assert.IsEmpty(failures, $"Grouped set-operation samples have stale resolver shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    [DataRow(ExceptWithGroupBySidesSampleFileName, 4)]
    [DataRow(Union3WithGroupBySidesSampleFileName, 4)]
    [DataRow(UnionWithGroupBySidesSampleFileName, 3)]
    public void GroupedSetOperationSamples_WhenCheckedIn_ShouldShareIdenticalAggregateHelpers(
        string fileName,
        int helperReferenceCount)
    {
        var sample = ReadSamples().Single(sample => sample.FileName == fileName).Content;

        Assert.Contains("private sealed class AggregateGroup0", sample);
        Assert.AreEqual(1, CountOccurrences(sample, "private sealed class AggregateGroup0"));
        Assert.AreEqual(1, CountOccurrences(sample, "private static List<AggregateGroup0> ParallelSingleKeyAggregate_0("));
        Assert.AreEqual(1, CountOccurrences(sample, "private static void ParallelSingleKeyAggregateShard_0("));
        Assert.AreEqual(1, CountOccurrences(sample, "private sealed class ParallelSingleKeyAggregateWorker_0"));
        Assert.AreEqual(1, CountOccurrences(sample, "private static void SerialSingleKeyAggregate_0("));
        Assert.AreEqual(helperReferenceCount, CountOccurrences(sample, "ParallelSingleKeyAggregate_0("));
        Assert.AreEqual(helperReferenceCount, CountOccurrences(sample, "SerialSingleKeyAggregate_0("));
        Assert.AreEqual(1, CountOccurrences(sample, "Parallel.For(0, workerCount, options, worker.Run);"));
        Assert.AreEqual(0, CountOccurrences(sample, "shardIndex =>"));
        Assert.AreEqual(0, CountOccurrences(sample, "ParallelSingleKeyAggregate_1"));
        Assert.AreEqual(0, CountOccurrences(sample, "ParallelSingleKeyAggregateShard_1"));
        Assert.AreEqual(0, CountOccurrences(sample, "ParallelSingleKeyAggregateWorker_1"));
        Assert.AreEqual(0, CountOccurrences(sample, "SerialSingleKeyAggregate_1"));
        Assert.AreEqual(0, CountOccurrences(sample, "private sealed class LeftLeftAggregateGroup"));
        Assert.AreEqual(0, CountOccurrences(sample, "private sealed class LeftRightAggregateGroup"));
        Assert.AreEqual(0, CountOccurrences(sample, "private sealed class RightAggregateGroup"));
        Assert.AreEqual(0, CountOccurrences(sample, "Func<"));
        Assert.AreEqual(0, CountOccurrences(sample, "Action<"));
        Assert.AreEqual(0, CountOccurrences(sample, ".GetValue<"));
        Assert.AreEqual(0, CountOccurrences(sample, ".SetValue<"));
        Assert.AreEqual(0, CountOccurrences(sample, "GroupSlot"));
    }

}
