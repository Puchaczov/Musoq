using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void SampleCorpus_WhenCheckedIn_ShouldStayWithinRuntimeV2ShapeBudget()
    {
        AssertShapeBudget("generated-code sample corpus", CorpusBudget, CountShapes(ReadSamples()));
    }

    [TestMethod]
    public void SourceScanSamples_WhenCheckedIn_ShouldUseTypedRowSourceBridge()
    {
        var samplesByFileName = ReadSamples()
            .ToDictionary(static sample => sample.FileName, static sample => sample.Content);
        var failures = SourceScanShapeExpectations
            .SelectMany(expectation => GetSourceScanShapeFailures(samplesByFileName, expectation))
            .ToArray();

        Assert.IsEmpty(failures, $"Source scan samples have stale resolver shape: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void Contexts_WhenCheckedIn_ShouldStayWithinRuntimeV2ShapeBudget()
    {
        var failures = ReadSamples()
            .Where(static sample => sample.Content.Contains(ContextsAccessPattern, StringComparison.Ordinal))
            .SelectMany(static sample => GetRetiredHelperShapeFailures(sample.FileName, sample.Content))
            .ToArray();

        Assert.IsEmpty(failures, $"Context-preserving samples still use retired helper shapes: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void ChainedApplyWindowSamples_WhenCheckedIn_ShouldStayWithinGeneratedCodeSizeBudgets()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName, static sample => sample.Content);
        var budgets = new[]
        {
            (ChainedApplyGroupedAggregateWindowSampleFileName, MaxLines: 1100),
            (ChainedApplyWindowSampleFileName, MaxLines: 850),
            (ChainedApplyMixedDistinctAggregateSortSampleFileName, MaxLines: 2000),
            (ChainedApplyMixedDistinctMinMaxAggregateSortSampleFileName, MaxLines: 2700),
            (ChainedApplyMixedDistinctAvgAggregateSortSampleFileName, MaxLines: 2100),
            (ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName, MaxLines: 850),
            (ChainedApplyMixedDistinctAvgAggregateWindowSampleFileName, MaxLines: 800),
            (ChainedApplyQualifyWindowSampleFileName, MaxLines: 850),
            (ChainedApplyGroupedAggregateQualifyWindowSampleFileName, MaxLines: 950)
        };

        var failures = budgets
            .Select(budget =>
            {
                var lineCount = CountLines(samples[budget.Item1]);
                return lineCount <= budget.MaxLines
                    ? null
                    : $"{budget.Item1}: {lineCount} lines exceeds budget {budget.MaxLines}";
            })
            .Where(static failure => failure != null)
            .ToArray();

        Assert.IsEmpty(failures, $"Generated chained-apply/window samples grew unexpectedly: {string.Join(", ", failures)}");
    }

    [TestMethod]
    public void ChainedApplyAggregateSamples_WhenCheckedIn_ShouldExtractTraversalAndUpdateHelpers()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName, static sample => sample.Content);

        AssertChainedApplyTraversalHelperShape(
            samples[ChainedApplyGroupedAggregateWindowSampleFileName],
            "TraverseWindowSourceTableNRows",
            "TraverseWindowSourceTableMRows",
            "UpdateGroupsAggregates");
        AssertChainedApplyTraversalHelperShape(
            samples[ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName],
            "TraverseWindowSourceTableNRows",
            "TraverseWindowSourceTableMRows",
            "UpdateGroupsAggregates");
        AssertChainedApplyTraversalHelperShape(
            samples[ChainedApplyGroupedAggregateQualifyWindowSampleFileName],
            "TraverseStatement0NRows",
            "TraverseStatement0MRows",
            null);
    }

    [TestMethod]
    public void AggregateHelperSamples_WhenCheckedIn_ShouldPassCancellationTokenThroughHelperLoops()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName, static sample => sample.Content);
        var q61 = samples[ChainedApplyGroupedAggregateWindowSampleFileName];
        var q72 = samples[ChainedApplyMixedDistinctMinMaxAggregateWindowSampleFileName];
        var q140 = samples[CorrelatedScalarAggregateSubquerySampleFileName];

        AssertAggregateCancellationShape(q61, "PopulateWindowSourceTableSingleKeyGroups", "FinalizeWindowSourceTableSingleKeyGroups");
        AssertAggregateCancellationShape(q72, "PopulateWindowSourceTableSingleKeyGroups", "FinalizeWindowSourceTableSingleKeyGroups");

        Assert.Contains("SerialSingleKeyAggregate_0(cte0_bRows, cte0Groups, cte0GroupsToFinalize, ref cte0NullGroup, token);", q140);
        Assert.Contains("private static void SerialSingleKeyAggregate_0(", q140);
        Assert.Contains("ref Cte0AggregateGroup nullGroup, CancellationToken token)", q140);
        Assert.Contains("token.ThrowIfCancellationRequested();", q140);
    }

    [TestMethod]
    public void DistinctSample_WhenCheckedIn_ShouldUseLeanDistinctSet()
    {
        var sample = ReadSamples().Single(static sample => sample.FileName == "Q08_Distinct.cs");

        Assert.Contains("CreateKeySet [distinctKeys: ValueTuple<string, string>]", sample.Content);
        Assert.Contains("If [Add((city, country))]", sample.Content);
        Assert.Contains("AppendShape [result <- ResultShape0(City: city, Country: country)]", sample.Content);
        Assert.Contains("var distinctKeys = new HashSet<ValueTuple<string, string>>();", sample.Content);
        Assert.Contains("if ((bool)distinctKeys.Add((city, country)))", sample.Content);
        Assert.IsFalse(sample.Content.Contains("AggregateGroup [ResultAggregateGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("GetOrAddValueTupleAggregateGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("private sealed class ResultAggregateGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains("rootGroup", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains(".GetValue<", StringComparison.Ordinal));
        Assert.IsFalse(sample.Content.Contains(ToDistinctTablePattern, StringComparison.Ordinal));
        Assert.AreEqual(0, CountOccurrences(sample.Content, SmartForEachPattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ConvertTableToSourcePattern));
        Assert.AreEqual(0, CountOccurrences(sample.Content, ContextsAccessPattern));
    }

    private static int CountLines(string content)
    {
        return content.Length == 0
            ? 0
            : content.Count(static character => character == '\n') + 1;
    }

    private static void AssertChainedApplyTraversalHelperShape(
        string sample,
        string outerHelper,
        string innerHelper,
        string? aggregateUpdateHelper)
    {
        Assert.Contains($"private static void {outerHelper}(CancellationToken token", sample);
        Assert.Contains($"private static void {innerHelper}(CancellationToken token", sample);
        Assert.Contains($"{outerHelper}(token,", sample);
        Assert.Contains($"{innerHelper}(token,", sample);
        Assert.Contains("RowChunk<", sample);
        Assert.Contains("token.ThrowIfCancellationRequested();", sample);

        if (aggregateUpdateHelper != null)
        {
            Assert.Contains($"private static void {aggregateUpdateHelper}(", sample);
            Assert.Contains($"{aggregateUpdateHelper}(", sample);
        }
    }

    private static void AssertAggregateCancellationShape(
        string sample,
        string populateHelper,
        string finalizeHelper)
    {
        Assert.Contains($"{populateHelper}(", sample);
        Assert.Contains($"{finalizeHelper}(", sample);
        Assert.Contains($", token);", sample);
        Assert.Contains($"private static void {populateHelper}(", sample);
        Assert.Contains("CancellationToken token)", sample);
        Assert.Contains($"private static void {finalizeHelper}(", sample);
        Assert.Contains("token.ThrowIfCancellationRequested();", sample);
    }

    [TestMethod]
    public void SetOperationSamples_WhenCheckedIn_ShouldUseStreamingUnionAllAndHashSetKeys()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName, static sample => sample.Content);
        var union = samples[UnionSampleFileName];
        var except = samples[ExceptSampleFileName];
        var unionAll = samples[UnionAllSampleFileName];
        var intersect = samples[IntersectSampleFileName];

        Assert.Contains("SetOperation [result = left Union right, HashSet]", union);
        Assert.Contains("CreateRowBuffer [left: List<LeftRow0>]", union);
        Assert.Contains("CreateRowBuffer [right: List<RightRow0>]", union);
        Assert.Contains("var resultKeys = new HashSet<string>(left.Count + right.Count);", union);
        Assert.Contains("resultKeys.Add((string)resultLeftRow.Name);", union);
        Assert.Contains("if (resultKeys.Add((string)resultRightRow.Name))", union);
        Assert.Contains("__musoqFinalShapeRows.Add(new LeftShape0((string)resultLeftRow.Name));", union);
        Assert.Contains("__musoqFinalShapeRows.Add(new LeftShape0((string)resultRightRow.Name));", union);
        Assert.IsFalse(union.Contains("new Table(\"left\"", StringComparison.Ordinal));
        Assert.IsFalse(union.Contains("new Table(\"right\"", StringComparison.Ordinal));
        Assert.IsFalse(union.Contains(".AddUnchecked(", StringComparison.Ordinal));
        Assert.IsFalse(union.Contains("Union(left, right", StringComparison.Ordinal));

        Assert.Contains("SetOperation [result = left Except right, HashSet]", except);
        Assert.Contains("CreateRowBuffer [left: List<LeftRow0>]", except);
        Assert.Contains("CreateRowBuffer [right: List<RightRow0>]", except);
        Assert.Contains("var resultRightKeys = new HashSet<string>(right.Count);", except);
        Assert.Contains("if (!resultRightKeys.Contains((string)resultLeftRow.Name))", except);
        Assert.Contains("__musoqFinalShapeRows.Add(new LeftShape0((string)resultLeftRow.Name));", except);
        Assert.IsFalse(except.Contains("new Table(\"left\"", StringComparison.Ordinal));
        Assert.IsFalse(except.Contains("new Table(\"right\"", StringComparison.Ordinal));
        Assert.IsFalse(except.Contains(".AddUnchecked(", StringComparison.Ordinal));
        Assert.IsFalse(except.Contains("Except(left, right", StringComparison.Ordinal));

        Assert.Contains("SetOperation [result = left Intersect right, HashSet]", intersect);
        Assert.Contains("CreateRowBuffer [left: List<LeftRow0>]", intersect);
        Assert.Contains("CreateRowBuffer [right: List<RightRow0>]", intersect);
        Assert.Contains("var resultRightKeys = new HashSet<string>(right.Count);", intersect);
        Assert.Contains("if (resultRightKeys.Contains((string)resultLeftRow.Name))", intersect);
        Assert.Contains("__musoqFinalShapeRows.Add(new LeftShape0((string)resultLeftRow.Name));", intersect);
        Assert.IsFalse(intersect.Contains("new Table(\"left\"", StringComparison.Ordinal));
        Assert.IsFalse(intersect.Contains("new Table(\"right\"", StringComparison.Ordinal));
        Assert.IsFalse(intersect.Contains(".AddUnchecked(", StringComparison.Ordinal));
        Assert.IsFalse(intersect.Contains("Intersect(left, right", StringComparison.Ordinal));

        Assert.Contains("CreateShapeRows [result: ResultShape0 from ResultRow0]", unionAll);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: ko3iko.Name)]", unionAll);
        Assert.Contains("AppendShape [result <- ResultShape0(Name: vo04qt.Name)]", unionAll);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(ko3iko.Name));", unionAll);
        Assert.Contains("__musoqFinalShapeRows.Add(new ResultShape0(vo04qt.Name));", unionAll);
        Assert.IsFalse(unionAll.Contains(".AddUnchecked(", StringComparison.Ordinal));
        Assert.IsFalse(unionAll.Contains("SetOperation [result = left UnionAll right]", StringComparison.Ordinal));
        Assert.IsFalse(unionAll.Contains("UnionAll(", StringComparison.Ordinal));
        Assert.IsFalse(unionAll.Contains("private sealed class LeftRow0", StringComparison.Ordinal));
        Assert.IsFalse(unionAll.Contains("private sealed class RightRow0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void DirectInterpretationSamples_WhenCompiledForInspection_ShouldFuseFinalProjection()
    {
        foreach (var fileName in DirectInterpretationProjectionSampleFileNames)
        {
            var result = CompileSampleForInspection(fileName);
            var failures = GetDirectInterpretationProjectionShapeFailures(
                fileName,
                result.ExecutionPlanText,
                result.GeneratedCSharpCode);

            Assert.IsEmpty(failures, $"{fileName} has stale interpretation projection shape: {string.Join(", ", failures)}");
        }
    }

    [TestMethod]
    public void DirectInterpretationSamples_WhenCheckedIn_ShouldFuseFinalProjection()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName, static sample => sample.Content);

        foreach (var fileName in DirectInterpretationProjectionSampleFileNames)
        {
            var content = samples[fileName];
            var failures = GetDirectInterpretationProjectionShapeFailures(fileName, content, content);

            Assert.IsEmpty(failures, $"{fileName} has stale interpretation projection shape: {string.Join(", ", failures)}");
        }
    }

    [TestMethod]
    public void NestedInterpretationExpansionSamples_WhenCompiledForInspection_ShouldFuseFinalProjectionBoundary()
    {
        foreach (var fileName in NestedInterpretationExpansionSampleFileNames)
        {
            var result = CompileSampleForInspection(fileName);
            var failures = GetNestedInterpretationExpansionShapeFailures(
                fileName,
                result.ExecutionPlanText,
                result.GeneratedCSharpCode);

            Assert.IsEmpty(failures, $"{fileName} has stale nested interpretation expansion shape: {string.Join(", ", failures)}");
        }
    }

    [TestMethod]
    public void NestedInterpretationExpansionSamples_WhenCheckedIn_ShouldFuseFinalProjectionBoundary()
    {
        var samples = ReadSamples().ToDictionary(static sample => sample.FileName, static sample => sample.Content);

        foreach (var fileName in NestedInterpretationExpansionSampleFileNames)
        {
            var content = samples[fileName];
            var failures = GetNestedInterpretationExpansionShapeFailures(fileName, content, content);

            Assert.IsEmpty(failures, $"{fileName} has stale nested interpretation expansion shape: {string.Join(", ", failures)}");
        }
    }
}
