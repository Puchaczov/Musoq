using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.SourcePlanning;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class PredicateTransferAtomTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow("s.Name IN ('beta',NULL)", "beta:2")]
    [DataRow("s.Name NOT IN ('beta',NULL)", "")]
    [DataRow("(s.Name IN ('beta',NULL)) IS NULL", "Alpha:1,item:1,null:1")]
    public void NullableMembershipInHaving_ShouldPreserveGroupedTruthTable(string predicate, string expected)
    {
        var provider = new SourcePlanningSchemaProvider(SourcePlanningMode.AcceptPredicate,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>> { ["#single"] = Rows });
        using var compiled = CreateAndRunVirtualMachine(
            $"select s.Name,Count(s.Id) from #single.items() s group by s.Name having {predicate}", schemaProvider: provider);
        var actual = compiled.Run().Select(row => $"{row.ReadValue(0) ?? "null"}:{row.ReadValue(1)}").Order(StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(expected.Length == 0 ? [] : expected.Split(','), actual);
        Assert.IsNull(provider.Requests.Single().Predicate);
    }

    [TestMethod]
    [DataRow("membershipValue")]
    [DataRow("membershipValue1")]
    public void MembershipLookup_ShouldNotCollideWithSourceAlias(string alias)
    {
        var provider = new SourcePlanningSchemaProvider(SourcePlanningMode.RejectAll,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>> { ["#single"] = Rows });
        using var compiled = CreateAndRunVirtualMachine(
            $"select {alias}.Id from #single.items() {alias} where {alias}.Score IN (0,1,2,3,4)", schemaProvider: provider);
        CollectionAssert.AreEqual(Rows.Select(row => row.Id).Order().ToArray(),
            compiled.Run().Select(row => (int)row.ReadValue(0)!).Order().ToArray());
    }

    [TestMethod]
    [DataRow(SourcePlanningMode.RejectAll)]
    [DataRow(SourcePlanningMode.AcceptPredicate)]
    [DataRow(SourcePlanningMode.AcceptFirstPredicate)]
    [DataRow(SourcePlanningMode.AcceptTypedStringPredicate)]
    public void MembershipBeforeAggregateWindowAndPaging_ShouldPreserveExactResults(SourcePlanningMode mode)
    {
        var provider = new SourcePlanningSchemaProvider(mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>> { ["#single"] = Rows });
        using var aggregate = CreateAndRunVirtualMachine(
            "select Count(s.Id) from #single.items() s where s.Score IN (1L,2L)", schemaProvider: provider);
        Assert.AreEqual(3, Convert.ToInt32(aggregate.Run().Single().ReadValue(0)));
        using var window = CreateAndRunVirtualMachine(
            "select s.Id, ROW_NUMBER() OVER (ORDER BY s.Id) as rn from #single.items() s where s.Name IN ('beta','x','y','z',NULL) order by rn skip 1 take 1",
            schemaProvider: provider);
        var row = window.Run().Single();
        Assert.AreEqual(3, row.ReadValue(0));
        Assert.AreEqual(2L, Convert.ToInt64(row.ReadValue(1)));
    }

    [TestMethod]
    [DataRow(SourcePlanningMode.RejectAll)]
    [DataRow(SourcePlanningMode.AcceptPredicate)]
    [DataRow(SourcePlanningMode.AcceptFirstPredicate)]
    public void ParameterizedRangeInCte_ShouldReevaluateEachExecution(SourcePlanningMode mode)
    {
        var provider = new SourcePlanningSchemaProvider(mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>> { ["#single"] = Rows });
        using var compiled = CreateAndRunVirtualMachine(
            "param(min: int = 1) with selected as (select s.Id,s.Score from #single.items() s where s.Score BETWEEN $min AND $min + 1) select s.Id from selected s order by s.Id",
            schemaProvider: provider);
        foreach (var minimum in new[] { 1, 2, 0, 1 })
        {
            global::Musoq.Tests.Common.NativeParameterTestInputs.Set(compiled.TypedParameters, compiled.ParameterDefinitions, "min", minimum);
            var expected = Rows.Where(row => row.Score >= minimum && row.Score <= minimum + 1).Select(row => row.Id).Order().ToArray();
            CollectionAssert.AreEqual(expected, compiled.Run().Select(row => (int)row.ReadValue(0)!).ToArray());
        }
    }

    public static IEnumerable<object[]> MembershipCases =>
        from size in new[] { 0, 1, 4, 5, 6, 16, 64 }
        from includesNull in new[] { false, true }
        from negated in new[] { false, true }
        from numeric in new[] { false, true }
        from mode in new[] { SourcePlanningMode.RejectAll, SourcePlanningMode.AcceptPredicate,
            SourcePlanningMode.AcceptFirstPredicate, SourcePlanningMode.AcceptTypedStringPredicate }
        select new object[] { size, includesNull, negated, numeric, mode };

    [TestMethod]
    [DynamicData(nameof(MembershipCases))]
    public void MembershipCardinality_ShouldNotChangeNullTruthTable(int size, bool includesNull, bool negated, bool numeric, SourcePlanningMode mode)
    {
        var members = Enumerable.Range(0, size).Select(index => numeric
            ? (index == 0 ? "2L" : (100 + index).ToString() + "L")
            : index == 0 ? "'beta'" : $"'unused-{index}'").ToArray();
        if (includesNull && size != 0) members[^1] = "NULL";
        var field = numeric ? "Score" : "Name";
        var predicate = $"s.{field} {(negated ? "NOT IN" : "IN")} ({string.Join(',', members)})";
        bool Admits(SourcePlanningEntity row)
        {
            if (size == 0) return negated;
            if (!numeric && row.Name == null) return false;
            var matches = !(includesNull && size == 1) && (numeric ? row.Score == 2 : row.Name == "beta");
            return negated ? !matches && !includesNull : matches;
        }
        var provider = new SourcePlanningSchemaProvider(mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>> { ["#single"] = Rows });
        if (size == 0)
        {
            var error = Assert.Throws<Musoq.Converter.Exceptions.MusoqQueryException>(() =>
                CreateAndRunVirtualMachine($"select s.Id from #single.items() s where {predicate}", schemaProvider: provider));
            Assert.Contains("MQ2037", error.Message);
            return;
        }
        var actual = CreateAndRunVirtualMachine($"select s.Id from #single.items() s where {predicate}", schemaProvider: provider)
            .Run().Select(row => (int)row.ReadValue(0)!).Order().ToArray();
        CollectionAssert.AreEqual(Rows.Where(Admits).Select(row => row.Id).Order().ToArray(), actual,
            $"{mode}; {predicate}");
    }

    private sealed record SingleCase(string Id, string Sql, Func<SourcePlanningEntity, bool> Accepts);
    private static readonly SourcePlanningEntity[] Rows =
    [new() { Id=1, Name=null, Score=0 }, new() { Id=2, Name="Alpha", Score=1 },
     new() { Id=3, Name="beta", Score=2 }, new() { Id=4, Name="item", Score=3 },
     new() { Id=3, Name="beta", Score=2 }];

    private static SingleCase[] Cases()
    {
        var probes = new SingleCase[] {
            new("in-null","s.Score IN (0,NULL)",row=>row.Score==0),
            new("not-in-null","s.Score NOT IN (0,NULL)",row=>false),
            new("name-in-null","s.Name IN ('beta',NULL)",row=>row.Name=="beta"),
            new("name-not-in","s.Name NOT IN ('beta')",row=>row.Name!=null&&row.Name!="beta"),
            new("contains-null","s.Name CONTAINS ('beta',NULL)",row=>row.Name==null||row.Name=="beta"),
            new("numeric-contains-null","s.Score CONTAINS (0,NULL)",row=>row.Score==0),
            new("numeric-contains-mixed","s.Score CONTAINS (1L,2L)",row=>row.Score is 1 or 2),
            new("numeric-not-contains-null","NOT (s.Score CONTAINS (0,NULL))",row=>row.Score!=0),
            new("not-equal","s.Name <> 'beta'",row=>row.Name!=null&&row.Name!="beta"),
            new("equal-null","s.Name = NULL",row=>false),
            new("not-equal-null","s.Name <> NULL",row=>false),
            new("name-is-null","s.Name IS NULL",row=>row.Name==null),
            new("between","s.Score BETWEEN 0 AND 2",row=>row.Score>=0&&row.Score<=2),
            new("between-or","s.Score BETWEEN 0 AND 2 OR s.Score = 3",row=>true),
            new("not-between","NOT (s.Score BETWEEN 0 AND 2)",row=>row.Score>2),
            new("like-not","s.Name NOT LIKE 'item%'",row=>!(row.Name?.StartsWith("item",StringComparison.OrdinalIgnoreCase)??false)),
            new("coalesce-null","(s.Name ?? 'none') IS NULL",row=>false),
            new("coalesce-equal","(s.Name ?? 'none') = 'none'",row=>row.Name==null),
            new("in-four-null","s.Name IN ('beta','unused1','unused2',NULL)",row=>row.Name=="beta"),
            new("in-five-null","s.Name IN ('beta','unused1','unused2','unused3',NULL)",row=>row.Name=="beta"),
            new("not-in-four-null","s.Name NOT IN ('beta','unused1','unused2',NULL)",row=>false),
            new("not-in-five-null","s.Name NOT IN ('beta','unused1','unused2','unused3',NULL)",row=>false),
            new("not-in-four","s.Name NOT IN ('beta','unused1','unused2','unused3')",row=>row.Name!=null&&row.Name!="beta"),
            new("not-in-five","s.Name NOT IN ('beta','unused1','unused2','unused3','unused4')",row=>row.Name!=null&&row.Name!="beta"),
            new("numeric-in-five-null","s.Score IN (0,10,11,12,NULL)",row=>row.Score==0),
            new("numeric-not-in-five-null","s.Score NOT IN (0,10,11,12,NULL)",row=>false),
            new("mixed-long-equal","s.Score = 1L",row=>row.Score==1),
            new("mixed-decimal-equal","s.Score = 1d",row=>row.Score==1),
            new("mixed-long-bound","s.Score >= 1L",row=>row.Score>=1),
            new("mixed-membership","s.Score in (1L,2L)",row=>row.Score is 1 or 2),
        };
        return probes;
    }

    public static IEnumerable<object[]> Scenarios =>
        from index in Enumerable.Range(0, Cases().Length)
        from mode in new[] { SourcePlanningMode.RejectAll, SourcePlanningMode.AcceptPredicate,
            SourcePlanningMode.AcceptFirstPredicate, SourcePlanningMode.AcceptTypedStringPredicate }
        from shape in new[] { "direct", "cte", "nested" }
        select new object[] { index, mode, shape };

    [TestMethod]
    [DynamicData(nameof(Scenarios))]
    public void AtomAcrossSourceBoundaries_ShouldMatchIndependentMultiset(int index, SourcePlanningMode mode, string shape)
    {
        var probe = Cases()[index];
        var source = shape == "direct" ? "#single.items()" : "wrapped";
        var prefix = shape switch
        {
            "cte" => "with wrapped as (select s.Id, s.Name, s.Score from #single.items() s) ",
            "nested" => "with first as (select s.Id, s.Name, s.Score from #single.items() s), wrapped as (select s.Id, s.Name, s.Score from first s) ",
            _ => ""
        };
        var query = $"{prefix}select s.Id from {source} s where {probe.Sql}";
        var provider = new SourcePlanningSchemaProvider(mode,
            new Dictionary<string, IReadOnlyList<SourcePlanningEntity>> { ["#single"] = Rows });
        var actual = CreateAndRunVirtualMachine(query, schemaProvider: provider).Run()
            .Select(row => (int)row.ReadValue(0)!).Order().ToArray();
        var expected = Rows.Where(probe.Accepts).Select(row => row.Id).Order().ToArray();
        CollectionAssert.AreEqual(expected, actual, $"{probe.Id}; {mode}; {query}");
    }
}
