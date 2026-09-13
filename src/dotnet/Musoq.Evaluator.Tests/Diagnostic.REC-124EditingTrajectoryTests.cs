using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC124EditingTrajectoryTests
{
    private static readonly IReadOnlyList<RecoveryCase> CandidateCases =
    [
        Sequence("A01", "SOURCE_ALIAS_TRAJECTORY", "select a.Name from #A.Entities() a where a.City = 'WARSAW'", "a.Name", "b.Name", "MQ3015_UnknownAlias", "b", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "a stale qualified projection alias is corrected before a predicate column changes"),
        Sequence("A02", "SOURCE_ALIAS_TRAJECTORY", "select a.Name from #A.Entities() a order by a.City", "a.City", "b.City", "MQ3015_UnknownAlias", "b", "a.Name", "a.Nmae", "MQ3001_UnknownColumn", "Nmae", "an order key alias failure is corrected before a projection column changes"),
        Sequence("A03", "SOURCE_ALIAS_TRAJECTORY", "select Count(*) from #A.Entities() a group by a.City", "a.City", "b.City", "MQ3015_UnknownAlias", "b", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "the grouped source alias is repaired before its grouping field changes"),
        Sequence("A04", "SOURCE_ALIAS_TRAJECTORY", "select a.Name from #A.Entities() a where a.Population > 0 order by a.City", "a.Population", "b.Population", "MQ3015_UnknownAlias", "b", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "a predicate source alias failure is corrected before the order key changes"),
        Sequence("A05", "SOURCE_ALIAS_TRAJECTORY", "select a.Name, a.City from #A.Entities() a", "a.Name", "b.Name", "MQ3015_UnknownAlias", "b", "a.City", "a.Unknown", "MQ3001_UnknownColumn", "Unknown", "one projection alias root is corrected before its sibling column changes"),
        Sequence("A06", "SOURCE_ALIAS_TRAJECTORY", "select a.Country from #A.Entities() a where a.City = 'WARSAW' order by a.Name", "a.Country", "b.Country", "MQ3015_UnknownAlias", "b", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "a source-qualified select root is corrected before a predicate root changes"),
        Sequence("B01", "PROJECTION_ALIAS_TRAJECTORY", "select a.City as CityOut from #A.Entities() a where CityOut = 'WARSAW'", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "a stale WHERE consumer follows a projection alias rename"),
        Sequence("B02", "PROJECTION_ALIAS_TRAJECTORY", "select a.Country as CountryOut from #A.Entities() a group by CountryOut", "as CountryOut", "as RegionOut", "MQ3001_UnknownColumn", "CountryOut", "a.Country", "a.Counrty", "MQ3001_UnknownColumn", "Counrty", "a grouped projection alias is repaired before the source expression changes"),
        Sequence("B03", "PROJECTION_ALIAS_TRAJECTORY", "select Count(*) as CountOut from #A.Entities() a group by a.Country having CountOut > 0", "as CountOut", "as TotalOut", "MQ3001_UnknownColumn", "CountOut", "a.Country", "a.Counrty", "MQ3001_UnknownColumn", "Counrty", "an aggregate HAVING alias is repaired before its grouping source changes"),
        Sequence("B04", "PROJECTION_ALIAS_TRAJECTORY", "select a.City as CityOut from #A.Entities() a order by CityOut", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "an ORDER BY projection alias is repaired before its source expression changes"),
        Sequence("B05", "PROJECTION_ALIAS_TRAJECTORY", "select a.City as CityOut, a.Name from #A.Entities() a where CityOut = 'WARSAW'", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "a.Name", "a.Nmae", "MQ3001_UnknownColumn", "Nmae", "a projection alias and a sibling field are corrected in sequence"),
        Sequence("B06", "PROJECTION_ALIAS_TRAJECTORY", "select RowNumber() over (order by a.City) as Position, a.Name as NameOut from #A.Entities() a order by Position", "as Position", "as RankOut", "MQ3001_UnknownColumn", "Position", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "a window output alias is repaired before its order expression changes"),
        Sequence("C01", "CTE_OUTPUT_TRAJECTORY", "with p as (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) select p.CityOut, p.NameOut from p", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "two exported CTE outputs are renamed one declaration at a time"),
        Sequence("C02", "CTE_OUTPUT_TRAJECTORY", "with p as (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) select p.CityOut from p where p.NameOut = 'WARSAW'", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "a CTE output consumed by a predicate is corrected before a second export changes"),
        Sequence("C03", "CTE_OUTPUT_TRAJECTORY", "with p as (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) select p.CityOut from p order by p.NameOut", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "a CTE ORDER BY consumer observes successive output corrections"),
        Sequence("C04", "CTE_OUTPUT_TRAJECTORY", "with p as (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) select p.CityOut from p where p.NameOut = 'BERLIN'", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "a CTE output consumer follows two output declaration edits"),
        Sequence("C05", "CTE_OUTPUT_TRAJECTORY", "with p as (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) select p.CityOut from p inner join #B.Entities() b on p.NameOut = b.Name", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "a CTE JOIN consumer tracks successive exported-name changes"),
        Sequence("C06", "CTE_OUTPUT_TRAJECTORY", "with p as (select a.City as CityOut, a.Name as NameOut from #A.Entities() a), q as (select p.CityOut, p.NameOut from p) select q.CityOut, q.NameOut from q", "q.CityOut", "q.TownOut", "MQ3001_UnknownColumn", "TownOut", "q.NameOut", "q.NmaeOut", "MQ3001_UnknownColumn", "NmaeOut", "nested CTE exports retain the current failing block after each consumer rename"),
        Sequence("D01", "DERIVED_OUTPUT_TRAJECTORY", "select d.CityOut, d.NameOut from (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) d", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "a derived-table output consumer follows two inner output edits"),
        Sequence("D02", "DERIVED_OUTPUT_TRAJECTORY", "select d.CityOut from (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) d where d.NameOut = 'WARSAW'", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "a derived WHERE consumer refreshes after each output rename"),
        Sequence("D03", "DERIVED_OUTPUT_TRAJECTORY", "select d.CityOut from (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) d order by d.NameOut", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "a derived ORDER BY consumer refreshes its exported output location"),
        Sequence("D04", "DERIVED_OUTPUT_TRAJECTORY", "select d.CityOut from (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) d where d.NameOut = 'BERLIN'", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "a derived output consumer tracks two active output declarations"),
        Sequence("D05", "DERIVED_OUTPUT_TRAJECTORY", "select d.CityOut from (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) d inner join #B.Entities() b on d.NameOut = b.Name", "as CityOut", "as TownOut", "MQ3001_UnknownColumn", "CityOut", "as NameOut", "as NmaeOut", "MQ3001_UnknownColumn", "NameOut", "a derived JOIN consumer tracks successive output changes"),
        Sequence("D06", "DERIVED_OUTPUT_TRAJECTORY", "select x.CityOut, x.NameOut from (select d.CityOut, d.NameOut from (select a.City as CityOut, a.Name as NameOut from #A.Entities() a) d) x", "x.CityOut", "x.TownOut", "MQ3001_UnknownColumn", "TownOut", "x.NameOut", "x.NmaeOut", "MQ3001_UnknownColumn", "NmaeOut", "nested derived outputs preserve the current consumer boundary"),
        Sequence("E01", "NESTED_SHADOWING_TRAJECTORY", "select a.Name from #A.Entities() a where exists (select b.Name from #B.Entities() b where b.Country = a.Country)", "b.Country", "c.Country", "MQ3015_UnknownAlias", "c", "a.Name", "a.Nmae", "MQ3001_UnknownColumn", "Nmae", "an inner shadowed alias is corrected before an outer projection changes"),
        Sequence("E02", "NESTED_SHADOWING_TRAJECTORY", "select a.Name from #A.Entities() a where a.City = 'WARSAW' and exists (select b.Name from #B.Entities() b where b.Country = a.Country)", "b.Country", "c.Country", "MQ3015_UnknownAlias", "c", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "an inner alias repair precedes an outer predicate repair"),
        Sequence("E03", "NESTED_SHADOWING_TRAJECTORY", "with p as (select a.Name, a.City from #A.Entities() a) select p.Name from p where exists (select 1 from #B.Entities() b where b.City = p.City)", "b.City", "c.City", "MQ3015_UnknownAlias", "c", "p.Name", "p.Nmae", "MQ3001_UnknownColumn", "Nmae", "a nested CTE consumer keeps inner and outer scope faults separate"),
        Sequence("E04", "NESTED_SHADOWING_TRAJECTORY", "select d.Name from (select a.Name, a.City from #A.Entities() a) d where exists (select 1 from #B.Entities() b where b.City = d.City)", "b.City", "c.City", "MQ3015_UnknownAlias", "c", "d.Name", "d.Nmae", "MQ3001_UnknownColumn", "Nmae", "a nested derived consumer keeps the repaired inner scope visible"),
        Sequence("E05", "NESTED_SHADOWING_TRAJECTORY", "with p as (select a.Name, a.City from #A.Entities() a), q as (select p.Name, p.City from p) select q.Name from q where q.City = 'WARSAW'", "q.City", "r.City", "MQ3015_UnknownAlias", "r", "q.Name", "q.Nmae", "MQ3001_UnknownColumn", "Nmae", "a nested exported alias is repaired before its sibling output changes"),
        Sequence("E06", "NESTED_SHADOWING_TRAJECTORY", "select a.Name from #A.Entities() a where exists (select b.Name from #B.Entities() b where b.Id = a.Id and b.City = 'BERLIN')", "b.Id", "c.Id", "MQ3015_UnknownAlias", "c", "a.Name", "a.Nmae", "MQ3001_UnknownColumn", "Nmae", "a correlated nested alias is repaired before the outer result field changes"),
        Sequence("F01", "JOIN_SCOPE_TRAJECTORY", "select a.Name, b.City from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id", "b.City", "c.City", "MQ3015_UnknownAlias", "c", "a.Id", "a.MissingId", "MQ3001_UnknownColumn", "MissingId", "a JOIN-side alias reference is corrected before a left key changes"),
        Sequence("F02", "JOIN_SCOPE_TRAJECTORY", "select a.Name, b.City from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id where a.City = 'WARSAW'", "a.Name", "c.Name", "MQ3015_UnknownAlias", "c", "b.Id", "b.MissingId", "MQ3001_UnknownColumn", "MissingId", "a left projection alias is corrected before a right join key changes"),
        Sequence("F03", "JOIN_SCOPE_TRAJECTORY", "select a.Name, b.City from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id order by b.City", "order by b.City", "order by c.City", "MQ3015_UnknownAlias", "c", "a.Id", "a.MissingId", "MQ3001_UnknownColumn", "MissingId", "a JOIN ORDER BY alias is corrected before a join key changes"),
        Sequence("F04", "JOIN_SCOPE_TRAJECTORY", "select a.Country, Count(*) from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id group by a.Country", "b.Id", "c.Id", "MQ3015_UnknownAlias", "c", "a.Id", "a.MissingId", "MQ3001_UnknownColumn", "MissingId", "a JOIN declaration reference is corrected before the join key changes"),
        Sequence("F05", "JOIN_SCOPE_TRAJECTORY", "select a.Name, b.City from #A.Entities() a left join #B.Entities() b on a.Id = b.Id where b.Country = 'DE'", "b.Country", "c.Country", "MQ3015_UnknownAlias", "c", "a.Name", "a.Nmae", "MQ3001_UnknownColumn", "Nmae", "a LEFT JOIN predicate alias is corrected before the left projection changes"),
        Sequence("F06", "JOIN_SCOPE_TRAJECTORY", "select a.Name, b.City from #A.Entities() a inner join #B.Entities() b on a.Id = b.Id where b.City = 'BERLIN' order by a.Name", "order by a.Name", "order by c.Name", "MQ3015_UnknownAlias", "c", "where b.City", "where b.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "a JOIN ORDER BY alias is corrected before a right predicate field changes"),
        Sequence("G01", "CALLABLE_TRAJECTORY", "select Substring(a.Name, 0, 2) from #A.Entities() a", "Substring", "Substrng", "MQ3086_UnknownCallable", "Substrng", "a.Name", "a.Nmae", "MQ3001_UnknownColumn", "Nmae", "an unknown callable is corrected before its argument changes"),
        Sequence("G02", "CALLABLE_TRAJECTORY", "select Length(a.Name) from #A.Entities() a", "Length", "Lenght", "MQ3086_UnknownCallable", "Lenght", "a.Name", "a.Nmae", "MQ3001_UnknownColumn", "Nmae", "a unary callable rename is corrected before its argument changes"),
        Sequence("G03", "CALLABLE_TRAJECTORY", "select Substring(a.Name, 0, 2) from #A.Entities() a where a.City = 'WARSAW'", "Substring", "Substrng", "MQ3086_UnknownCallable", "Substrng", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "a callable in a projection is corrected before a predicate argument changes"),
        Sequence("G04", "CALLABLE_TRAJECTORY", "select Sum(a.Population) from #A.Entities() a", "Sum", "Smu", "MQ3086_UnknownCallable", "Smu", "a.Population", "a.Missing", "MQ3001_UnknownColumn", "Missing", "an aggregate callable rename is corrected before its source field changes"),
        Sequence("G05", "CALLABLE_TRAJECTORY", "select ToInt32(a.Population) from #A.Entities() a", "ToInt32", "ToInt3", "MQ3086_UnknownCallable", "ToInt3", "a.Population", "a.Missing", "MQ3001_UnknownColumn", "Missing", "a conversion callable rename is corrected before its source field changes"),
        Sequence("G06", "CALLABLE_TRAJECTORY", "select Length(a.Name), a.City from #A.Entities() a", "Length", "Lenght", "MQ3086_UnknownCallable", "Lenght", "a.City", "a.Ctiy", "MQ3001_UnknownColumn", "Ctiy", "a callable root is corrected before an independent projection field changes"),
        Sequence("H01", "INDEPENDENT_SIBLING_TRAJECTORY", "select a.Name, a.City from #A.Entities() a", "a.Name", "a.Missing", "MQ3001_UnknownColumn", "Missing", "a.City", "a.Unknown", "MQ3001_UnknownColumn", "Unknown", "a first sibling root is repaired before the second sibling becomes the only root"),
        Sequence("H02", "INDEPENDENT_SIBLING_TRAJECTORY", "select a.Name from #A.Entities() a where a.City = 'WARSAW'", "a.Name", "a.Missing", "MQ3001_UnknownColumn", "Missing", "a.City", "a.Unknown", "MQ3001_UnknownColumn", "Unknown", "a projection sibling is repaired before a WHERE sibling becomes current"),
        Sequence("H03", "INDEPENDENT_SIBLING_TRAJECTORY", "select a.Name from #A.Entities() a order by a.City", "a.Name", "a.Missing", "MQ3001_UnknownColumn", "Missing", "a.City", "a.Unknown", "MQ3001_UnknownColumn", "Unknown", "a projection sibling is repaired before an ORDER BY sibling becomes current"),
        Sequence("H04", "INDEPENDENT_SIBLING_TRAJECTORY", "select Self.Name, a.City from #A.Entities() a", "Self.Name", "Self.Missing", "MQ3028_UnknownProperty", "Missing", "a.City", "a.Unknown", "MQ3001_UnknownColumn", "Unknown", "a property root is repaired before a qualified column sibling becomes current"),
        Sequence("H05", "INDEPENDENT_SIBLING_TRAJECTORY", "select Sum(a.Population) from #A.Entities() a where a.City = 'WARSAW'", "a.Population", "a.Missing", "MQ3001_UnknownColumn", "Missing", "a.City", "a.Unknown", "MQ3001_UnknownColumn", "Unknown", "an aggregate argument root is repaired before a predicate sibling becomes current"),
        Sequence("H06", "INDEPENDENT_SIBLING_TRAJECTORY", "select RowNumber() over (order by a.Name), a.City from #A.Entities() a", "a.Name", "a.Missing", "MQ3001_UnknownColumn", "Missing", "a.City", "a.Unknown", "MQ3001_UnknownColumn", "Unknown", "a window key root is repaired before a projection sibling becomes current"),
    ];

    [TestMethod]
    public void EditingTrajectories_ShouldHonorFrozenContract()
    {
        foreach (var candidate in CandidateCases)
        {
            Assert.IsTrue(Analyze(candidate.Steps[0].Query).IsSuccess, candidate.Id);
            foreach (var step in candidate.Steps)
                AssertStep(candidate, step);
        }
    }

    [TestMethod]
    public void CandidateMatrix_ShouldCoverEightTrajectoryFamilies()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(48, CandidateCases.Select(static candidate => candidate.Id).Distinct(StringComparer.Ordinal));
        var familyCounts = CandidateCases.GroupBy(static candidate => candidate.Family).ToDictionary(static group => group.Key, static group => group.Count(), StringComparer.Ordinal);
        CollectionAssert.AreEquivalent(new[] { "SOURCE_ALIAS_TRAJECTORY", "PROJECTION_ALIAS_TRAJECTORY", "CTE_OUTPUT_TRAJECTORY", "DERIVED_OUTPUT_TRAJECTORY", "NESTED_SHADOWING_TRAJECTORY", "JOIN_SCOPE_TRAJECTORY", "CALLABLE_TRAJECTORY", "INDEPENDENT_SIBLING_TRAJECTORY" }, familyCounts.Keys.ToArray());
        Assert.IsTrue(familyCounts.Values.All(static count => count == 6));
        Assert.IsTrue(CandidateCases.All(static candidate => candidate.Steps.Count == 5));
    }

    [TestMethod]
    public void RepeatedFullAnalysis_ShouldRemoveStaleErrorsAndShiftCurrentCoordinates()
    {
        foreach (var candidate in CandidateCases)
        {
            var beforeShift = Analyze(candidate.Steps[2].Query).Errors.Single();
            var shifted = Analyze(candidate.Steps[3].Query).Errors.Single();
            Assert.AreEqual(beforeShift.Code, shifted.Code, candidate.Id);
            Assert.AreEqual(beforeShift.Span.Start + 2, shifted.Span.Start, candidate.Id);
            Assert.AreEqual(beforeShift.Span.Length, shifted.Span.Length, candidate.Id);
            var beforeText = candidate.Steps[2].Query.Substring(beforeShift.Span.Start, beforeShift.Span.Length);
            var shiftedText = candidate.Steps[3].Query.Substring(shifted.Span.Start, shifted.Span.Length);
            Assert.AreEqual(beforeText, shiftedText, candidate.Id);
            var repeated = Analyze(candidate.Steps[3].Query).Errors.Single();
            Assert.AreEqual(shifted.Span, repeated.Span, candidate.Id);
            Assert.AreEqual(shifted.Message, repeated.Message, candidate.Id);
        }
    }

    [TestMethod]
    public void RepairSequence_ShouldExposeOnlyTheNextCurrentRoot()
    {
        foreach (var candidate in CandidateCases)
        {
            var first = Analyze(candidate.Steps[1].Query).Errors.ToArray();
            var second = Analyze(candidate.Steps[2].Query).Errors.ToArray();
            var fixedResult = Analyze(candidate.Steps[4].Query);
            CollectionAssert.AreEqual(candidate.Steps[1].ExpectedCodes.ToArray(), first.Select(static item => item.Code).ToArray(), candidate.Id);
            CollectionAssert.AreEqual(candidate.Steps[2].ExpectedCodes.ToArray(), second.Select(static item => item.Code).ToArray(), candidate.Id);
            Assert.IsTrue(fixedResult.IsSuccess, candidate.Id);
        }
    }

    private static void AssertStep(RecoveryCase candidate, TrajectoryStep step)
    {
        var result = Analyze(step.Query);
        var errors = result.Errors.ToArray();
        CollectionAssert.AreEqual(step.ExpectedCodes.ToArray(), errors.Select(static item => item.Code).ToArray(), $"{candidate.Id}/{step.Role}");
        Assert.HasCount(step.ExpectedCodes.Count, errors, $"{candidate.Id}/{step.Role}");
        for (var index = 0; index < errors.Length; index++)
        {
            var error = errors[index];
            Assert.AreEqual(DiagnosticPhase.Bind, error.Phase, $"{candidate.Id}/{step.Role}");
            Assert.AreEqual(DiagnosticSourceKind.Query, error.SourceKind, $"{candidate.Id}/{step.Role}");
            Assert.IsTrue(error.Location.IsValid && error.EndLocation.IsValid, $"{candidate.Id}/{step.Role}");
            Assert.IsTrue(error.Span.Start >= 0 && error.Span.End <= step.Query.Length, $"{candidate.Id}/{step.Role}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(error.ContextSnippet), $"{candidate.Id}/{step.Role}");
            var text = step.Query.Substring(error.Span.Start, error.Span.Length);
            Assert.IsTrue(text.Contains(step.ExpectedTokens[index], StringComparison.OrdinalIgnoreCase) || error.Message.Contains(step.ExpectedTokens[index], StringComparison.OrdinalIgnoreCase), $"{candidate.Id}/{step.Role}: {error.Message}");
        }
    }

    private static RecoveryCase Sequence(string id, string family, string seed, string firstBefore, string firstAfter, string firstCode, string firstToken, string secondBefore, string secondAfter, string secondCode, string secondToken, string rootCause)
    {
        var marker = $" /* {id} */";
        var first = ReplaceOnce(seed, firstBefore, firstAfter);
        var repaired = ReplaceOnce(first, firstAfter, firstBefore);
        var second = ReplaceOnce(repaired, secondBefore, secondAfter);
        var shifted = "  " + second;
        var fixedQuery = ReplaceOnce(shifted, secondAfter, secondBefore);
        return new RecoveryCase(id, family, rootCause,
        [
            Step("valid-seed", seed + marker, [], []),
            Step("first-fault", first + marker, [Enum.Parse<DiagnosticCode>(firstCode)], [firstToken]),
            Step("second-fault-after-first-repair", second + marker, [Enum.Parse<DiagnosticCode>(secondCode)], [secondToken]),
            Step("shifted-reanalysis", shifted + marker, [Enum.Parse<DiagnosticCode>(secondCode)], [secondToken]),
            Step("fixed", fixedQuery + marker, [], [])
        ]);
    }

    private static TrajectoryStep Step(string role, string query, IReadOnlyList<DiagnosticCode> codes, IReadOnlyList<string> tokens) => new(role, query, codes, tokens);

    private static string ReplaceOnce(string source, string before, string after)
    {
        var start = source.IndexOf(before, StringComparison.Ordinal);
        if (start < 0 || source.IndexOf(before, start + before.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException($"Expected exactly one '{before}' occurrence in '{source}'.");
        return source[..start] + after + source[(start + before.Length)..];
    }

    private static QueryAnalysisResult Analyze(string query) =>
        new QueryAnalyzer(new BasicSchemaProvider<BasicEntity>(CreateSources())).Analyze(query);

    private static Dictionary<string, IEnumerable<BasicEntity>> CreateSources() =>
        new() { ["#A"] = [new BasicEntity("WARSAW", "PL", 100)], ["#B"] = [new BasicEntity("BERLIN", "DE", 200)] };

    private sealed record RecoveryCase(string Id, string Family, string RootCause, IReadOnlyList<TrajectoryStep> Steps);
    private sealed record TrajectoryStep(string Role, string Query, IReadOnlyList<DiagnosticCode> ExpectedCodes, IReadOnlyList<string> ExpectedTokens)
    {
        public string Message => string.Join("|", ExpectedCodes);
    }
}
