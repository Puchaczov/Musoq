using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DiagnosticREC117NullPrecisionTests
{
    private const string SystemSeed = "select d.Dummy from #system.dual() d";
    private const string JoinSeed =
        "select a.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy";
    private const string ValuesSeed =
        "select a.Id from values { ( Id: 1 ) } a left join values { ( Id: 2 ) } b on a.Id = b.Id";
    private readonly TestsLoggerResolver loggerResolver = new();

    [TestMethod]
    public void AdvisoryMatrix_ShouldHonorFrozenContract()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(4, CandidateCases.GroupBy(static candidate => candidate.Family));
        Assert.IsTrue(CandidateCases.GroupBy(static candidate => candidate.Family).All(static family => family.Count() == 12));

        foreach (var candidate in CandidateCases)
        {
            var analysis = Analyze(candidate);

            Assert.IsFalse(analysis.HasErrors, $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(analysis.Errors, $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            Assert.AreEqual(
                candidate.ExpectedDiagnostics.Count,
                analysis.Warnings.Count(),
                $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            CollectionAssert.AreEqual(
                candidate.ExpectedDiagnostics.ToArray(),
                analysis.Warnings.Select(static warning => warning.Code).ToArray(),
                candidate.Id);

            foreach (var warning in analysis.Warnings)
            {
                Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity, candidate.Id);
                Assert.AreEqual(DiagnosticPhase.Bind, warning.Phase, candidate.Id);
                Assert.AreEqual(DiagnosticSourceKind.Query, warning.SourceKind, candidate.Id);
                Assert.IsTrue(warning.Location.IsValid, candidate.Id);
                Assert.IsTrue(warning.Span.Start >= 0 && warning.Span.End <= candidate.Query.Length, candidate.Id);
            }

            var syntax = new QueryAnalyzer(candidate.ProviderFactory()).ValidateSyntax(candidate.Query);
            Assert.IsFalse(syntax.HasErrors, $"{candidate.Id}: {FormatDiagnostics(syntax.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(syntax.Warnings, $"{candidate.Id}: {FormatDiagnostics(syntax.Diagnostics, candidate.Query)}");
        }
    }

    [TestMethod]
    public void WarningCompilation_ShouldRemainRunnableAndPreserveWarningContract()
    {
        foreach (var candidate in CandidateCases)
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                candidate.Query,
                $"REC117_{candidate.Id}_{Guid.NewGuid():N}",
                candidate.ProviderFactory(),
                loggerResolver,
                new CompilationOptions());

            Assert.IsTrue(build.Succeeded, $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            Assert.IsEmpty(build.Errors, $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            Assert.AreEqual(
                candidate.ExpectedDiagnostics.Count,
                build.Warnings.Count(),
                $"{candidate.Id}: {FormatDiagnostics(build.Diagnostics, candidate.Query)}");
            CollectionAssert.AreEqual(
                candidate.ExpectedDiagnostics.ToArray(),
                build.Warnings.Select(static warning => warning.Code).ToArray(),
                candidate.Id);

            using var table = build.CompiledQuery!.Run();
            Assert.IsNotNull(table, candidate.Id);
        }
    }

    [TestMethod]
    public void NullComparisonAndOuterFilterGuidance_ShouldKeepMeaningChangingAlternativesExplicit()
    {
        var nullComparison = Analyze(Candidate("N01"));
        var nullWarning = nullComparison.Warnings.Single(static warning =>
            warning.Code == DiagnosticCode.MQ5017_NullComparison);
        var nullEnvelope = MusoqErrorEnvelope.FromDiagnostic(nullWarning, Candidate("N01").Query);

        CollectionAssert.AreEqual(
            new[]
            {
                "Replace the comparison with IS NULL or IS NOT NULL.",
                "Use IS DISTINCT FROM when a total comparison is required."
            },
            nullEnvelope.SuggestedFixes.ToArray());
        Assert.IsTrue(nullEnvelope.Actions.All(static action => action.TextEdit is null));

        var outer = Candidate("F01");
        var outerAnalysis = Analyze(outer);
        var outerWarning = outerAnalysis.Warnings.Single(static warning =>
            warning.Code == DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter);
        var outerEnvelope = MusoqErrorEnvelope.FromDiagnostic(outerWarning, outer.Query);

        CollectionAssert.AreEqual(
            new[]
            {
                "Move the restriction into the JOIN ON clause when it is part of matching.",
                "Use an explicit row-presence predicate when removing unmatched rows is intentional."
            },
            outerEnvelope.SuggestedFixes.ToArray());
        Assert.IsTrue(outerEnvelope.Actions.All(static action => action.TextEdit is null));

        var build = InstanceCreator.CompileWithDiagnostics(
            outer.Query,
            $"REC117_intent_{Guid.NewGuid():N}",
            outer.ProviderFactory(),
            loggerResolver);

        Assert.IsTrue(build.Succeeded, FormatDiagnostics(build.Diagnostics, outer.Query));
        using var table = build.CompiledQuery!.Run();
        Assert.AreEqual(0, table.Count, "The warning path must retain the authored WHERE semantics; moving it into ON would change this result.");
    }

    private static QueryAnalysisResult Analyze(AdvisoryCase candidate)
    {
        return new QueryAnalyzer(candidate.ProviderFactory()).Analyze(candidate.Query);
    }

    private static AdvisoryCase Candidate(string id)
    {
        return CandidateCases.Single(candidate => candidate.Id == id);
    }

    private static AdvisoryCase SystemCase(
        string id,
        string family,
        string query,
        IReadOnlyList<DiagnosticCode> expectedDiagnostics)
    {
        return new AdvisoryCase(
            id,
            family,
            SystemSeed,
            query,
            static () => new SystemSchemaProvider(),
            expectedDiagnostics);
    }

    private static AdvisoryCase JoinCase(
        string id,
        string family,
        string query,
        IReadOnlyList<DiagnosticCode> expectedDiagnostics)
    {
        return new AdvisoryCase(
            id,
            family,
            JoinSeed,
            query,
            static () => new AdvisoryWarningPropagationTests.NullableJoinSchemaProvider(),
            expectedDiagnostics);
    }

    private static AdvisoryCase ValuesCase(
        string id,
        string family,
        string query,
        IReadOnlyList<DiagnosticCode> expectedDiagnostics)
    {
        return new AdvisoryCase(
            id,
            family,
            ValuesSeed,
            query,
            static () => new SystemSchemaProvider(),
            expectedDiagnostics);
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics, string query)
    {
        return $"{query}{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))}";
    }

    private static IReadOnlyList<AdvisoryCase> CandidateCases { get; } =
    [
        SystemCase("N01", "NULL_COMPARISON", "select d.Dummy from #system.dual() d where d.Dummy = null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N02", "NULL_COMPARISON", "select d.Dummy from #system.dual() d where d.Dummy <> null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N03", "NULL_COMPARISON", "select d.Dummy from #system.dual() d where d.Dummy != null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N04", "NULL_COMPARISON", "select d.Dummy from #system.dual() d where d.Dummy < null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N05", "NULL_COMPARISON", "select d.Dummy from #system.dual() d where d.Dummy <= null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N06", "NULL_COMPARISON", "select d.Dummy from #system.dual() d where d.Dummy > null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N07", "NULL_COMPARISON", "select d.Dummy from #system.dual() d where d.Dummy >= null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N08", "NULL_COMPARISON", "select d.Dummy, Count(*) from #system.dual() d group by d.Dummy having d.Dummy = null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N09", "NULL_COMPARISON", "select Count(*) filter (where d.Dummy <= null) from #system.dual() d", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N10", "NULL_COMPARISON", "select d.Dummy, RowNumber() over (order by d.Dummy) as rn from #system.dual() d qualify RowNumber() over (order by d.Dummy) = 1 and d.Dummy > null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("N11", "NULL_COMPARISON", "select d.Dummy = null as IsUnknown from #system.dual() d", []),
        SystemCase("N12", "NULL_COMPARISON", "select d.Dummy from #system.dual() d order by d.Dummy = null", []),

        JoinCase("O01", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy is null", [DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck]),
        ValuesCase("O02", "OUTER_PRESENCE", "select a.Id, b.Id from values { ( Id: 1 ) } a left join values { ( Id: 2 ) } b on a.Id = b.Id where b.Id is null", []),
        JoinCase("O03", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a right join #join.items() b on a.Dummy = b.Dummy where a.Dummy is null", [DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck]),
        JoinCase("O04", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a full outer join #join.items() b on a.Dummy = b.Dummy where a.Dummy is null", [DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck]),
        JoinCase("O05", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a full outer join #join.items() b on a.Dummy = b.Dummy where b.Dummy is null", [DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck]),
        JoinCase("O06", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a outer apply #join.items() b where b.Dummy is null", [DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck]),
        JoinCase("O07", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b is missing", []),
        JoinCase("O08", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b is present", []),
        JoinCase("O09", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy is null or a.Dummy is distinct from null", [DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck]),
        JoinCase("O10", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy is null and a.Dummy is distinct from null", [DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck]),
        JoinCase("O11", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a cross join #join.items() b where b.Dummy is null", []),
        JoinCase("O12", "OUTER_PRESENCE", "select a.Dummy, b.Dummy from #join.items() a left join (select source.Dummy from #join.items() source) b on a.Dummy = b.Dummy where b.Dummy is null", []),

        JoinCase("F01", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy = 'match'", [DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter]),
        JoinCase("F02", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a right join #join.items() b on a.Dummy = b.Dummy where a.Dummy = 'match'", [DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter]),
        JoinCase("F03", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a full outer join #join.items() b on a.Dummy = b.Dummy where b.Dummy = 'match'", [DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter]),
        JoinCase("F04", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a outer apply #join.items() b where b.Dummy = 'match'", [DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter]),
        JoinCase("F05", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy and b.Dummy = 'match'", []),
        JoinCase("F06", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a inner join #join.items() b on a.Dummy = b.Dummy where b.Dummy = 'match'", []),
        JoinCase("F07", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy = 'match' or a.Dummy = 'keep'", []),
        JoinCase("F08", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy = 'match' and a.Dummy = 'keep'", [DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter]),
        JoinCase("F09", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b is present and b.Dummy = 'match'", []),
        JoinCase("F10", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b is missing or b.Dummy = 'match'", []),
        JoinCase("F11", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy is not null", [DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter]),
        JoinCase("F12", "OUTER_FILTER", "select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy contains ('match')", []),

        SystemCase("M01", "NOT_IN", "select d.Dummy from #system.dual() d where d.Dummy not in ('Alice', null)", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("M02", "NOT_IN", "select d.Dummy from #system.dual() d where d.Dummy not in (null)", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("M03", "NOT_IN", "select d.Dummy from #system.dual() d where d.Dummy not in ('Alice', null, 'Bob')", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("M04", "NOT_IN", "let missing: string = null; select d.Dummy from #system.dual() d where d.Dummy not in ('Alice', $missing)", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("M05", "NOT_IN", "let missing: string = null; select d.Dummy from #system.dual() d where d.Dummy not in ($missing, 'Alice')", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("M06", "NOT_IN", "select d.Dummy from #system.dual() d where d.Dummy not in ('Alice', null) or d.Dummy = 'single'", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("M07", "NOT_IN", "select d.Dummy from #system.dual() d inner join #system.dual() e on d.Dummy not in ('Alice', null)", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("M08", "NOT_IN", "select d.Dummy, Count(*) from #system.dual() d group by d.Dummy having d.Dummy not in ('Alice', null)", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("M09", "NOT_IN", "select Count(*) filter (where d.Dummy not in ('Alice', null)) from #system.dual() d", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("M10", "NOT_IN", "select d.Dummy from #system.dual() d where d.Dummy in ('Alice', null)", []),
        SystemCase("M11", "NOT_IN", "select d.Dummy from #system.dual() d where d.Dummy not in ('Alice', 'Bob')", []),
        SystemCase("M12", "NOT_IN", "select d.Dummy not in ('Alice', null) as Excluded from #system.dual() d", [])
    ];

    private sealed record AdvisoryCase(
        string Id,
        string Family,
        string SeedQuery,
        string Query,
        Func<ISchemaProvider> ProviderFactory,
        IReadOnlyList<DiagnosticCode> ExpectedDiagnostics);
}
