using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;

namespace Musoq.Evaluator.Tests;

public sealed partial class NegatedPredicateTests
{
    private sealed record NamedPredicate(string Sql, Func<int, int, bool> Accepts);

    private static readonly NamedPredicate[] NamedPredicates =
    [
        new("NOT (r.Score = l.Score)", (l, r) => r != l),
        new("NOT (r.Score <> l.Score)", (l, r) => r == l),
        new("NOT (r.Score < l.Score)", (l, r) => r >= l),
        new("NOT (r.Score <= l.Score)", (l, r) => r > l),
        new("NOT (r.Score > l.Score)", (l, r) => r <= l),
        new("NOT (r.Score >= l.Score)", (l, r) => r < l),
        new("r.Score <> l.Score", (l, r) => r != l),
        new("r.Score = l.Score", (l, r) => r == l),
        new("r.Score >= l.Score", (l, r) => r >= l),
        new("r.Score > l.Score", (l, r) => r > l),
        new("r.Score <= l.Score", (l, r) => r <= l),
        new("r.Score < l.Score", (l, r) => r < l),
        new("NOT (l.Score > r.Score)", (l, r) => r >= l),
        new("NOT (r.Score > l.Score + 2)", (l, r) => r <= l + 2),
        new("NOT (r.Score < 2)", (_, r) => r >= 2),
        new("NOT (l.Score < 2)", (l, _) => l >= 2),
        new("NOT (r.Score < l.Score AND r.Score = 1)", (l, r) => !(r < l && r == 1)),
        new("NOT (r.Score < l.Score OR r.Score > l.Score + 2)", (l, r) => r >= l && r <= l + 2),
        new("NOT (NOT (r.Score < l.Score))", (l, r) => r < l),
        new("NOT (NOT (NOT (r.Score < l.Score)))", (l, r) => r >= l),
        new("l.Score >= 0 AND NOT (r.Score < l.Score)", (l, r) => l >= 0 && r >= l),
        new("NOT (1 = 1)", (_, _) => false),
        new("NOT (1 = 0)", (_, _) => true),
        new("NOT (r.Score in (0, 2))", (_, r) => r != 0 && r != 2),
        new("NOT (r.Score between 0 and 2)", (_, r) => r < 0 || r > 2),
        new("NOT (ToString(r.Score) = '2')", (_, r) => r != 2),
        new("NOT (ToString(r.Score) rlike '^2$')", (_, r) => r != 2),
        new("NOT (ToString(r.Score) like '2%')", (_, r) => r != 2),
        new("NOT (r.Name like 'item-2')", (_, r) => r != 2),
        new("NOT (r.Name rlike '^item-2$')", (_, r) => r != 2),
        new("NOT (r.Name in ('item-0', 'item-2'))", (_, r) => r != 0 && r != 2),
        new("NOT (r.Name contains ('item-0', 'item-2'))", (_, r) => r != 0 && r != 2),
        new("NOT (r.Name is null)", (_, _) => true),
        new("r.Score >= l.Score OR r.Score <> 1", (l, r) => r >= l || r != 1),
        new("r.Score >= l.Score AND r.Score <= l.Score + 2", (l, r) => r >= l && r <= l + 2)
    ];

    public static IEnumerable<object[]> NamedExecutionCases =>
        from index in Enumerable.Range(0, NamedPredicates.Length)
        from shape in new[] { "direct", "cte", "nested-cte", "on" }
        from mode in Modes
        select new object[] { index, shape, mode };

    [TestMethod]
    [DynamicData(nameof(NamedExecutionCases))]
    public void NamedPredicate_WhenJoined_ShouldReturnExactPairMultiset(int index, string shape, SourcePlanningMode mode)
    {
        var predicate = NamedPredicates[index];
        AssertPairs(predicate.Sql, predicate.Accepts, new ExecutionScenario(shape, mode, CreateRows()));
    }

    [TestMethod]
    [DataRow("NOT (r.Score < l.Score)", "1:10,1:11,1:12,2:12")]
    [DataRow("NOT (r.Score > l.Score + 2)", "1:10,1:11,1:12,2:10,2:11,2:12")]
    public void OriginalOccurrenceFixture_ShouldReturnExplicitExpectedPairs(string predicate, string expected)
    {
        var rows = new Dictionary<string, IReadOnlyList<SourcePlanningEntity>>
        {
            ["#left"] = [Row(1, 1, 1), Row(2, 2, 1)],
            ["#right"] = [Row(10, 1, 1), Row(11, 1, 1), Row(12, 3, 1)]
        };
        var provider = new SourcePlanningSchemaProvider(SourcePlanningMode.RejectAll, rows);
        var compiled = CreateAndRunVirtualMachine(PairQuery(predicate, "direct"), schemaProvider: provider);
        var actual = TableMaterializationTestHelper.Materialize(compiled.Run())
            .Select(row => $"{row.ReadValue(0)}:{row.ReadValue(1)}").Order(StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(expected.Split(','), actual);
    }

    [TestMethod]
    [DataRow("#left")]
    [DataRow("#right")]
    public void WhenJoinInputIsEmpty_ShouldReturnNoPairs(string emptySource)
    {
        var rows = CreateRows().ToDictionary(entry => entry.Key, entry => entry.Value);
        rows[emptySource] = [];
        AssertPairs("NOT (r.Score < l.Score)", (l, r) => r >= l,
            new ExecutionScenario("direct", SourcePlanningMode.AcceptPredicate, rows));
    }

    [TestMethod]
    public void WhenThreeSourcesHaveNegatedCrossSourcePredicate_ShouldPreserveAllMatchingTriples()
    {
        var rows = CreateRows().ToDictionary(entry => entry.Key, entry => entry.Value);
        rows["#third"] = [Row(20, 1, 1), Row(21, 2, 2)];
        var expected = (from left in rows["#left"] from right in rows["#right"] from third in rows["#third"]
                        where left.JoinKey == right.JoinKey && right.JoinKey == third.JoinKey
                        where !(right.Score < left.Score || third.Score > right.Score)
                        select $"{left.Id}:{right.Id}:{third.Id}").Order(StringComparer.Ordinal).ToArray();
        var compiled = CreateAndRunVirtualMachine(
            "select l.Id, r.Id, t.Id from #left.items() l join #right.items() r on l.JoinKey = r.JoinKey join #third.items() t on r.JoinKey = t.JoinKey where NOT (r.Score < l.Score OR t.Score > r.Score)",
            schemaProvider: new SourcePlanningSchemaProvider(SourcePlanningMode.AcceptFirstPredicate, rows));
        var actual = TableMaterializationTestHelper.Materialize(compiled.Run())
            .Select(row => $"{row.ReadValue(0)}:{row.ReadValue(1)}:{row.ReadValue(2)}").Order(StringComparer.Ordinal).ToArray();

        CollectionAssert.AreEqual(expected, actual);
    }
}
