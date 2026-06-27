using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    [DataRow(LeftJoinSampleFileName)]
    [DataRow(RightJoinSampleFileName)]
    [DataRow(LeftJoinWithMultipleColumnsSampleFileName)]
    [DataRow(LeftJoinTwoSchemasSameKeySampleFileName)]
    public void SimpleOuterHashJoinSample_WhenCompiledForInspection_ShouldUseExecutionBackend(string fileName)
    {
        var result = CompileSampleForInspection(fileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("HashProbeNoMatch", result.ExecutionPlanText);
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void SimpleInnerHashJoinSample_WhenCompiledForInspection_ShouldPruneFinalSourceContexts()
    {
        var result = CompileSampleForInspection(InnerJoinSampleFileName);

        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a.Name, b.Country));", result.GeneratedCSharpCode);
        Assert.DoesNotContain("(object)a, (object)b", result.GeneratedCSharpCode);
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, "new object[] { a, b }"));
    }

    [TestMethod]
    public void SimpleInnerHashJoinSample_WhenCompiledForInspection_ShouldInlineFinalJoinProjection()
    {
        var result = CompileSampleForInspection(InnerJoinSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ForEach [ab in _tableResults[0].Rows]", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("new Statement0Row0", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_tableResults[0] = statement0", StringComparison.Ordinal));
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a.Name, b.Country));", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("result.AddDirect(new ResultRow0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SimpleInnerHashJoinSample_WhenCheckedIn_ShouldKeepSingleUseFusionShape()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == InnerJoinSampleFileName).Content;

        Assert.Contains("CtePhase [cte0]", sample);
        Assert.Contains("OnPhaseChanged(\"compiled:cte0\", QueryPhase.Begin);", sample);
        Assert.Contains("OnPhaseChanged(\"compiled:cte0\", QueryPhase.End);", sample);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a.Name, b.Country));", sample);
        Assert.IsFalse(sample.Contains("AppendHashJoinRows(aRows, bHash, result, token);", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("result.AddDirect(new ResultRow0(a.Name, b.Country));", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("StoreTable [statement0 -> _tableResults[0]]", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("private Table[] _tableResults", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("_tableResults[0]", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("Statement0Row0", StringComparison.Ordinal));
        Assert.IsFalse(sample.Contains("BuildCte0(", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SimpleInnerHashJoinSample_WhenCheckedIn_ShouldUseHashJoinBucketAndEnumerableCapacity()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == InnerJoinSampleFileName).Content;
        var computeMethod = GetComputeMethod(sample);

        Assert.Contains("CreateHash [bHash: int -> BasicEntity]", sample);
        Assert.Contains(
            "new Dictionary<int, HashJoinBucket<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity>>()",
            sample);
        Assert.Contains("foreach (var bChunk in bRows)", computeMethod);
        Assert.Contains("foreach (var aChunk in aRows)", computeMethod);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a.Name, b.Country));", computeMethod);
        Assert.IsFalse(sample.Contains("private static void AppendHashJoinRows(", StringComparison.Ordinal));
        Assert.Contains("token.ThrowIfCancellationRequested();", sample);
        Assert.IsFalse(sample.Contains("Dictionary<int, List<", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow(LeftJoinSampleFileName)]
    [DataRow(RightJoinSampleFileName)]
    [DataRow(LeftJoinWithMultipleColumnsSampleFileName)]
    [DataRow(LeftJoinTwoSchemasSameKeySampleFileName)]
    public void SimpleOuterHashJoinSample_WhenCompiledForExecution_ShouldRunExecutableQuery(string fileName)
    {
        var table = CompileSampleForExecution(fileName).Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void SimpleOuterHashJoinSamples_WhenCheckedIn_ShouldUseExecutionIrNullExtendedHashProbe()
    {
        var samples = ReadSamples()
            .Where(static sample => SimpleOuterHashJoinSampleFileNames.Contains(sample.FileName))
            .ToArray();

        Assert.HasCount(SimpleOuterHashJoinSampleFileNames.Length, samples);

        var failures = samples
            .SelectMany(static sample => GetSimpleOuterHashJoinShapeFailures(sample.FileName, sample.Content))
            .ToArray();

        Assert.IsEmpty(failures, $"Simple outer hash join samples have stale resolver shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void LeftJoinTwoSchemasSameKeySample_WhenCheckedIn_ShouldExtractHashJoinLoopsIntoHelpers()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == LeftJoinTwoSchemasSameKeySampleFileName).Content;
        var computeMethod = GetComputeMethod(sample);

        Assert.Contains("foreach (var bChunk in bRows)", computeMethod);
        Assert.Contains("foreach (var aChunk in aRows)", computeMethod);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(a.Id, b.Id));", computeMethod);
        Assert.IsFalse(sample.Contains("private static void AppendLeftJoinRows(", StringComparison.Ordinal));
        Assert.Contains("token.ThrowIfCancellationRequested();", sample);
        Assert.AreEqual(
            0,
            Regex.Matches(
                sample,
                Regex.Escape("[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]") + @"\s+private static ").Count);
    }

    [TestMethod]
    public void AsOfJoinSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(AsOfJoinSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("AsOfProbe [b <-", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, decimal>", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void AsOfJoinSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(AsOfJoinSampleFileName).Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void AsOfJoinSample_WhenCheckedIn_ShouldUseExecutionIrAsOfProbe()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == AsOfJoinSampleFileName);
        var failures = GetAsOfJoinShapeFailures(sample.Content);

        Assert.IsEmpty(failures, $"{AsOfJoinSampleFileName} has stale resolver shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void AsOfTieBreakSample_WhenCompiledForInspection_ShouldUseTypedTieBreakIndex()
    {
        var result = CompileSampleForInspection(AsOfTieBreakSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("AsOfProbe [b <-", result.ExecutionPlanText);
        Assert.Contains(
            "EvaluationHelper.CreateAsOfIndex<Musoq.Evaluator.Tests.Schema.Basic.BasicEntity, decimal, decimal>",
            result.GeneratedCSharpCode);
        Assert.Contains("Musoq.Evaluator.IR.Bindings.NullOrdering.Last", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void AsOfTieBreakSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(AsOfTieBreakSampleFileName).Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void AsOfTieBreakSample_WhenCheckedIn_ShouldUseTypedTieBreakIndex()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == AsOfTieBreakSampleFileName);
        var failures = GetAsOfTieBreakShapeFailures(sample.Content);

        Assert.IsEmpty(failures, $"{AsOfTieBreakSampleFileName} has stale tie-break shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void CteBackedAsOfJoinSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(CteBackedAsOfJoinSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]", result.ExecutionPlanText);
        Assert.Contains("AsOfProbe [r <- _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.CreateAsOfIndex<Cte0Row0, decimal>", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void CteBackedAsOfJoinSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(CteBackedAsOfJoinSampleFileName).Run();

        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void CteBackedAsOfJoinSample_WhenCheckedIn_ShouldUseExecutionIrTableBackedAsOfProbe()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == CteBackedAsOfJoinSampleFileName);
        var failures = GetCteBackedAsOfJoinShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{CteBackedAsOfJoinSampleFileName} has stale table-backed ASOF shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void DynamicCteBackedAsOfJoinSample_WhenCompiledForInspection_ShouldUseExecutionBackend()
    {
        var result = CompileSampleForInspection(DynamicCteBackedAsOfJoinSampleFileName);

        Assert.IsFalse(
            result.ExecutionPlanText.Contains("ExecutionPlanUnsupported", StringComparison.Ordinal),
            result.ExecutionPlanText);
        Assert.Contains("ExpandoAdapter [d: dDynamicRow0]", result.ExecutionPlanText);
        Assert.Contains("ExpandoAdapter [l: lDynamicRow0]", result.ExecutionPlanText);
        Assert.Contains("StoreTable [cte0 -> _cteRowResults.Slot0: List<Cte0Row0>]", result.ExecutionPlanText);
        Assert.Contains("AsOfProbe [r <- _cteRowResults.Slot0", result.ExecutionPlanText);
        Assert.Contains("EvaluationHelper.CreateAsOfIndex<Cte0Row0, int>", result.GeneratedCSharpCode);
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("_tableResults[0]", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("new Table(\"cte0\"", StringComparison.Ordinal));
        Assert.IsFalse(result.GeneratedCSharpCode.Contains("EvaluationHelper.FindAsOfMatch<", StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, GetColumnValuePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(result.GeneratedCSharpCode, ContextsAccessPattern));
    }

    [TestMethod]
    public void DynamicCteBackedAsOfJoinSample_WhenCompiledForExecution_ShouldRunExecutableQuery()
    {
        var table = CompileSampleForExecution(DynamicCteBackedAsOfJoinSampleFileName).Run();

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("ada", table[0][0]);
        Assert.AreEqual("ada", table[0][1]);
        Assert.AreEqual("bea", table[1][0]);
        Assert.AreEqual("bea", table[1][1]);
        Assert.AreEqual("cid", table[2][0]);
        Assert.AreEqual("cid", table[2][1]);
    }

    [TestMethod]
    public void DynamicCteBackedAsOfJoinSample_WhenCheckedIn_ShouldUseExecutionIrDynamicTypedAsOfProbe()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == DynamicCteBackedAsOfJoinSampleFileName);
        var failures = GetDynamicCteBackedAsOfJoinShapeFailures(sample.Content);

        Assert.IsEmpty(
            failures,
            $"{DynamicCteBackedAsOfJoinSampleFileName} has stale dynamic typed ASOF shape: {string.Join(", ", failures)}");
    }
}
