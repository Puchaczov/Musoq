using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC092MeaningPreservationTests : BasicEntityTestBase
{
    [TestMethod]
    [DynamicData(nameof(MeaningChangingRepairCases))]
    public void MeaningChangingRepairs_ShouldBeRejectedByDiscriminatingFixtures(SemanticRepairCase candidate)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(candidate.CaseId));

        var gold = Execute(candidate.GoldQuery, candidate);
        var repaired = Execute(candidate.RepairedQuery, candidate);

        AssertSnapshot(gold, candidate.GoldRows, candidate.ExpectedColumns, candidate.Ordered, candidate.CaseId + " gold");
        AssertSnapshot(repaired, candidate.RepairedRows, candidate.ExpectedColumns, candidate.Ordered,
            candidate.CaseId + " deliberately meaning-changing repair");

        Assert.IsFalse(
            SemanticallyEqual(gold, repaired, candidate.Ordered),
            $"{candidate.CaseId}: a compiling and running meaning-changing repair was accepted as equivalent.");
    }

    [TestMethod]
    public void CandidateMatrix_ShouldContainTwelveCasesPerRepairSensitiveFamily()
    {
        Assert.HasCount(48, CandidateCases);

        var familyCounts = CandidateCases
            .GroupBy(static candidate => candidate.Family)
            .ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(
            new[] { "dropped-filter", "added-distinct", "changed-join", "replaced-constant" },
            familyCounts.Keys.ToArray());
        Assert.IsTrue(familyCounts.Values.All(static count => count == 12));
        Assert.HasCount(48, CandidateCases.Select(static candidate => candidate.CaseId).Distinct(StringComparer.Ordinal));
    }

    [TestMethod]
    public void StrictCast_ShouldNotBeSilentlySoftenedOnInvalidInput()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "not-a-number" },
                new BasicEntity { Id = 2, Name = "7" }
            ]
        };

        var strict = CreateAndRunVirtualMachine("select Name::int as Result from #A.Entities()", sources);
        Assert.Throws<QueryExecutionException>(() => _ = strict.Run(TestContext.CancellationToken).Count);

        var soft = CreateAndRunVirtualMachine("select ToInt32(Name) as Result from #A.Entities()", sources);
        var softTable = TableMaterializationTestHelper.Materialize(soft.Run(TestContext.CancellationToken));
        TableMaterializationTestHelper.AssertColumns(softTable, ("Result", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(softTable, new object?[] { null }, [7]);
    }

    [TestMethod]
    public void StrictCastAndSoftConversion_ShouldAgreeOnlyForValidInput()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "7" },
                new BasicEntity { Id = 2, Name = "8" }
            ]
        };

        var strict = CreateAndRunVirtualMachine("select Name::int as Result from #A.Entities()", sources);
        var soft = CreateAndRunVirtualMachine("select ToInt32(Name) as Result from #A.Entities()", sources);

        var strictTable = TableMaterializationTestHelper.Materialize(strict.Run(TestContext.CancellationToken));
        var softTable = TableMaterializationTestHelper.Materialize(soft.Run(TestContext.CancellationToken));
        TableMaterializationTestHelper.AssertColumns(strictTable, ("Result", typeof(int?)));
        TableMaterializationTestHelper.AssertColumns(softTable, ("Result", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(strictTable, [7], [8]);
        TableMaterializationTestHelper.AssertRowsInOrder(softTable, [7], [8]);
    }

    [TestMethod]
    public void Parse_ShouldNotBeSilentlyRewrittenToTryParseOnInvalidInput()
    {
        var sources = CreateParseSources("1:valid", "malformed");
        const string declaration = "text Pair { Key: until ':', Value: rest trim };";
        var strict = CreateAndRunVirtualMachine(
            declaration + " select a.Id, p.Key from #A.Entities() a cross apply Parse<Pair>(a.Name) p",
            sources);
        Assert.Throws<ParseException>(() => _ = strict.Run(TestContext.CancellationToken).Count);

        var soft = CreateAndRunVirtualMachine(
            declaration + " select a.Id, p.Key from #A.Entities() a outer apply TryParse<Pair>(a.Name) p",
            sources);
        var softTable = TableMaterializationTestHelper.Materialize(soft.Run(TestContext.CancellationToken));
        TableMaterializationTestHelper.AssertColumns(
            softTable,
            ("a.Id", typeof(int)),
            ("p.Key", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            softTable,
            new object?[] { 1, "1" },
            new object?[] { 2, null });
    }

    [TestMethod]
    public void ParseAndTryParse_ShouldAgreeOnlyForValidInput()
    {
        var sources = CreateParseSources("1:valid", "2:also-valid");
        const string declaration = "text Pair { Key: until ':', Value: rest trim };";
        var strict = CreateAndRunVirtualMachine(
            declaration + " select a.Id, p.Key from #A.Entities() a cross apply Parse<Pair>(a.Name) p",
            sources);
        var soft = CreateAndRunVirtualMachine(
            declaration + " select a.Id, p.Key from #A.Entities() a outer apply TryParse<Pair>(a.Name) p",
            sources);

        var strictTable = TableMaterializationTestHelper.Materialize(strict.Run(TestContext.CancellationToken));
        var softTable = TableMaterializationTestHelper.Materialize(soft.Run(TestContext.CancellationToken));
        TableMaterializationTestHelper.AssertColumns(strictTable, ("a.Id", typeof(int)), ("p.Key", typeof(string)));
        TableMaterializationTestHelper.AssertColumns(softTable, ("a.Id", typeof(int)), ("p.Key", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            strictTable,
            new object?[] { 1, "1" },
            new object?[] { 2, "2" });
        TableMaterializationTestHelper.AssertRowsInOrder(
            softTable,
            new object?[] { 1, "1" },
            new object?[] { 2, "2" });
    }

    public static IEnumerable<object[]> MeaningChangingRepairCases()
    {
        foreach (var candidate in CandidateCases)
            yield return [candidate];
    }

    private static readonly ExpectedColumn[] TextResult = Columns(("Result", typeof(string)));
    private static readonly ExpectedColumn[] IntResult = Columns(("Result", typeof(int)));
    private static readonly ExpectedColumn[] DecimalResult = Columns(("Result", typeof(decimal)));
    private static readonly ExpectedColumn[] JoinResult = Columns(("LeftName", typeof(string)), ("RightName", typeof(string)));

    private static readonly IReadOnlyList<SemanticRepairCase> CandidateCases =
    [
        // Dropped filters: the gold query selects a discriminating subset; the repair drops or weakens its predicate.
        new("REC-092-F01", "dropped-filter",
            "select Name as Result from #A.Entities() where Country = 'PL'",
            "select Name as Result from #A.Entities()",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A1"], ["A2"]), Rows(["A1"], ["A2"], ["A3"], ["A4"])),
        new("REC-092-F02", "dropped-filter",
            "select Name as Result from #A.Entities() where City is null",
            "select Name as Result from #A.Entities() where City = null",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A4"]), Rows()),
        new("REC-092-F03", "dropped-filter",
            "select Name as Result from #A.Entities() where NullableValue is null",
            "select Name as Result from #A.Entities() where NullableValue = null",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A1"], ["A3"]), Rows()),
        new("REC-092-F04", "dropped-filter",
            "param(min: decimal) select Name as Result from #A.Entities() where Population > $min",
            "param(min: decimal) select Name as Result from #A.Entities() where Population >= $min",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A3"], ["A4"]), Rows(["A2"], ["A3"], ["A4"]), "min", 20m),
        new("REC-092-F05", "dropped-filter",
            "select Name as Result from #A.Entities() where Country = 'PL' and Population >= 20",
            "select Name as Result from #A.Entities() where Country = 'PL'",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A2"]), Rows(["A1"], ["A2"])),
        new("REC-092-F06", "dropped-filter",
            "select Name as Result from #A.Entities() where Country = 'PL' or City = 'Berlin'",
            "select Name as Result from #A.Entities() where Country = 'PL' and City = 'Berlin'",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A1"], ["A2"], ["A3"]), Rows()),
        new("REC-092-F07", "dropped-filter",
            "select Name as Result from #A.Entities() where not (Country = 'PL')",
            "select Name as Result from #A.Entities() where Country = 'PL'",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A3"]), Rows(["A1"], ["A2"])),
        new("REC-092-F08", "dropped-filter",
            "select Name as Result from #A.Entities() where Population between 20 and 30",
            "select Name as Result from #A.Entities() where Population > 20 and Population < 30",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A2"], ["A3"]), Rows()),
        new("REC-092-F09", "dropped-filter",
            "select Name as Result from #A.Entities() where Id in (1, 3)",
            "select Name as Result from #A.Entities() where Id in (1, 2)",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A1"], ["A3"]), Rows(["A1"], ["A2"])),
        new("REC-092-F10", "dropped-filter",
            "select Name as Result from #A.Entities() where Id not in (1, 2)",
            "select Name as Result from #A.Entities() where Id not in (1, 3)",
            FixtureKind.FilterRows, false, TextResult,
            Rows(["A3"], ["A4"]), Rows(["A2"], ["A4"])),
        new("REC-092-F11", "dropped-filter",
            "select Name as Result from #A.Entities() where Country = 'PL'",
            "select Name as Result from #A.Entities()",
            FixtureKind.FilterRowsWithDuplicates, false, TextResult,
            Rows(["D1"], ["D1"], ["D3"]), Rows(["D1"], ["D1"], ["D2"], ["D3"])),
        new("REC-092-F12", "dropped-filter",
            "select Name as Result from #A.Entities() where Country = 'PL' order by Id desc",
            "select Name as Result from #A.Entities() order by Id desc",
            FixtureKind.FilterRows, true, TextResult,
            Rows(["A2"], ["A1"]), Rows(["A4"], ["A3"], ["A2"], ["A1"])),

        // Added DISTINCT: every repaired query compiles, but removes a duplicate that the gold query preserves.
        new("REC-092-D01", "added-distinct",
            "select Name as Result from #A.Entities()",
            "select distinct Name as Result from #A.Entities()",
            FixtureKind.DistinctRows, false, TextResult,
            Rows(["alpha"], ["alpha"], ["beta"], new object?[] { null }, new object?[] { null }),
            Rows(["alpha"], ["beta"], new object?[] { null })),
        new("REC-092-D02", "added-distinct",
            "select City as Result from #A.Entities()",
            "select distinct City as Result from #A.Entities()",
            FixtureKind.DistinctRows, false, TextResult,
            Rows(["X"], ["X"], ["Y"], ["Z"], ["Z"]),
            Rows(["X"], ["Y"], ["Z"])),
        new("REC-092-D03", "added-distinct",
            "select Name as Result from #A.Entities() where City = 'X'",
            "select distinct Name as Result from #A.Entities() where City = 'X'",
            FixtureKind.DistinctRows, false, TextResult,
            Rows(["alpha"], ["alpha"]), Rows(["alpha"])),
        new("REC-092-D04", "added-distinct",
            "select Name as Result from #A.Entities() where Name is null",
            "select distinct Name as Result from #A.Entities() where Name is null",
            FixtureKind.DistinctRows, false, TextResult,
            Rows(new object?[] { null }, new object?[] { null }), Rows(new object?[] { null })),
        new("REC-092-D05", "added-distinct",
            "select Name as Result from #A.Entities() where Id >= 2",
            "select distinct Name as Result from #A.Entities() where Id >= 2",
            FixtureKind.DistinctRows, false, TextResult,
            Rows(["alpha"], ["beta"], new object?[] { null }, new object?[] { null }),
            Rows(["alpha"], ["beta"], new object?[] { null })),
        new("REC-092-D06", "added-distinct",
            "select Name as Result from #A.Entities()",
            "select distinct Name as Result from #A.Entities()",
            FixtureKind.DistinctRowsAlt, false, TextResult,
            Rows(["red"], ["red"], ["blue"], ["red"]), Rows(["red"], ["blue"])),
        new("REC-092-D07", "added-distinct",
            "select Name as Result from #A.Entities() union all select Name as Result from #B.Entities()",
            "select Name as Result from #A.Entities() union select Name as Result from #B.Entities()",
            FixtureKind.DistinctRows, false, TextResult,
            Rows(["alpha"], ["alpha"], ["beta"], new object?[] { null }, new object?[] { null }, ["alpha"], ["gamma"]),
            Rows(["alpha"], ["beta"], new object?[] { null }, ["gamma"])),
        new("REC-092-D08", "added-distinct",
            "select Name as Result from #A.Entities() where Name is not null union all select Name as Result from #B.Entities() where Name is not null",
            "select Name as Result from #A.Entities() where Name is not null union select Name as Result from #B.Entities() where Name is not null",
            FixtureKind.DistinctRows, false, TextResult,
            Rows(["alpha"], ["alpha"], ["beta"], ["alpha"], ["gamma"]),
            Rows(["alpha"], ["beta"], ["gamma"])),
        new("REC-092-D09", "added-distinct",
            "select Name as Result from #A.Entities() union all (Name) select Name as Result from #B.Entities()",
            "select Name as Result from #A.Entities() union (Name) select Name as Result from #B.Entities()",
            FixtureKind.DistinctRows, false, TextResult,
            Rows(["alpha"], ["alpha"], ["beta"], new object?[] { null }, new object?[] { null }, ["alpha"], ["gamma"]),
            Rows(["alpha"], ["beta"], new object?[] { null }, ["gamma"])),
        new("REC-092-D10", "added-distinct",
            "select Name as ResultName, City as ResultCity from #A.Entities()",
            "select distinct Name as ResultName, City as ResultCity from #A.Entities()",
            FixtureKind.DistinctRows, false, Columns(("ResultName", typeof(string)), ("ResultCity", typeof(string))),
            Rows(["alpha", "X"], ["alpha", "X"], ["beta", "Y"], [null, "Z"], [null, "Z"]),
            Rows(["alpha", "X"], ["beta", "Y"], [null, "Z"])),
        new("REC-092-D11", "added-distinct",
            "select Name as Result from #A.Entities() where City = 'Z'",
            "select distinct Name as Result from #A.Entities() where City = 'Z'",
            FixtureKind.DistinctRows, false, TextResult,
            Rows(new object?[] { null }, new object?[] { null }), Rows(new object?[] { null })),
        new("REC-092-D12", "added-distinct",
            "select Name as Result from #A.Entities() where Id >= 1",
            "select distinct Name as Result from #A.Entities() where Id >= 1",
            FixtureKind.DistinctRowsAlt, false, TextResult,
            Rows(["red"], ["red"], ["blue"], ["red"]), Rows(["red"], ["blue"])),

        // Changed joins: source-side multiplicity and NULL extension make the join choice observable.
        new("REC-092-J01", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Country = b.Country",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A1", "B1"], ["A2", "B2"]),
            Rows(["A1", "B1"], ["A1", "B5"], ["A2", "B1"], ["A2", "B5"], ["A3", "B2"])),
        new("REC-092-J02", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.City = b.City",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A1", "B1"], ["A2", "B2"]),
            Rows(["A1", "B1"], ["A1", "B5"], ["A2", "B2"], ["A3", "B1"], ["A3", "B5"], ["A4", "B6"])),
        new("REC-092-J03", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a left join #B.Entities() b on a.Id = b.Id",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A1", "B1"], ["A2", "B2"], ["A3", null], ["A4", null]),
            Rows(["A1", "B1"], ["A2", "B2"])),
        new("REC-092-J04", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a left join #B.Entities() b on a.Country = b.Country",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a left join #B.Entities() b on a.Id = b.Id",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A1", "B1"], ["A1", "B5"], ["A2", "B1"], ["A2", "B5"], ["A3", "B2"], ["A4", null]),
            Rows(["A1", "B1"], ["A2", "B2"], ["A3", null], ["A4", null])),
        new("REC-092-J05", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id and b.NullableValue is not null",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A2", "B2"]), Rows(["A1", "B1"], ["A2", "B2"])),
        new("REC-092-J06", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id or a.Id = 4",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A1", "B1"], ["A2", "B2"]),
            Rows(["A1", "B1"], ["A2", "B2"], ["A4", "B1"], ["A4", "B2"], ["A4", "B5"], ["A4", "B6"])),
        new("REC-092-J07", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a cross join #B.Entities() b where a.Id = b.Id",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a cross join #B.Entities() b",
            FixtureKind.SmallJoinRows, false, JoinResult,
            Rows(["A1", "B1"]), Rows(["A1", "B1"], ["A1", "B3"], ["A2", "B1"], ["A2", "B3"])),
        new("REC-092-J08", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Country = b.Country",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Country is not distinct from b.Country",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A1", "B1"], ["A1", "B5"], ["A2", "B1"], ["A2", "B5"], ["A3", "B2"]),
            Rows(["A1", "B1"], ["A1", "B5"], ["A2", "B1"], ["A2", "B5"], ["A3", "B2"], ["A4", "B6"])),
        new("REC-092-J09", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a left join #B.Entities() b on a.Id = b.Id and b.NullableValue is not null",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.NullableValue is not null",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A1", null], ["A2", "B2"], ["A3", null], ["A4", null]),
            Rows(["A2", "B2"])),
        new("REC-092-J10", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id + 1",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A1", "B1"], ["A2", "B2"]), Rows(["A2", "B1"], ["A3", "B2"])),
        new("REC-092-J11", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b is missing",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b is present",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A3", null], ["A4", null]), Rows(["A1", "B1"], ["A2", "B2"])),
        new("REC-092-J12", "changed-join",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a full outer join #B.Entities() b on a.Id = b.Id",
            "select a.Name as LeftName, b.Name as RightName from #A.Entities() a left join #B.Entities() b on a.Id = b.Id",
            FixtureKind.JoinRows, false, JoinResult,
            Rows(["A1", "B1"], ["A2", "B2"], ["A3", null], ["A4", null], [null, "B5"], [null, "B6"]),
            Rows(["A1", "B1"], ["A2", "B2"], ["A3", null], ["A4", null])),

        // Replaced constants: boundaries, NULL predicates, ordering and parameter values all change the task.
        new("REC-092-C01", "replaced-constant",
            "select Name as Result from #A.Entities() where Id = 2",
            "select Name as Result from #A.Entities() where Id = 3",
            FixtureKind.ConstantRows, false, TextResult, Rows(["two"]), Rows(["three"])),
        new("REC-092-C02", "replaced-constant",
            "select Name as Result from #A.Entities() where Population > 20",
            "select Name as Result from #A.Entities() where Population >= 20",
            FixtureKind.ConstantRowsAlt, false, TextResult,
            Rows(["tres"], ["cuatro"]), Rows(["dos"], ["tres"], ["cuatro"])),
        new("REC-092-C03", "replaced-constant",
            "select Id + 10 as Result from #A.Entities()",
            "select Id + 100 as Result from #A.Entities()",
            FixtureKind.ConstantRows, false, IntResult, Rows([11], [12], [13], [14]), Rows([101], [102], [103], [104])),
        new("REC-092-C04", "replaced-constant",
            "select Money + 1 as Result from #A.Entities()",
            "select Money + 10 as Result from #A.Entities()",
            FixtureKind.ConstantRowsAlt, false, DecimalResult,
            Rows([12m], [23m], [34m], [45m]), Rows([21m], [32m], [43m], [54m])),
        new("REC-092-C05", "replaced-constant",
            "select Name as Result from #A.Entities() order by Id desc skip 1 take 2",
            "select Name as Result from #A.Entities() order by Id desc skip 2 take 2",
            FixtureKind.ConstantRows, true, TextResult,
            Rows(["three"], ["two"]), Rows(["two"], ["one"])),
        new("REC-092-C06", "replaced-constant",
            "select Name as Result from #A.Entities() order by Id desc take 2",
            "select Name as Result from #A.Entities() order by Id desc take 3",
            FixtureKind.ConstantRows, true, TextResult,
            Rows(["four"], ["three"]), Rows(["four"], ["three"], ["two"])),
        new("REC-092-C07", "replaced-constant",
            "select Name as Result from #A.Entities() where NullableValue is null",
            "select Name as Result from #A.Entities() where NullableValue is not null",
            FixtureKind.ConstantRows, false, TextResult,
            Rows(["one"], ["four"]), Rows(["two"], ["three"])),
        new("REC-092-C08", "replaced-constant",
            "select Name as Result from #A.Entities() where City in ('A', 'C')",
            "select Name as Result from #A.Entities() where City in ('A', 'D')",
            FixtureKind.ConstantRows, false, TextResult,
            Rows(["one"], ["three"]), Rows(["one"], ["four"])),
        new("REC-092-C09", "replaced-constant",
            "select case when Id <= 2 then 'low' else 'high' end as Result from #A.Entities()",
            "select case when Id < 2 then 'low' else 'high' end as Result from #A.Entities()",
            FixtureKind.ConstantRows, false, TextResult,
            Rows(["low"], ["low"], ["high"], ["high"]), Rows(["low"], ["high"], ["high"], ["high"])),
        new("REC-092-C10", "replaced-constant",
            "select Name as Result from #A.Entities() where Money between 2 and 3",
            "select Name as Result from #A.Entities() where Money > 2 and Money < 3",
            FixtureKind.ConstantRows, false, TextResult,
            Rows(["two"], ["three"]), Rows()),
        new("REC-092-C11", "replaced-constant",
            "param(limit: int) select Name as Result from #A.Entities() where Id <= $limit",
            "param(limit: int) select Name as Result from #A.Entities() where Id < $limit",
            FixtureKind.ConstantRows, false, TextResult,
            Rows(["one"], ["two"], ["three"]), Rows(["one"], ["two"]), "limit", 3),
        new("REC-092-C12", "replaced-constant",
            "select Name as Result from #A.Entities() where (Country ?? 'UNKNOWN') = 'UNKNOWN'",
            "select Name as Result from #A.Entities() where (Country ?? 'UNKNOWN') = 'PL'",
            FixtureKind.ConstantRows, false, TextResult, Rows(["four"]), Rows(["one"]))
    ];

    private QuerySnapshot Execute(string query, SemanticRepairCase candidate)
    {
        var vm = CreateAndRunVirtualMachine(query, CreateSources(candidate.Fixture));
        if (candidate.ParameterName != null)
            vm.Parameters[candidate.ParameterName] = candidate.ParameterValue;

        var table = TableMaterializationTestHelper.Materialize(vm.Run(TestContext.CancellationToken));
        return new QuerySnapshot(
            table.Columns.Select(static column => new ExpectedColumn(column.ColumnName, column.ColumnType)).ToArray(),
            table.Select(static row => row.Values.ToArray()).ToArray());
    }

    private static void AssertSnapshot(
        QuerySnapshot actual,
        object?[][] expectedRows,
        ExpectedColumn[] expectedColumns,
        bool ordered,
        string subject)
    {
        Assert.HasCount(expectedColumns.Length, actual.Columns, subject + ": unexpected column count.");
        for (var index = 0; index < expectedColumns.Length; index++)
        {
            Assert.AreEqual(expectedColumns[index].Name, actual.Columns[index].Name, subject);
            Assert.AreEqual(expectedColumns[index].Type, actual.Columns[index].Type, subject);
        }

        AssertRows(expectedRows, actual.Rows, ordered, subject);
    }

    private static void AssertRows(object?[][] expected, object?[][] actual, bool ordered, string subject)
    {
        Assert.AreEqual(expected.Length, actual.Length, subject + ": unexpected row count.");

        if (ordered)
        {
            for (var index = 0; index < expected.Length; index++)
                Assert.IsTrue(RowEquals(expected[index], actual[index]), subject + $": unexpected row {index}.");
            return;
        }

        var remaining = actual.ToList();
        foreach (var expectedRow in expected)
        {
            var match = remaining.FindIndex(actualRow => RowEquals(expectedRow, actualRow));
            Assert.IsGreaterThanOrEqualTo(match, 0, subject + ": expected bag row was not found.");
            remaining.RemoveAt(match);
        }
    }

    private static bool SemanticallyEqual(QuerySnapshot left, QuerySnapshot right, bool ordered)
    {
        if (left.Columns.Length != right.Columns.Length ||
            !left.Columns.Zip(right.Columns).All(static pair =>
                pair.First.Name == pair.Second.Name && pair.First.Type == pair.Second.Type))
            return false;

        if (left.Rows.Length != right.Rows.Length)
            return false;

        if (ordered)
            return left.Rows.Zip(right.Rows).All(static pair => RowEquals(pair.First, pair.Second));

        var remaining = right.Rows.ToList();
        foreach (var row in left.Rows)
        {
            var match = remaining.FindIndex(candidate => RowEquals(row, candidate));
            if (match < 0)
                return false;
            remaining.RemoveAt(match);
        }

        return remaining.Count == 0;
    }

    private static bool RowEquals(object?[] left, object?[] right)
    {
        return left.Length == right.Length && left.Zip(right).All(static pair => ValueEquals(pair.First, pair.Second));
    }

    private static bool ValueEquals(object? left, object? right)
    {
        if (left is null || right is null)
            return left is null && right is null;

        return left.Equals(right);
    }

    private static ExpectedColumn[] Columns(params (string Name, Type Type)[] columns) =>
        columns.Select(static column => new ExpectedColumn(column.Name, column.Type)).ToArray();

    private static object?[][] Rows(params object?[][] rows) => rows;

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateSources(FixtureKind fixture) => fixture switch
    {
        FixtureKind.FilterRows => new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "A1", Country = "PL", City = "Warsaw", Population = 10m, Money = 100m, NullableValue = null },
                new BasicEntity { Id = 2, Name = "A2", Country = "PL", City = "Warsaw", Population = 20m, Money = 200m, NullableValue = 0 },
                new BasicEntity { Id = 3, Name = "A3", Country = "DE", City = "Berlin", Population = 30m, Money = 300m, NullableValue = null },
                new BasicEntity { Id = 4, Name = "A4", Country = null, City = null, Population = 40m, Money = 400m, NullableValue = 5 }
            ]
        },
        FixtureKind.FilterRowsWithDuplicates => new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "D1", Country = "PL" },
                new BasicEntity { Id = 2, Name = "D1", Country = "PL" },
                new BasicEntity { Id = 3, Name = "D2", Country = "DE" },
                new BasicEntity { Id = 4, Name = "D3", Country = "PL" }
            ]
        },
        FixtureKind.DistinctRows => new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "alpha", City = "X" },
                new BasicEntity { Id = 2, Name = "alpha", City = "X" },
                new BasicEntity { Id = 3, Name = "beta", City = "Y" },
                new BasicEntity { Id = 4, Name = null, City = "Z" },
                new BasicEntity { Id = 5, Name = null, City = "Z" }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 6, Name = "alpha", City = "X" },
                new BasicEntity { Id = 7, Name = "gamma", City = "W" }
            ]
        },
        FixtureKind.DistinctRowsAlt => new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "red", City = "A" },
                new BasicEntity { Id = 2, Name = "red", City = "B" },
                new BasicEntity { Id = 3, Name = "blue", City = "C" },
                new BasicEntity { Id = 4, Name = "red", City = "D" }
            ]
        },
        FixtureKind.JoinRows => new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "A1", Country = "PL", City = "X" },
                new BasicEntity { Id = 2, Name = "A2", Country = "PL", City = "Y" },
                new BasicEntity { Id = 3, Name = "A3", Country = "DE", City = "X" },
                new BasicEntity { Id = 4, Name = "A4", Country = null, City = "Z" }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 1, Name = "B1", Country = "PL", City = "X", NullableValue = null },
                new BasicEntity { Id = 2, Name = "B2", Country = "DE", City = "Y", NullableValue = 7 },
                new BasicEntity { Id = 5, Name = "B5", Country = "PL", City = "X", NullableValue = null },
                new BasicEntity { Id = 6, Name = "B6", Country = null, City = "Z", NullableValue = 0 }
            ]
        },
        FixtureKind.SmallJoinRows => new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "A1" },
                new BasicEntity { Id = 2, Name = "A2" }
            ],
            ["#B"] =
            [
                new BasicEntity { Id = 1, Name = "B1" },
                new BasicEntity { Id = 3, Name = "B3" }
            ]
        },
        FixtureKind.ConstantRows => new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "one", Country = "PL", City = "A", Population = 10m, Money = 1m, NullableValue = null },
                new BasicEntity { Id = 2, Name = "two", Country = "DE", City = "B", Population = 20m, Money = 2m, NullableValue = 0 },
                new BasicEntity { Id = 3, Name = "three", Country = "US", City = "C", Population = 30m, Money = 3m, NullableValue = 1 },
                new BasicEntity { Id = 4, Name = "four", Country = null, City = "D", Population = 40m, Money = 4m, NullableValue = null }
            ]
        },
        FixtureKind.ConstantRowsAlt => new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = "uno", Country = "ES", City = "Q", Population = 10m, Money = 11m, NullableValue = null },
                new BasicEntity { Id = 2, Name = "dos", Country = "ES", City = "R", Population = 20m, Money = 22m, NullableValue = 0 },
                new BasicEntity { Id = 3, Name = "tres", Country = "IT", City = "S", Population = 30m, Money = 33m, NullableValue = 1 },
                new BasicEntity { Id = 4, Name = "cuatro", Country = null, City = "T", Population = 40m, Money = 44m, NullableValue = null }
            ]
        },
        _ => throw new ArgumentOutOfRangeException(nameof(fixture), fixture, null)
    };

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateParseSources(string first, string second) =>
        new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity { Id = 1, Name = first },
                new BasicEntity { Id = 2, Name = second }
            ]
        };

    public enum FixtureKind
    {
        FilterRows,
        FilterRowsWithDuplicates,
        DistinctRows,
        DistinctRowsAlt,
        JoinRows,
        SmallJoinRows,
        ConstantRows,
        ConstantRowsAlt
    }

    public sealed record ExpectedColumn(string Name, Type Type);

    private sealed record QuerySnapshot(ExpectedColumn[] Columns, object?[][] Rows);

    public sealed record SemanticRepairCase(
        string CaseId,
        string Family,
        string GoldQuery,
        string RepairedQuery,
        FixtureKind Fixture,
        bool Ordered,
        ExpectedColumn[] ExpectedColumns,
        object?[][] GoldRows,
        object?[][] RepairedRows,
        string? ParameterName = null,
        object? ParameterValue = null);
}
