using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Evaluator.Tables;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class DiagnosticREC120WarningAwareApiTests
{
    private const string SystemSeed = "select d.Dummy from #system.dual() d";
    private readonly TestsLoggerResolver loggerResolver = new();

    [TestMethod]
    public void ApiSurfaceMatrix_ShouldKeepWarningsWithinTheirDocumentedPhases()
    {
        Assert.HasCount(48, CandidateCases);
        Assert.HasCount(4, CandidateCases.GroupBy(static candidate => candidate.Family));
        Assert.IsTrue(CandidateCases.GroupBy(static candidate => candidate.Family).All(static family => family.Count() == 12));

        foreach (var candidate in CandidateCases)
        {
            var analysis = Analyze(candidate.Query);
            Assert.IsFalse(analysis.HasErrors, $"{candidate.Id}: {FormatDiagnostics(analysis.Diagnostics, candidate.Query)}");
            AssertCodes(candidate.Id, analysis.Warnings, candidate.ExpectedWarnings);

            var syntax = new QueryAnalyzer(new SystemSchemaProvider()).ValidateSyntax(candidate.Query);
            Assert.IsFalse(syntax.HasErrors, $"{candidate.Id}: {FormatDiagnostics(syntax.Diagnostics, candidate.Query)}");
            AssertCodes(candidate.Id, syntax.Warnings, candidate.ExpectedSyntaxWarnings);

            foreach (var warning in analysis.Warnings)
                AssertWarningMetadata(candidate.Id, warning);
        }
    }

    [TestMethod]
    public async Task WarningAwareCompilation_ShouldPreserveWarningsAcrossPromisedSurfaces()
    {
        foreach (var candidate in CompilationCases)
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                candidate.Query,
                $"REC120_default_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver);
            var optionsBuild = InstanceCreator.CompileWithDiagnostics(
                candidate.Query,
                $"REC120_options_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());
            var asyncBuild = await InstanceCreator.CompileWithDiagnosticsAsync(
                candidate.Query,
                $"REC120_async_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());
            var inspection = InstanceCreator.CompileForInspection(
                candidate.Query,
                $"REC120_inspection_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());
            var analysisItems = InstanceCreator.CreateForAnalyze(
                candidate.Query,
                $"REC120_analyze_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());

            AssertBuildWarnings(candidate, build);
            AssertBuildWarnings(candidate, optionsBuild);
            AssertBuildWarnings(candidate, asyncBuild);
            AssertCodes(candidate.Id, inspection.Warnings, candidate.ExpectedWarnings);
            AssertCodes(candidate.Id, analysisItems.DiagnosticContext.Warnings, candidate.ExpectedWarnings);
            Assert.IsNotNull(inspection.ExecutionPlan, candidate.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode), candidate.Id);

            using var diagnosticTable = build.CompiledQuery!.Run();
            using var convenienceQuery = InstanceCreator.CompileForExecution(
                candidate.Query,
                $"REC120_convenience_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver);
            using var convenienceTable = convenienceQuery.Run();
            AssertTablesEqual(candidate.Id, diagnosticTable, convenienceTable);
        }
    }

    [TestMethod]
    public void DiagnosticEnvelopes_ShouldSeparateErrorsFromWarningsAtThePublicBoundary()
    {
        foreach (var candidate in EnvelopeCases)
        {
            var result = InstanceCreator.CompileWithDiagnostics(
                candidate.Query,
                $"REC120_envelope_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());

            Assert.IsTrue(result.Succeeded, $"{candidate.Id}: {FormatDiagnostics(result.Diagnostics, candidate.Query)}");
            CollectionAssert.AreEqual(candidate.ExpectedWarnings.ToArray(), result.Warnings.Select(static item => item.Code).ToArray(), candidate.Id);
            Assert.IsEmpty(result.ToEnvelopes(), candidate.Id);
            CollectionAssert.AreEqual(candidate.ExpectedWarnings.ToArray(), result.ToAllEnvelopes().Select(static item => item.Code).ToArray(), candidate.Id);
        }

        var errorCases = new[]
        {
            new EnvelopeExpectation("E13", "select Missing from #system.dual() d", [DiagnosticCode.MQ3001_UnknownColumn]),
            new EnvelopeExpectation("E14", "select Missing(d.Dummy) from #system.dual() d", [DiagnosticCode.MQ3086_UnknownCallable]),
            new EnvelopeExpectation("E15", string.Empty, [DiagnosticCode.MQ2016_IncompleteStatement])
        };

        foreach (var candidate in errorCases)
        {
            var result = InstanceCreator.CompileWithDiagnostics(
                candidate.Query,
                $"REC120_error_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());

            Assert.IsFalse(result.Succeeded, candidate.Id);
            CollectionAssert.AreEqual(candidate.ExpectedErrors.ToArray(), result.Errors.Select(static item => item.Code).ToArray(), candidate.Id);
            CollectionAssert.AreEqual(candidate.ExpectedErrors.ToArray(), result.ToEnvelopes().Select(static item => item.Code).ToArray(), candidate.Id);
            CollectionAssert.AreEqual(candidate.ExpectedErrors.ToArray(), result.ToAllEnvelopes().Select(static item => item.Code).ToArray(), candidate.Id);
        }
    }

    [TestMethod]
    public void WarningPreservation_ShouldKeepGeneratedExecutionCacheAndResultsStable()
    {
        foreach (var candidate in PreservationCases)
        {
            var warningBuild = InstanceCreator.CompileWithDiagnostics(
                candidate.WarningQuery,
                $"REC120_preserve_warning_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());

            Assert.IsTrue(warningBuild.Succeeded, $"{candidate.Id}: {FormatDiagnostics(warningBuild.Diagnostics, candidate.WarningQuery)}");
            CollectionAssert.AreEqual(candidate.ExpectedWarnings.ToArray(), warningBuild.Warnings.Select(static item => item.Code).ToArray(), candidate.Id);
            Assert.IsNotNull(warningBuild.BuildItems, candidate.Id);

            using var warningTable = warningBuild.CompiledQuery!.Run();
            if (candidate.QuietQuery is not null)
            {
                using var quietQuery = InstanceCreator.CompileForExecution(
                    candidate.QuietQuery,
                    $"REC120_preserve_quiet_{candidate.Id}_{Guid.NewGuid():N}",
                    new SystemSchemaProvider(),
                    loggerResolver);
                using var quietTable = quietQuery.Run();
                AssertTablesEqual(candidate.Id, warningTable, quietTable);
            }

            var inspection = InstanceCreator.CompileForInspection(
                candidate.WarningQuery,
                $"REC120_preserve_inspection_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());
            Assert.IsNotNull(inspection.ExecutionPlan, candidate.Id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode), candidate.Id);

            var replay = InstanceCreator.CompileWithDiagnostics(
                candidate.WarningQuery,
                $"REC120_preserve_replay_{candidate.Id}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                loggerResolver,
                new CompilationOptions());
            CollectionAssert.AreEqual(
                warningBuild.Warnings.Select(static item => item.Code).ToArray(),
                replay.Warnings.Select(static item => item.Code).ToArray(),
                candidate.Id);
        }

        var suffix = Guid.NewGuid().ToString("N");
        var firstQuery = $"select 'C:\\new\\test' as Path, 'token-{suffix}' as Token from #artifact.items() i";
        var secondQuery = $"select  'C:\\new\\test'  as  Path,  'token-{suffix}'  as  Token  from #artifact.items()  i";
        var firstProvider = new ArtifactSchemaProvider(new ArtifactSchema("first"));
        var secondProvider = new ArtifactSchemaProvider(new ArtifactSchema("second"));
        var first = InstanceCreator.CompileWithDiagnostics(
            firstQuery,
            $"REC120_cache_first_{suffix}",
            firstProvider,
            loggerResolver,
            new CompilationOptions());
        var second = InstanceCreator.CompileWithDiagnostics(
            secondQuery,
            $"REC120_cache_second_{suffix}",
            secondProvider,
            loggerResolver,
            new CompilationOptions());

        Assert.IsTrue(first.Succeeded, FormatDiagnostics(first.Diagnostics, firstQuery));
        Assert.IsTrue(second.Succeeded, FormatDiagnostics(second.Diagnostics, secondQuery));
        Assert.HasCount(1, first.Warnings);
        Assert.HasCount(1, second.Warnings);
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, first.Warnings[0].Code);
        Assert.AreEqual(first.Warnings[0].Code, second.Warnings[0].Code);
        Assert.IsNotNull(first.BuildItems);
        Assert.IsNotNull(second.BuildItems);
        var firstIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(first.BuildItems!, firstProvider);
        var secondIdentity = InstanceCreator.GetCanonicalExecutionEntryIdentityForTests(second.BuildItems!, secondProvider);
        Assert.AreNotEqual(0, firstIdentity);
        Assert.AreEqual(firstIdentity, secondIdentity);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(new SystemSchemaProvider()).Analyze(query);
    }

    private static void AssertBuildWarnings(ApiCase candidate, BuildResult result)
    {
        Assert.IsTrue(result.Succeeded, $"{candidate.Id}: {FormatDiagnostics(result.Diagnostics, candidate.Query)}");
        Assert.IsEmpty(result.Errors, candidate.Id);
        AssertCodes(candidate.Id, result.Warnings, candidate.ExpectedWarnings);
    }

    private static void AssertCodes(string id, IEnumerable<Diagnostic> diagnostics, IReadOnlyList<DiagnosticCode> expected)
    {
        var actual = diagnostics.Select(static item => item.Code).ToArray();
        CollectionAssert.AreEqual(expected.ToArray(), actual, $"{id}: expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}]");
    }

    private static void AssertWarningMetadata(string id, Diagnostic warning)
    {
        Assert.AreEqual(DiagnosticSeverity.Warning, warning.Severity, id);
        Assert.AreEqual(DiagnosticSourceKind.Query, warning.SourceKind, id);
        Assert.IsTrue(warning.Location.IsValid, id);
        Assert.IsTrue(warning.EndLocation.IsValid, id);
        Assert.IsTrue(warning.Span.Length > 0, id);
    }

    private static void AssertTablesEqual(string id, Table expected, Table actual)
    {
        Assert.AreEqual(expected.Count, actual.Count, id);
        Assert.AreEqual(expected.Columns.Count(), actual.Columns.Count(), id);
        for (var rowIndex = 0; rowIndex < expected.Count; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < expected.Columns.Count(); columnIndex++)
                Assert.AreEqual(expected[rowIndex][columnIndex], actual[rowIndex][columnIndex], id);
        }
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics, string query)
    {
        return $"{query}{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))}";
    }

    private static ApiCase SystemCase(
        string id,
        string family,
        string query,
        IReadOnlyList<DiagnosticCode> expectedWarnings,
        IReadOnlyList<DiagnosticCode>? expectedSyntaxWarnings = null)
    {
        return new ApiCase(id, family, query, expectedWarnings, expectedSyntaxWarnings ?? []);
    }

    private static IReadOnlyList<ApiCase> SurfaceCases { get; } =
    [
        SystemCase("P01", "PHASE_SURFACES", "select 'C:\\new\\test' from #system.dual() d", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape], [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        SystemCase("P02", "PHASE_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy rlike '\\bword\\b'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("P03", "PHASE_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy like '*.log'", [DiagnosticCode.MQ5016_GlobWildcardInLike]),
        SystemCase("P04", "PHASE_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy = null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("P05", "PHASE_SURFACES", SystemSeed + " skip 1", [DiagnosticCode.MQ5021_UnorderedSkip]),
        SystemCase("P06", "PHASE_SURFACES", "with dead as (select d.Dummy from #system.dual() d) select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("P07", "PHASE_SURFACES", "let dead: int = 1; select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5023_UnusedScriptVariable]),
        SystemCase("P08", "PHASE_SURFACES", "select case when false then 'dead' else 'live' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("P09", "PHASE_SURFACES", SystemSeed + " where true", [DiagnosticCode.MQ5010_TautologicalCondition]),
        SystemCase("P10", "PHASE_SURFACES", SystemSeed + " where false", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        SystemCase("P11", "PHASE_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy not in ('x', null)", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("P12", "PHASE_SURFACES", SystemSeed, [])
    ];

    private static IReadOnlyList<ApiCase> CompilationCases { get; } =
    [
        SystemCase("C01", "COMPILATION_SURFACES", "select 'C:\\new\\test' from #system.dual() d", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        SystemCase("C02", "COMPILATION_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy rlike '\\bword\\b'", [DiagnosticCode.MQ5015_SuspiciousRegexEscape]),
        SystemCase("C03", "COMPILATION_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy like '*.log'", [DiagnosticCode.MQ5016_GlobWildcardInLike]),
        SystemCase("C04", "COMPILATION_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy = null", [DiagnosticCode.MQ5017_NullComparison]),
        SystemCase("C05", "COMPILATION_SURFACES", SystemSeed + " skip 1", [DiagnosticCode.MQ5021_UnorderedSkip]),
        SystemCase("C06", "COMPILATION_SURFACES", "with dead as (select d.Dummy from #system.dual() d) select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("C07", "COMPILATION_SURFACES", "let dead: int = 1; select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5023_UnusedScriptVariable]),
        SystemCase("C08", "COMPILATION_SURFACES", "select case when false then 'dead' else 'live' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("C09", "COMPILATION_SURFACES", SystemSeed + " where true", [DiagnosticCode.MQ5010_TautologicalCondition]),
        SystemCase("C10", "COMPILATION_SURFACES", SystemSeed + " where false", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        SystemCase("C11", "COMPILATION_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy not in ('x', null)", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("C12", "COMPILATION_SURFACES", SystemSeed + " ", [])
    ];

    private static IReadOnlyList<ApiCase> EnvelopeCases { get; } =
    [
        SystemCase("E01", "ENVELOPE_SURFACES", "select 'C:\\new\\test' from #system.dual() d", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape]),
        SystemCase("E02", "ENVELOPE_SURFACES", "select 'C:\\new\\test' from #system.dual() d where true", [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, DiagnosticCode.MQ5010_TautologicalCondition]),
        SystemCase("E03", "ENVELOPE_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy rlike '\\bword\\b' skip 1", [DiagnosticCode.MQ5015_SuspiciousRegexEscape, DiagnosticCode.MQ5021_UnorderedSkip]),
        SystemCase("E04", "ENVELOPE_SURFACES", "let dead: int = 1; with dead_cte as (select d.Dummy from #system.dual() d) select d.Dummy from #system.dual() d", [DiagnosticCode.MQ5023_UnusedScriptVariable, DiagnosticCode.MQ5022_UnusedCte]),
        SystemCase("E05", "ENVELOPE_SURFACES", "select case when false then 'dead' else 'live' end from #system.dual() d where true", [DiagnosticCode.MQ5008_UnreachableCode, DiagnosticCode.MQ5010_TautologicalCondition]),
        SystemCase("E06", "ENVELOPE_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy = null skip 1", [DiagnosticCode.MQ5017_NullComparison, DiagnosticCode.MQ5021_UnorderedSkip]),
        SystemCase("E07", "ENVELOPE_SURFACES", "select d.Dummy from #system.dual() d where d.Dummy not in ('x', null)", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        SystemCase("E08", "ENVELOPE_SURFACES", "select case when 1 = 1 then 'live' else 'dead' end from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        SystemCase("E09", "ENVELOPE_SURFACES", SystemSeed + " where 1 = 2", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        SystemCase("E10", "ENVELOPE_SURFACES", SystemSeed + " order by d.Dummy skip 1", []),
        SystemCase("E11", "ENVELOPE_SURFACES", SystemSeed + " order by d.Dummy take 1", []),
        SystemCase("E12", "ENVELOPE_SURFACES", SystemSeed + " ", [])
    ];

    private static IReadOnlyList<PreservationCase> PreservationCases { get; } =
    [
        new("R01", "PRESERVATION", SystemSeed + " where true", SystemSeed, [DiagnosticCode.MQ5010_TautologicalCondition]),
        new("R02", "PRESERVATION", SystemSeed + " where false", SystemSeed + " where d.Dummy = '__never__'", [DiagnosticCode.MQ5011_ContradictoryCondition]),
        new("R03", "PRESERVATION", "with dead as (select d.Dummy from #system.dual() d) select d.Dummy from #system.dual() d", SystemSeed, [DiagnosticCode.MQ5022_UnusedCte]),
        new("R04", "PRESERVATION", "let dead: int = 1; select d.Dummy from #system.dual() d", SystemSeed, [DiagnosticCode.MQ5023_UnusedScriptVariable]),
        new("R05", "PRESERVATION", "select case when false then 'dead' else 'live' end from #system.dual() d", "select 'live' as Value from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        new("R06", "PRESERVATION", SystemSeed + " skip 1", SystemSeed + " order by d.Dummy skip 1", [DiagnosticCode.MQ5021_UnorderedSkip]),
        new("R07", "PRESERVATION", "select d.Dummy from #system.dual() d where d.Dummy = null", "select d.Dummy from #system.dual() d where d.Dummy is null", [DiagnosticCode.MQ5017_NullComparison]),
        new("R08", "PRESERVATION", "select d.Dummy from #system.dual() d where d.Dummy not in ('x', null)", "select d.Dummy from #system.dual() d where d.Dummy = '__never__'", [DiagnosticCode.MQ5024_NullSensitiveNotIn]),
        new("R09", "PRESERVATION", "with dead as (select d.Dummy from #system.dual() d) select d.Dummy from #system.dual() d", SystemSeed, [DiagnosticCode.MQ5022_UnusedCte]),
        new("R10", "PRESERVATION", "select 1 ?? 'fallback' from #system.dual() d", "select 1 from #system.dual() d", [DiagnosticCode.MQ5008_UnreachableCode]),
        new("R11", "PRESERVATION", "let dead: int = 1; select d.Dummy from #system.dual() d", SystemSeed, [DiagnosticCode.MQ5023_UnusedScriptVariable]),
        new("R12", "PRESERVATION", "select 'C:\\new\\test' from #system.dual() d", null, [DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape])
    ];

    private static IReadOnlyList<ApiCase> CandidateCases { get; } =
        SurfaceCases
            .Concat(CompilationCases)
            .Concat(EnvelopeCases)
            .Concat(PreservationCases.Select(static item =>
                new ApiCase(item.Id, item.Family, item.WarningQuery, item.ExpectedWarnings, [])))
            .Select(static item => new ApiCase(
                item.Id,
                item.Family,
                item.Query,
                item.ExpectedWarnings,
                item.ExpectedWarnings.Where(static code => code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape).ToArray()))
            .ToArray();

    private sealed record ApiCase(
        string Id,
        string Family,
        string Query,
        IReadOnlyList<DiagnosticCode> ExpectedWarnings,
        IReadOnlyList<DiagnosticCode> ExpectedSyntaxWarnings);

    private sealed record PreservationCase(
        string Id,
        string Family,
        string WarningQuery,
        string? QuietQuery,
        IReadOnlyList<DiagnosticCode> ExpectedWarnings);

    private sealed record EnvelopeExpectation(
        string Id,
        string Query,
        IReadOnlyList<DiagnosticCode> ExpectedErrors);
}
