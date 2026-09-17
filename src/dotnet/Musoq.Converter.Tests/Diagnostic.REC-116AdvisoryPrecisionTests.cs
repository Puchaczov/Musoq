using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DiagnosticREC116AdvisoryPrecisionTests
{
    private const string SystemSeed = "select d.Dummy from #system.dual() d";
    private const string RawSeed = "select f.Path from #raw.files(r'safe\\root', true) f";
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
                Assert.IsTrue(warning.Location.IsValid, candidate.Id);
                Assert.IsTrue(warning.Span.Start >= 0 && warning.Span.End <= candidate.Query.Length, candidate.Id);
            }

            var syntax = new QueryAnalyzer(candidate.ProviderFactory()).ValidateSyntax(candidate.Query);
            Assert.IsFalse(syntax.HasErrors, $"{candidate.Id}: {FormatDiagnostics(syntax.Diagnostics, candidate.Query)}");
            Assert.AreEqual(
                candidate.ExpectedSyntaxWarningCount,
                syntax.Warnings.Count(),
                $"{candidate.Id}: {FormatDiagnostics(syntax.Diagnostics, candidate.Query)}");
            Assert.IsTrue(
                syntax.Warnings.All(static warning => warning.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape),
                candidate.Id);
        }
    }

    [TestMethod]
    public void WarningCompilation_ShouldRemainRunnableAndPreserveWarningContract()
    {
        foreach (var candidate in CandidateCases)
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                candidate.Query,
                $"REC116_{candidate.Id}_{Guid.NewGuid():N}",
                candidate.ProviderFactory(),
                loggerResolver);

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
    public void WarningLocations_ShouldIdentifyTheFirstHazardAndDeduplicatePerLiteral()
    {
        var rooted = Analyze(Candidate("P01"));
        var rootedWarning = rooted.Warnings.Single();
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, rootedWarning.Code);
        Assert.AreEqual(DiagnosticPhase.Parse, rootedWarning.Phase);
        Assert.AreEqual(Candidate("P01").Query.IndexOf(@"\n", StringComparison.Ordinal), rootedWarning.Span.Start);
        Assert.AreEqual(2, rootedWarning.Span.Length);

        var regex = Analyze(Candidate("R01"));
        var regexWarning = regex.Warnings.Single();
        Assert.AreEqual(DiagnosticCode.MQ5015_SuspiciousRegexEscape, regexWarning.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, regexWarning.Phase);
        Assert.AreEqual(Candidate("R01").Query.IndexOf(@"\b", StringComparison.Ordinal), regexWarning.Span.Start);
        Assert.AreEqual(2, regexWarning.Span.Length);

        var glob = Analyze(Candidate("L01"));
        var globWarning = glob.Warnings.Single();
        Assert.AreEqual(DiagnosticCode.MQ5016_GlobWildcardInLike, globWarning.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, globWarning.Phase);
        Assert.AreEqual(Candidate("L01").Query.IndexOf('*'), globWarning.Span.Start);
        Assert.AreEqual(1, globWarning.Span.Length);

        var twoRegexLiterals = Analyze(Candidate("R09"));
        Assert.HasCount(2, twoRegexLiterals.Warnings);
        Assert.AreEqual(2, twoRegexLiterals.Warnings.Select(static warning => warning.Span.Start).Distinct().Count());
        Assert.IsTrue(twoRegexLiterals.Warnings.All(static warning => warning.Span.Length == 2));

        var twoPathLiterals = Analyze(Candidate("P12"));
        Assert.HasCount(2, twoPathLiterals.Warnings);
        Assert.AreEqual(2, twoPathLiterals.Warnings.Select(static warning => warning.Span.Start).Distinct().Count());
        Assert.IsTrue(twoPathLiterals.Warnings.All(static warning => warning.Span.Length == 2));

        var characterClass = Analyze(Candidate("R03"));
        Assert.IsEmpty(characterClass.Warnings);
    }

    [TestMethod]
    public void SyntaxOnlyAndBoundAnalysis_ShouldRespectAdvisoryPhaseBoundary()
    {
        foreach (var id in new[] { "P05", "R01", "L01", "S01", "S02", "S03" })
        {
            var candidate = Candidate(id);
            var syntax = new QueryAnalyzer(candidate.ProviderFactory()).ValidateSyntax(candidate.Query);
            var analysis = Analyze(candidate);

            Assert.IsEmpty(syntax.Warnings, id);
            Assert.IsNotEmpty(analysis.Warnings, id);
            Assert.IsTrue(
                analysis.Warnings.All(static warning => warning.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape
                    ? warning.Phase == DiagnosticPhase.Parse
                    : warning.Phase == DiagnosticPhase.Bind),
                $"{id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
        }

        var rooted = Candidate("P01");
        var rootedSyntax = new QueryAnalyzer(rooted.ProviderFactory()).ValidateSyntax(rooted.Query);
        Assert.HasCount(1, rootedSyntax.Warnings);
        Assert.AreEqual(DiagnosticPhase.Parse, rootedSyntax.Warnings.Single().Phase);
    }

    [TestMethod]
    public void PathWarning_ShouldPreserveAuthoredOrdinaryStringValue()
    {
        var candidate = Candidate("P05");
        var build = InstanceCreator.CompileWithDiagnostics(
            candidate.Query,
            $"REC116_value_{Guid.NewGuid():N}",
            candidate.ProviderFactory(),
            loggerResolver);

        Assert.IsTrue(build.Succeeded, FormatDiagnostics(build.Diagnostics, candidate.Query));
        using var table = build.CompiledQuery!.Run();
        Assert.HasCount(1, table);
        Assert.AreEqual("some\text", table[0][0]);
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
        IReadOnlyList<DiagnosticCode> expectedDiagnostics,
        int expectedSyntaxWarningCount = 0)
    {
        return new AdvisoryCase(
            id,
            family,
            SystemSeed,
            query,
            static () => new SystemSchemaProvider(),
            expectedDiagnostics,
            expectedSyntaxWarningCount);
    }

    private static AdvisoryCase RawCase(
        string id,
        string family,
        string query,
        IReadOnlyList<DiagnosticCode> expectedDiagnostics,
        int expectedSyntaxWarningCount = 0)
    {
        return new AdvisoryCase(
            id,
            family,
            RawSeed,
            query,
            static () => new RawStringLiteralSchemaProvider(),
            expectedDiagnostics,
            expectedSyntaxWarningCount);
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics, string query)
    {
        return $"{query}{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))}";
    }

    private static IReadOnlyList<AdvisoryCase> CandidateCases { get; } =
    [
        SystemCase("P01", "PATH", @"select 'C:\new\test' from #system.dual() d", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape], 1),
        SystemCase("P02", "PATH", @"select '/tmp\new\test' from #system.dual() d", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape], 1),
        SystemCase("P03", "PATH", @"select '.\new\test' from #system.dual() d", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape], 1),
        SystemCase("P04", "PATH", @"select '..\new\test' from #system.dual() d", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape], 1),
        RawCase("P05", "PATH", @"select f.Path from #raw.files('some\text', true) f", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        RawCase("P06", "PATH", @"select f.Path from #raw.files('folder\new', true) f", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        RawCase("P07", "PATH", @"select f.Path from #raw.files(r'some\text', true) f", []),
        RawCase("P08", "PATH", @"select f.Path from #raw.files('some\\text', true) f", []),
        RawCase("P09", "PATH", @"select f.Path from #raw.files('\t', true) f", []),
        RawCase("P10", "PATH", @"select f.Path from #raw.files('some\q', true) f", []),
        RawCase("P11", "PATH", @"select f.Path from #raw.files(r'safe\root', true) f where f.Path = 'other\temp'", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        RawCase("P12", "PATH", @"select f.Path from #raw.files(r'safe\root', true) f where f.Path in ('one\temp', 'two\new')", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),

        SystemCase("R01", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy rlike '\bword\b'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("R02", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy rlike '\b'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("R03", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy rlike '[\b]'", []),
        SystemCase("R04", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy rlike r'[\b]'", []),
        SystemCase("R05", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy rlike '\\bword\\b'", []),
        SystemCase("R06", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy rlike '\d+\s+\w+'", []),
        SystemCase("R07", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy rlike Dummy", []),
        SystemCase("R08", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy not rlike '\bword\b'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("R09", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy rlike '\bword' or d.Dummy rlike '\bother'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape, DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("R10", "REGEX", @"select d.Dummy from #system.dual() d where d.Dummy rlike '\bword\b' and d.Dummy like '%d%'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("R11", "REGEX", @"select Match('\bword\b', d.Dummy) from #system.dual() d", [DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("R12", "REGEX", @"select IsMatch(d.Dummy, '\bword\b') from #system.dual() d", [DiagnosticCode.MQ5015_SuspiciousRegexEscape]),

        SystemCase("L01", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like '*.log'", [DiagnosticCode.MQ5016_GlobWildcardInLike]),
        SystemCase("L02", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like 'file?.txt'", [DiagnosticCode.MQ5016_GlobWildcardInLike]),
        SystemCase("L03", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like 'logs/?'", [DiagnosticCode.MQ5016_GlobWildcardInLike]),
        SystemCase("L04", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like 'percent%value'", []),
        SystemCase("L05", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like '_leading'", []),
        SystemCase("L06", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like 'glob*%mixed'", []),
        SystemCase("L07", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like 'glob_*'", []),
        SystemCase("L08", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like '* prose'", []),
        SystemCase("L09", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like '? question'", []),
        SystemCase("L10", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like 'what?'", []),
        SystemCase("L11", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like Dummy", []),
        SystemCase("L12", "LIKE", @"select d.Dummy from #system.dual() d where d.Dummy like r'*.log'", [DiagnosticCode.MQ5016_GlobWildcardInLike]),

        RawCase("S01", "SURFACES", @"select f.Path from #raw.files('reports\new', true) f", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        SystemCase("S02", "SURFACES", @"select d.Dummy from #system.dual() d where d.Dummy rlike 'prefix\bword'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("S03", "SURFACES", @"select d.Dummy from #system.dual() d where d.Dummy like 'reports/*.log'", [DiagnosticCode.MQ5016_GlobWildcardInLike]),
        SystemCase("S04", "SURFACES", @"select '\\server\share\new' from #system.dual() d", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape], 1),
        RawCase("S05", "SURFACES", @"select f.Path from #raw.files('archive\new', true) f", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        SystemCase("S06", "SURFACES", @"select d.Dummy from #system.dual() d where d.Dummy rlike '[\b]'", []),
        RawCase("S07", "SURFACES", @"select f.Path from #raw.files(r'safe\root', true) f where f.Path = 'archive\temp'", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        RawCase("S08", "SURFACES", @"select f.Path from #raw.files(r'safe\root', true) f where f.Path in ('first\temp', 'second\new')", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        SystemCase("S09", "SURFACES", @"select d.Dummy from #system.dual() d where d.Dummy rlike '\bfirst' or d.Dummy rlike '\bsecond'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape, DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("S10", "SURFACES", @"select d.Dummy from #system.dual() d where d.Dummy rlike '\bword' and d.Dummy like '*.log'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape, DiagnosticCode.MQ5016_GlobWildcardInLike]),
        RawCase("S11", "SURFACES", @"select f.Path from #raw.files('source\new', true) f where f.Path = 'predicate\temp'", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        SystemCase("S12", "SURFACES", @"select d.Dummy from #system.dual() d where d.Dummy not like 'hello?'", [])
    ];

    private sealed record AdvisoryCase(
        string Id,
        string Family,
        string SeedQuery,
        string Query,
        Func<ISchemaProvider> ProviderFactory,
        IReadOnlyList<DiagnosticCode> ExpectedDiagnostics,
        int ExpectedSyntaxWarningCount);
}
