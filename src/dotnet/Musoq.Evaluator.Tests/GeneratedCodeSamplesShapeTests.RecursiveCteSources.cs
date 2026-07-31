using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    private static readonly string[] RecursiveSnapshotSampleFileNames =
    [
        "Q198_RecursiveInnerJoinEdges.cs",
        "Q199_RecursiveCrossJoinFilter.cs",
        "Q202_RecursiveInvariantSourceSnapshot.cs",
        "Q203_RecursiveInvariantHashLookup.cs",
        "Q223_RecursiveUncorrelatedApplySnapshot.cs",
        "Q224_RecursiveCompositeInvariantSubplan.cs",
        "Q225_RecursiveMutableSourceValueSnapshot.cs",
        "Q226_RecursiveSnapshotLimitCodeShape.cs"
    ];

    private static readonly string[] RecursiveCorrelatedApplySampleFileNames =
    [
        "Q200_RecursiveCrossApplyNeighbors.cs",
        "Q201_RecursiveOuterApplyNeighbors.cs"
    ];

    private static readonly string[] RecursiveWaveFiveSampleFileNames =
    [
        .. RecursiveSnapshotSampleFileNames,
        .. RecursiveCorrelatedApplySampleFileNames,
        "Q204_RecursivePriorValuesCteEdges.cs"
    ];

    public static IEnumerable<object[]> RecursiveSnapshotSampleData =>
        RecursiveSnapshotSampleFileNames.Select(static fileName => new object[] { fileName });

    public static IEnumerable<object[]> RecursiveCorrelatedApplySampleData =>
        RecursiveCorrelatedApplySampleFileNames.Select(static fileName => new object[] { fileName });

    [TestMethod]
    [DynamicData(nameof(RecursiveSnapshotSampleData))]
    public void RecursiveInvariantSourceSample_ShouldPlanTypedInputBeforeRecursiveMember(string fileName)
    {
        var sample = ReadSample(fileName);
        var execution = ReadExecutionPlan(fileName);
        var code = ReadGeneratedCode(fileName);
        var physical = ReadGeneratedSampleSection(sample.Content, "Physical Plan", "Execution Plan");
        var setupIndex = execution.IndexOf("  InvariantSetup", StringComparison.Ordinal);
        var memberIndex = execution.IndexOf(
            "  RecursiveMember",
            setupIndex + "  InvariantSetup".Length,
            StringComparison.Ordinal);
        var createIndex = execution.IndexOf("Create", setupIndex, StringComparison.Ordinal);
        var carrierIndex = code.IndexOf("private readonly struct Cte0Invariant0Row0", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, setupIndex, fileName);
        Assert.IsGreaterThan(setupIndex, createIndex, fileName);
        Assert.IsGreaterThan(createIndex, memberIndex, fileName);
        Assert.IsGreaterThanOrEqualTo(0, carrierIndex, fileName);
        Assert.Contains("Invariant [__recursive_", physical, fileName);
        var expectedSnapshotLimit = fileName == "Q226_RecursiveSnapshotLimitCodeShape.cs" ? 1 : 10_000_000;
        Assert.Contains($"max snapshot rows {expectedSnapshotLimit}", execution, fileName);
        Assert.AreEqual(1, CountText(execution, "RecursiveSnapshotGuard ["), fileName);
        Assert.AreEqual(1, CountText(code, "int __cte0SnapshotRows = 0;"), fileName);
        Assert.IsGreaterThan(0, CountText(code, "MQ7009_RecursiveCteSnapshotLimitExceeded"), fileName);
        Assert.AreEqual(
            CountText(code, "MQ7009_RecursiveCteSnapshotLimitExceeded"),
            CountText(code, "__cte0SnapshotRows++;"),
            fileName);
        Assert.AreEqual(1, CountText(code, "while (cte0CurrentFrontier.Count > 0)"), fileName);
        Assert.IsFalse(execution.Contains("MaterializeChunked [", StringComparison.Ordinal), fileName);
        Assert.IsFalse(code.Contains("MaterializeChunkedRows", StringComparison.Ordinal), fileName);
    }

    [TestMethod]
    public void RecursiveInvariantHashSample_ShouldBuildTypedIndexOutsideFixedPointLoop()
    {
        var sample = ReadSample("Q203_RecursiveInvariantHashLookup.cs");
        var code = ReadGeneratedCode("Q203_RecursiveInvariantHashLookup.cs");
        var execution = ReadExecutionPlan(sample.FileName);
        var setupIndex = execution.IndexOf("  InvariantSetup", StringComparison.Ordinal);
        var memberIndex = execution.IndexOf(
            "  RecursiveMember",
            setupIndex + "  InvariantSetup".Length,
            StringComparison.Ordinal);
        var hashPlanIndex = execution.IndexOf("CreateHash [", setupIndex, StringComparison.Ordinal);
        var hashCodeIndex = code.IndexOf(
            "var cte0Invariant0Hash = new Dictionary<int, HashJoinBucket<Cte0Invariant0Row0>>",
            StringComparison.Ordinal);
        var loopCodeIndex = sample.Content.IndexOf(
            "while (cte0CurrentFrontier.Count > 0)",
            StringComparison.Ordinal);

        Assert.IsGreaterThan(setupIndex, hashPlanIndex);
        Assert.IsGreaterThan(hashPlanIndex, memberIndex);
        Assert.IsGreaterThanOrEqualTo(0, hashCodeIndex);
        Assert.IsGreaterThan(hashCodeIndex, loopCodeIndex);
        Assert.IsGreaterThan(
            code.IndexOf("if (__cte0SnapshotRows >= 10000000)", StringComparison.Ordinal),
            code.IndexOf("__cte0SnapshotRows++;", StringComparison.Ordinal));
        Assert.IsGreaterThan(
            code.IndexOf("__cte0SnapshotRows++;", StringComparison.Ordinal),
            code.IndexOf("Cte0Invariant0Row0 cte0Invariant0Row", StringComparison.Ordinal));
        Assert.AreEqual(1, CountText(code, "var cte0Invariant0Hash = new Dictionary<"));
        Assert.IsFalse(
            execution[memberIndex..].Contains("CreateHash [cte0Invariant0Hash", StringComparison.Ordinal));
    }

    [TestMethod]
    [DynamicData(nameof(RecursiveCorrelatedApplySampleData))]
    public void RecursiveCorrelatedApplySample_ShouldKeepSourceInsideRecursiveMember(string fileName)
    {
        var sample = ReadSample(fileName);
        var execution = ReadExecutionPlan(fileName);
        var code = ReadGeneratedCode(fileName);
        var memberIndex = execution.IndexOf("  RecursiveMember", StringComparison.Ordinal);
        var sourceIndex = execution.IndexOf("SourceScan [e: RecursiveGraphEdge]", memberIndex, StringComparison.Ordinal);

        Assert.IsFalse(execution.Contains("  InvariantSetup", StringComparison.Ordinal), fileName);
        Assert.IsGreaterThanOrEqualTo(0, memberIndex, fileName);
        Assert.IsGreaterThan(memberIndex, sourceIndex, fileName);
        Assert.IsFalse(execution.Contains("CreateTable [cte0NextFrontier_statement", StringComparison.Ordinal), fileName);
        Assert.IsFalse(execution.Contains("StoreTable [cte0NextFrontier_statement", StringComparison.Ordinal), fileName);
        Assert.IsFalse(code.Contains("_tableResults[", StringComparison.Ordinal), fileName);
        Assert.IsFalse(execution.Contains("NextFrontierStatement", StringComparison.Ordinal), fileName);
    }

    [TestMethod]
    public void RecursivePriorValuesCteSample_ShouldUseTypedHashPayloadWithoutRecursiveContext()
    {
        var sample = ReadSample("Q204_RecursivePriorValuesCteEdges.cs");

        var code = ReadGeneratedCode(sample.FileName);
        Assert.Contains("private readonly struct Cte0HashPayload0", code);
        Assert.Contains("Dictionary<int, HashJoinBucket<Cte0HashPayload0>>", code);
        Assert.IsFalse(code.Contains("__context0", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("object[] __contexts", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("Dictionary<object", StringComparison.Ordinal));
    }

    [TestMethod]
    public void RecursiveWaveFiveSamples_ShouldAvoidGeneralPurposeHotPathConstructs()
    {
        foreach (var fileName in RecursiveWaveFiveSampleFileNames)
        {
            var sample = ReadSample(fileName);

            var code = ReadGeneratedCode(fileName);
            Assert.IsFalse(code.Contains(".Select(", StringComparison.Ordinal), fileName);
            Assert.IsFalse(code.Contains(".Where(", StringComparison.Ordinal), fileName);
            Assert.IsFalse(code.Contains("System.Reflection", StringComparison.Ordinal), fileName);
            Assert.IsFalse(code.Contains("Action<", StringComparison.Ordinal), fileName);
            Assert.IsFalse(code.Contains("Func<", StringComparison.Ordinal), fileName);
        }
    }
}
