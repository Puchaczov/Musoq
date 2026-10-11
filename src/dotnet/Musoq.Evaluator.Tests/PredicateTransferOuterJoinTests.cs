using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PredicateTransferOuterJoinTests : BasicEntityTestBase
{
    private sealed record OuterCase(string Id, string Sql, Func<SourcePlanningEntity?, SourcePlanningEntity?, bool?> Evaluate);
    private static readonly SourcePlanningEntity[] Left = [Row(1, 0, 1, null), Row(2, 1, 1, "beta"), Row(3, 2, 1, "item-2"), Row(4, 3, 2, "x"), Row(5, 4, 3, "y"), Row(6, 5, 9, null)];
    private static readonly SourcePlanningEntity[] Right = [Row(10, 0, 1, "beta"), Row(11, 1, 1, null), Row(12, 2, 2, "x"), Row(13, 3, 3, "item-3"), Row(14, 4, 8, null)];

    private static OuterCase[] Cases()
    {
        var probes = new OuterCase[] {
            new("right-null","r.Name is null",(l,r)=>r?.Name==null),
            new("left-null","l.Name is null",(l,r)=>l?.Name==null),
            new("right-not-null","r.Name is not null",(l,r)=>r?.Name!=null),
            new("left-not-null","l.Name is not null",(l,r)=>l?.Name!=null),
            new("right-like-not","NOT (r.Name like 'item-%')",(l,r)=>!(r?.Name?.StartsWith("item-",StringComparison.OrdinalIgnoreCase)??false)),
            new("right-regex-not","NOT (r.Name rlike '^item-')",(l,r)=>!(r?.Name?.StartsWith("item-",StringComparison.Ordinal)??false)),
            new("right-like-is-null","(r.Name like 'item-%') is null",(l,r)=>false),
            new("null-or-bound","r.Score >= 5 OR r.Name is null",(l,r)=>(r==null?(bool?)null:r.Score>=5)|(r?.Name==null)),
            new("right-contains-null","r.Name contains ('beta',null)",(l,r)=>r?.Name==null||r.Name=="beta"),
            new("right-in","r.Score in (0,2)",(l,r)=>r==null?null:r.Score is 0 or 2),
            new("right-negative","NOT (r.Name = 'beta')",(l,r)=>r?.Name==null?null:r.Name!="beta"),
            new("cross-compare","r.Score >= l.Score",(l,r)=>l==null||r==null?null:r.Score>=l.Score),
        };
        return probes;
    }

    public static IEnumerable<object[]> Scenarios =>
        from index in Enumerable.Range(0, Cases().Length)
        from kind in new[] { "inner", "left", "right", "full outer" }
        from placement in new[] { "where", "on" }
        from cte in new[] { false, true }
        from mode in new[] { SourcePlanningMode.RejectAll, SourcePlanningMode.AcceptPredicate,
            SourcePlanningMode.AcceptFirstPredicate, SourcePlanningMode.AcceptTypedStringPredicate }
        select new object[] { index, kind, placement, mode, cte };

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void PredicateAtJoinBoundary_ShouldPreserveNullExtension(int index, string kind, string placement, SourcePlanningMode mode, bool cte)
    {
        Verify(Cases()[index], kind, placement, mode, cte, Left, Right);
    }

    public static IEnumerable<object[]> EmptyScenarios =>
        from emptySide in new[] { "left", "right", "both" }
        from kind in new[] { "inner", "left", "right", "full outer" }
        from index in new[] { 0, 4, 8, 9 }
        from mode in new[] { SourcePlanningMode.RejectAll, SourcePlanningMode.AcceptPredicate,
            SourcePlanningMode.AcceptFirstPredicate, SourcePlanningMode.AcceptTypedStringPredicate }
        select new object[] { emptySide, kind, index, mode };

    [TestMethod]
    [DynamicData(nameof(EmptyScenarios))]
    public void EmptyJoinInput_ShouldPreserveExpectedNullExtendedRows(string emptySide, string kind, int index, SourcePlanningMode mode)
    {
        Verify(Cases()[index], kind, "where", mode, false,
            emptySide is "left" or "both" ? [] : Left, emptySide is "right" or "both" ? [] : Right);
    }

    private void Verify(OuterCase probe, string kind, string placement, SourcePlanningMode mode, bool cte,
        SourcePlanningEntity[] left, SourcePlanningEntity[] right)
    {
        var pairs = Join(left, right, kind, placement == "on" ? probe.Evaluate : (_, _) => true);
        var expected = pairs.Where(pair => placement == "on" || probe.Evaluate(pair.Item1, pair.Item2) == true)
            .Select(pair => $"{Id(pair.Item1)}:{Id(pair.Item2)}").Order().ToArray();
        var tail = placement == "on" ? $"and ({probe.Sql})" : $"where {probe.Sql}";
        var prefix = cte ? "with lefts as (select x.Id,x.Score,x.Name,x.JoinKey from #left.items() x), rights as (select x.Id,x.Score,x.Name,x.JoinKey from #right.items() x) " : "";
        var leftSource = cte ? "lefts" : "#left.items()";
        var rightSource = cte ? "rights" : "#right.items()";
        var query = $"{prefix}select l.Id as LeftId,r.Id as RightId from {leftSource} l {kind} join {rightSource} r on l.JoinKey=r.JoinKey {tail}";
        var provider = new SourcePlanningSchemaProvider(mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>> { ["#left"] = left, ["#right"] = right });
        using var compiled = CreateAndRunVirtualMachine(query, schemaProvider: provider);
        var actual = compiled.Run()
            .Select(row => $"{row.ReadValue(0) ?? "null"}:{row.ReadValue(1) ?? "null"}").Order().ToArray();
        CollectionAssert.AreEqual(expected, actual, $"{probe.Id}; {mode}; {query}");
    }

    static IEnumerable<(SourcePlanningEntity?, SourcePlanningEntity?)> Join(SourcePlanningEntity[] left, SourcePlanningEntity[] right, string kind, Func<SourcePlanningEntity?, SourcePlanningEntity?, bool?> on)
    {
        var result = new List<(SourcePlanningEntity?, SourcePlanningEntity?)>();
        foreach (var l in left)
        {
            var matches = right.Where(r => r.JoinKey == l.JoinKey && on(l, r) == true).ToArray();
            result.AddRange(matches.Select(r => ((SourcePlanningEntity?)l, (SourcePlanningEntity?)r)));
            if (matches.Length == 0 && kind is "left" or "full outer") result.Add((l, null));
        }
        if (kind is "right" or "full outer")
            result.AddRange(right.Where(r => !left.Any(l => l.JoinKey == r.JoinKey && on(l, r) == true)).Select(r => ((SourcePlanningEntity?)null, (SourcePlanningEntity?)r)));
        return result;
    }
    static string Id(SourcePlanningEntity? row) => row?.Id.ToString() ?? "null";
    static SourcePlanningEntity Row(int id, int score, int key, string? name) => new() { Id = id, Score = score, JoinKey = key, Name = name, Category = $"file-{key}" };
}
