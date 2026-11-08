using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeProfiledSamplesShapeTests
{
    private const string RecursiveUnionAllDisabledFileName = "P08_RecursiveUnionAll_Disabled.cs";
    private const string RecursiveUnionAllSourceBoundariesFileName = "P09_RecursiveUnionAll_SourceBoundaries.cs";
    private const string RecursiveUnionAllFullFileName = "P10_RecursiveUnionAll_Full.cs";
    private const string RecursiveKeyedUnionFullFileName = "P11_RecursiveKeyedUnion_Full.cs";
    private const string RecursiveInvariantIndexedEdgesFullFileName = "P12_RecursiveInvariantIndexedEdges_Full.cs";
    private const string RecursiveTypedInvariantDirectIndexFullFileName =
        "P13_RecursiveTypedInvariantDirectIndex_Full.cs";

    [TestMethod]
    public void RecursiveUnionAllDisabledSample_WhenCheckedIn_ShouldHaveZeroProfilingHotPath()
    {
        var sample = ReadProfiledSample(RecursiveUnionAllDisabledFileName);

        AssertDoesNotContainAny(sample, DisabledProfilingMarkers, RecursiveUnionAllDisabledFileName);
    }

    [TestMethod]
    public void RecursiveUnionAllSourceBoundariesSample_WhenCheckedIn_ShouldAvoidOperatorCounters()
    {
        var sample = ReadProfiledSample(RecursiveUnionAllSourceBoundariesFileName);

        AssertContainsAll(
            sample,
            [
                "IProfiledRunnable",
                "RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)",
                "while (cte0CurrentFrontier.Count > 0)"
            ],
            RecursiveUnionAllSourceBoundariesFileName);
        AssertDoesNotContainAny(
            sample,
            [
                "GetOperatorHandle",
                "BeginOperatorValue",
                "AddOperatorInputRows",
                "AddOperatorOutputRows"
            ],
            RecursiveUnionAllSourceBoundariesFileName);
    }

    [TestMethod]
    public void RecursiveUnionAllFullSample_WhenCheckedIn_ShouldProfileOneScopeAndAggregateFrontiers()
    {
        var sample = ReadProfiledSample(RecursiveUnionAllFullFileName);
        var recursive = ResolveOperator(
            sample,
            RecursiveUnionAllFullFileName,
            "RecursiveCte",
            "counter; result cte0");
        var append = ResolveOperator(
            sample,
            RecursiveUnionAllFullFileName,
            "RecursiveAppend",
            "cte0NextFrontier <-");
        var recursivePrefix = CreateScopePrefix(recursive);

        AssertContainsBeginOperator(sample, RecursiveUnionAllFullFileName, recursive);
        AssertContainsTryFinallyOperatorScope(sample, RecursiveUnionAllFullFileName, recursivePrefix);
        AssertContainsAll(
            sample,
            [
                $"long {recursivePrefix}InputRows = 0L;",
                $"long {recursivePrefix}OutputRows = 0L;",
                $"{recursivePrefix}InputRows += cte0CurrentFrontier.Count;",
                $"{recursivePrefix}OutputRows += cte0NextFrontier.Count;",
                $"{recursivePrefix}Scope.AddInputRows({recursivePrefix}InputRows);",
                $"{recursivePrefix}Scope.AddOutputRows({recursivePrefix}OutputRows);"
            ],
            RecursiveUnionAllFullFileName);
        AssertContainsCounterInputRows(sample, RecursiveUnionAllFullFileName, append, "1");
        AssertContainsCounterOutputRows(sample, RecursiveUnionAllFullFileName, append, "1");
        AssertDoesNotContainBeginOperator(sample, RecursiveUnionAllFullFileName, append);
    }

    [TestMethod]
    public void RecursiveKeyedUnionFullSample_WhenCheckedIn_ShouldCountOnlyAcceptedTypedKeys()
    {
        var sample = ReadProfiledSample(RecursiveKeyedUnionFullFileName);
        var append = ResolveOperator(
            sample,
            RecursiveKeyedUnionFullFileName,
            "RecursiveAppend",
            "cte0NextFrontier <-");
        var appendPrefix = CreateScopePrefix(append);
        var seenDeclaration = sample.IndexOf("var cte0Seen = new HashSet<int>();", StringComparison.Ordinal);
        var seenAdd = sample.IndexOf("if (cte0Seen.Add(__cte0NextFrontierCandidate0))", StringComparison.Ordinal);
        var acceptedCounter = sample.IndexOf($"{appendPrefix}OutputRows += 1;", seenAdd, StringComparison.Ordinal);
        var resultStore = sample.IndexOf("_cteRowResults.Slot0 = cte0;", seenAdd, StringComparison.Ordinal);

        AssertContainsAll(
            sample,
            [
                "var cte0Seen = new HashSet<int>();",
                $"{appendPrefix}InputRows += 1;",
                $"profileRecorder?.AddOperatorInputRows({appendPrefix}Handle, {appendPrefix}InputRows);",
                $"profileRecorder?.AddOperatorOutputRows({appendPrefix}Handle, {appendPrefix}OutputRows);"
            ],
            RecursiveKeyedUnionFullFileName);
        Assert.IsGreaterThanOrEqualTo(0, seenAdd, "The typed seen-set probe should be generated.");
        Assert.IsGreaterThan(seenAdd, acceptedCounter, "Accepted output must be counted inside the seen-set branch.");
        Assert.IsGreaterThan(seenDeclaration, resultStore, "The recursive hot path should end at the CTE result store.");
        AssertDoesNotContainAny(
            sample[seenDeclaration..resultStore],
            ["HashSet<object", "object[]"],
            RecursiveKeyedUnionFullFileName);
        AssertDoesNotContainBeginOperator(sample, RecursiveKeyedUnionFullFileName, append);
    }

    [TestMethod]
    public void RecursiveInvariantIndexedEdgesFullSample_WhenCheckedIn_ShouldBuildDirectTypedIndexBeforeLoop()
    {
        var sample = ReadProfiledSample(RecursiveInvariantIndexedEdgesFullFileName);
        var hash = sample.IndexOf(
            "var cte0Invariant0Hash = new Dictionary<int, HashJoinBucket<Cte0Invariant0Row0>>",
            StringComparison.Ordinal);
        var loop = sample.IndexOf("while (cte0CurrentFrontier.Count > 0)", StringComparison.Ordinal);

        Assert.AreEqual(1, CountProfiledOccurrences(sample, "CreateHash [cte0Invariant0Hash: int -> Row]"));
        Assert.AreEqual(1, CountProfiledOccurrences(sample, "profileRecorder?.CreateSourceRecorder(\"e\")"));
        Assert.IsGreaterThanOrEqualTo(0, hash, "The invariant hash should use its narrow typed carrier.");
        Assert.IsGreaterThan(hash, loop, "Invariant hash construction must remain outside the fixed-point loop.");
        AssertContainsAll(
            sample,
            [
                "private readonly struct Cte0Invariant0Row0",
                "ProfiledChunkedEnumerable<Musoq.Evaluator.Tests.Schema.RecursiveCte.RecursiveGraphEdge>.Create",
                "HashProbe [cte0Invariant0Hash[r.Id] -> cte0Invariant0HashMatches]"
            ],
            RecursiveInvariantIndexedEdgesFullFileName);
        AssertDoesNotContainAny(
            sample,
            ["MaterializeChunked [", "MaterializeChunkedRows", "List<Cte0Invariant0Row0>"],
            RecursiveInvariantIndexedEdgesFullFileName);
    }

    [TestMethod]
    public void RecursiveTypedInvariantDirectIndexFullSample_WhenCheckedIn_ShouldAvoidRedundantSnapshotList()
    {
        var sample = ReadProfiledSample(RecursiveTypedInvariantDirectIndexFullFileName);
        var hash = sample.IndexOf("new Dictionary<", StringComparison.Ordinal);
        var loop = sample.IndexOf("while (cte0CurrentFrontier.Count > 0)", StringComparison.Ordinal);

        Assert.IsGreaterThanOrEqualTo(0, hash);
        Assert.IsGreaterThan(hash, loop);
        AssertContainsAll(
            sample,
            [
                "private readonly struct Cte0Invariant0Row0",
                "RecursiveSnapshotGuard [",
                "HashProbe ["
            ],
            RecursiveTypedInvariantDirectIndexFullFileName);
        AssertDoesNotContainAny(
            sample,
            ["List<Cte0Invariant0Row0>", "MaterializeChunkedRows"],
            RecursiveTypedInvariantDirectIndexFullFileName);
    }

    private static int CountProfiledOccurrences(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;
}
