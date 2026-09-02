using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Helpers;
using Musoq.Schema.Managers;
using Musoq.Schema.Reflection;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class AdvisoryWarningPropagationTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void AnalyzerAndDiagnosticCompilation_ShouldExposeTheSameWarningContract()
    {
        foreach (var testCase in CreateWarningCases())
        {
            var analysis = new QueryAnalyzer(testCase.ProviderFactory()).Analyze(testCase.Query);
            Assert.IsFalse(analysis.HasErrors, FormatDiagnostics(analysis.Diagnostics, testCase.Query));

            var analysisWarning = analysis.Warnings.SingleOrDefault(warning => warning.Code == testCase.Code);
            Assert.IsNotNull(analysisWarning, FormatDiagnostics(analysis.Diagnostics, testCase.Query));

            var build = InstanceCreator.CompileWithDiagnostics(
                testCase.Query,
                $"AdvisoryPropagation_{testCase.Code}_{Guid.NewGuid():N}",
                testCase.ProviderFactory(),
                _loggerResolver,
                new CompilationOptions());

            Assert.IsTrue(build.Succeeded, FormatDiagnostics(build.Diagnostics, testCase.Query));
            Assert.IsEmpty(build.Errors, FormatDiagnostics(build.Diagnostics, testCase.Query));
            var compilationWarning = build.Warnings.SingleOrDefault(warning => warning.Code == testCase.Code);
            Assert.IsNotNull(compilationWarning, FormatDiagnostics(build.Diagnostics, testCase.Query));
            AssertEquivalent(analysisWarning, compilationWarning);
        }
    }

    [TestMethod]
    public async Task DiagnosticCompilation_ShouldPropagateWarningsThroughOptionsAsyncInspectionAndAnalyzeSurfaces()
    {
        const string query = @"select 'C:\new\test' from #system.dual()";
        var provider = new SystemSchemaProvider();
        var options = new CompilationOptions();

        var defaultBuild = InstanceCreator.CompileWithDiagnostics(
            query,
            $"AdvisoryDefault_{Guid.NewGuid():N}",
            provider,
            _loggerResolver);
        var optionsBuild = InstanceCreator.CompileWithDiagnostics(
            query,
            $"AdvisoryOptions_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            options);
        var asyncBuild = await InstanceCreator.CompileWithDiagnosticsAsync(
            query,
            $"AdvisoryAsync_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            options);
        var inspection = InstanceCreator.CompileForInspection(
            query,
            $"AdvisoryInspection_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            options);
        var analyzeItems = InstanceCreator.CreateForAnalyze(
            query,
            $"AdvisoryAnalyze_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            options);

        var expected = defaultBuild.Warnings.Single(warning =>
            warning.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape);
        foreach (var diagnostics in new[]
                 {
                     optionsBuild.Warnings,
                     asyncBuild.Warnings,
                     inspection.Warnings,
                     analyzeItems.DiagnosticContext.Warnings.ToList()
                 })
        {
            var actual = diagnostics.Single(warning =>
                warning.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape);
            AssertEquivalent(expected, actual);
        }

        Assert.IsTrue(defaultBuild.Succeeded, FormatDiagnostics(defaultBuild.Diagnostics, query));
        Assert.IsTrue(optionsBuild.Succeeded, FormatDiagnostics(optionsBuild.Diagnostics, query));
        Assert.IsTrue(asyncBuild.Succeeded, FormatDiagnostics(asyncBuild.Diagnostics, query));
        Assert.IsNotNull(inspection.ExecutionPlan);
        Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode));
        Assert.IsNotNull(analyzeItems.SemanticArtifacts);
    }

    [TestMethod]
    public async Task SemanticPatternWarning_ShouldHaveTheSameContractAcrossAllCompilationSurfaces()
    {
        const string query = @"select d.Dummy from #system.dual() d where d.Dummy rlike '\bword\b'";
        var options = new CompilationOptions();

        var analysis = new QueryAnalyzer(new SystemSchemaProvider()).Analyze(query);
        var defaultBuild = InstanceCreator.CompileWithDiagnostics(
            query,
            $"PatternDefault_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver);
        var optionsBuild = InstanceCreator.CompileWithDiagnostics(
            query,
            $"PatternOptions_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            options);
        var asyncBuild = await InstanceCreator.CompileWithDiagnosticsAsync(
            query,
            $"PatternAsync_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            options);
        var inspection = InstanceCreator.CompileForInspection(
            query,
            $"PatternInspection_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            options);
        var analyzeItems = InstanceCreator.CreateForAnalyze(
            query,
            $"PatternAnalyze_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            options);

        var expected = analysis.Warnings.Single(warning =>
            warning.Code == DiagnosticCode.MQ5015_SuspiciousRegexEscape);
        var actualDiagnostics = new[]
        {
            defaultBuild.Warnings,
            optionsBuild.Warnings,
            asyncBuild.Warnings,
            inspection.Warnings,
            analyzeItems.DiagnosticContext.Warnings.ToList()
        };

        Assert.IsFalse(analysis.HasErrors, FormatDiagnostics(analysis.Diagnostics, query));
        Assert.IsTrue(defaultBuild.Succeeded, FormatDiagnostics(defaultBuild.Diagnostics, query));
        Assert.IsTrue(optionsBuild.Succeeded, FormatDiagnostics(optionsBuild.Diagnostics, query));
        Assert.IsTrue(asyncBuild.Succeeded, FormatDiagnostics(asyncBuild.Diagnostics, query));
        Assert.IsNotNull(inspection.ExecutionPlan);
        Assert.IsNotNull(analyzeItems.SemanticArtifacts);
        Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode));

        foreach (var diagnostics in actualDiagnostics)
            AssertEquivalent(expected, diagnostics.Single(warning =>
                warning.Code == DiagnosticCode.MQ5015_SuspiciousRegexEscape));

        using var table = defaultBuild.CompiledQuery!.Run();
        Assert.AreEqual("d.Dummy", table.Columns.First().ColumnName);
    }

    [TestMethod]
    public void ValidateSyntax_ShouldExposeOnlyLexicalWarningsWhileAnalyzeAddsBindingWarnings()
    {
        const string rooted = @"select 'C:\new\test' from #system.dual()";
        const string relative = @"select f.Path from #raw.files('some\text', true) f";

        var rootedSyntax = new QueryAnalyzer(new SystemSchemaProvider()).ValidateSyntax(rooted);
        var relativeSyntax = new QueryAnalyzer(new RawStringLiteralSchemaProvider()).ValidateSyntax(relative);
        var relativeAnalysis = new QueryAnalyzer(new RawStringLiteralSchemaProvider()).Analyze(relative);

        Assert.IsFalse(rootedSyntax.HasErrors, FormatDiagnostics(rootedSyntax.Diagnostics, rooted));
        Assert.AreEqual(
            1,
            rootedSyntax.Warnings.Count(static warning =>
                warning.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape));
        Assert.IsEmpty(relativeSyntax.Warnings, FormatDiagnostics(relativeSyntax.Diagnostics, relative));
        Assert.AreEqual(
            1,
            relativeAnalysis.Warnings.Count(static warning =>
                warning.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape));
        Assert.IsFalse(relativeAnalysis.HasErrors, FormatDiagnostics(relativeAnalysis.Diagnostics, relative));
    }

    [TestMethod]
    public void WarningOnlyCompilation_ShouldRemainRunnableAndKeepExistingRuntimeSemantics()
    {
        const string query = @"select 'C:\new\test' from #system.dual()";

        var diagnosticBuild = InstanceCreator.CompileWithDiagnostics(
            query,
            $"AdvisoryRunnableDiagnostics_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver);
        var convenienceQuery = InstanceCreator.CompileForExecution(
            query,
            $"AdvisoryRunnableConvenience_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver);

        Assert.IsTrue(diagnosticBuild.Succeeded, FormatDiagnostics(diagnosticBuild.Diagnostics, query));
        Assert.IsFalse(diagnosticBuild.HasErrors);
        Assert.IsNotNull(diagnosticBuild.CompiledQuery);
        Assert.IsEmpty(diagnosticBuild.Errors);
        Assert.IsEmpty(diagnosticBuild.ToEnvelopes());
        Assert.AreEqual(1, diagnosticBuild.Warnings.Count);

        using var diagnosticTable = diagnosticBuild.CompiledQuery.Run();
        using var convenienceTable = convenienceQuery.Run();
        Assert.AreEqual(diagnosticTable[0][0], convenienceTable[0][0]);
        Assert.AreEqual("C:\n" + "ew\t" + "est", diagnosticTable[0][0]);
    }

    [TestMethod]
    public void AllDiagnosticEnvelopes_ShouldIncludeWarningsWhileToEnvelopesRemainsErrorOnly()
    {
        const string query = @"select 'C:\new\test' from #system.dual()";

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"AdvisoryAllEnvelopes_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            new CompilationOptions());

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics, query));
        Assert.IsEmpty(result.ToEnvelopes());

        var all = result.ToAllEnvelopes();
        Assert.HasCount(1, all);
        Assert.AreEqual(DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape, all[0].Code);
        Assert.AreEqual(DiagnosticSeverity.Warning, all[0].Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, all[0].Phase);
        Assert.AreEqual(result.Warnings[0].Location.Offset, all[0].Offset);
        Assert.AreEqual(result.Warnings[0].EndLocation.Offset, all[0].EndOffset);
        Assert.AreEqual(result.Warnings[0].Span.Length, all[0].Length);
    }

    [TestMethod]
    public async Task WarningCacheReplay_ShouldKeepWarningsImmutableAndRootsIndependentForConcurrentCallers()
    {
        const string query = @"select 'C:\new\test' from #system.dual()";

        var results = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(index => Task.Run(() => InstanceCreator.CompileWithDiagnostics(
                query,
                $"AdvisoryConcurrent_{index}_{Guid.NewGuid():N}",
                new SystemSchemaProvider(),
                _loggerResolver,
                new CompilationOptions()))));

        Assert.IsTrue(results.All(static result => result.Succeeded));
        Assert.IsTrue(results.All(static result => result.Warnings.Count == 1));
        Assert.IsTrue(results.All(static result =>
            result.Warnings[0].Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape));
        Assert.AreEqual(results.Length, results.Select(static result => result.Warnings).Distinct().Count());
        Assert.AreEqual(
            1,
            results.Select(static result =>
                    (result.Warnings[0].Code, result.Warnings[0].Message, result.Warnings[0].Location.Offset,
                        result.Warnings[0].EndLocation.Offset))
                .Distinct()
                .Count());
    }

    [TestMethod]
    public void FailedParsingAndBinding_ShouldNotPoisonLaterWarningCacheRequests()
    {
        var invalidQuery = $"select Missing from #system.dual() where '{Guid.NewGuid():N}' = null";
        var failed = InstanceCreator.CompileWithDiagnostics(
            invalidQuery,
            $"AdvisoryFailed_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            new CompilationOptions());

        Assert.IsFalse(failed.Succeeded);
        Assert.IsTrue(failed.Errors.Any(static diagnostic =>
            diagnostic.Code == DiagnosticCode.MQ3001_UnknownColumn));

        const string validQuery = @"select 'C:\new\test' from #system.dual()";
        var recovered = InstanceCreator.CompileWithDiagnostics(
            validQuery,
            $"AdvisoryRecovered_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            new CompilationOptions());

        Assert.IsTrue(recovered.Succeeded, FormatDiagnostics(recovered.Diagnostics, validQuery));
        Assert.AreEqual(
            1,
            recovered.Warnings.Count(static warning =>
                warning.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape));
    }

    [TestMethod]
    public void MixedWarnings_ShouldBeSourceOrderedAndSpecificWarningsShouldNotDuplicate()
    {
        const string query = @"select 'C:\new\test' from #system.dual() where true skip 1";

        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"AdvisoryMixed_{Guid.NewGuid():N}",
            new SystemSchemaProvider(),
            _loggerResolver,
            new CompilationOptions());

        Assert.IsTrue(result.Succeeded, FormatDiagnostics(result.Diagnostics, query));
        Assert.IsEmpty(result.Errors, FormatDiagnostics(result.Diagnostics, query));
        Assert.AreEqual(
            1,
            result.Warnings.Count(static warning =>
                warning.Code == DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape));
        Assert.AreEqual(
            1,
            result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5010_TautologicalCondition));
        Assert.AreEqual(
            1,
            result.Warnings.Count(static warning => warning.Code == DiagnosticCode.MQ5021_UnorderedSkip));

        var ordered = result.Warnings.OrderBy(static warning => warning.Location.Offset).ToArray();
        CollectionAssert.AreEqual(ordered, result.Warnings.ToArray());
        CollectionAssert.AreEqual(
            new[]
            {
                DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape,
                DiagnosticCode.MQ5010_TautologicalCondition,
                DiagnosticCode.MQ5021_UnorderedSkip
            },
            ordered.Select(static warning => warning.Code).ToArray(),
            FormatDiagnostics(result.Diagnostics, query));
    }

    private static IReadOnlyList<WarningCase> CreateWarningCases()
    {
        return
        [
            new(
                @"select 'C:\new\test' from #system.dual()",
                DiagnosticCode.MQ5014_SuspiciousOrdinaryStringEscape,
                static () => new SystemSchemaProvider()),
            new(
                @"select d.Dummy from #system.dual() d where d.Dummy rlike '\bword\b'",
                DiagnosticCode.MQ5015_SuspiciousRegexEscape,
                static () => new SystemSchemaProvider()),
            new(
                @"select d.Dummy from #system.dual() d where d.Dummy like '*.log'",
                DiagnosticCode.MQ5016_GlobWildcardInLike,
                static () => new SystemSchemaProvider()),
            new(
                @"select d.Dummy from #system.dual() d where d.Dummy = null",
                DiagnosticCode.MQ5017_NullComparison,
                static () => new SystemSchemaProvider()),
            new(
                @"select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy is null",
                DiagnosticCode.MQ5018_AmbiguousOuterJoinNullCheck,
                static () => new NullableJoinSchemaProvider()),
            new(
                @"select a.Dummy, b.Dummy from #join.items() a left join #join.items() b on a.Dummy = b.Dummy where b.Dummy = 'match'",
                DiagnosticCode.MQ5019_NullRejectingOuterJoinFilter,
                static () => new NullableJoinSchemaProvider()),
            new(
                @"select d.Dummy from #system.dual() d skip 1",
                DiagnosticCode.MQ5021_UnorderedSkip,
                static () => new SystemSchemaProvider()),
            new(
                @"with dead as (select d.Dummy from #system.dual() d) select d.Dummy from #system.dual() d",
                DiagnosticCode.MQ5022_UnusedCte,
                static () => new SystemSchemaProvider()),
            new(
                @"let dead: int = 1; select d.Dummy from #system.dual() d",
                DiagnosticCode.MQ5023_UnusedScriptVariable,
                static () => new SystemSchemaProvider()),
            new(
                @"select case when false then 'dead' else 'live' end from #system.dual()",
                DiagnosticCode.MQ5008_UnreachableCode,
                static () => new SystemSchemaProvider()),
            new(
                @"select d.Dummy from #system.dual() d where true",
                DiagnosticCode.MQ5010_TautologicalCondition,
                static () => new SystemSchemaProvider()),
            new(
                @"select d.Dummy from #system.dual() d where 1 = 2",
                DiagnosticCode.MQ5011_ContradictoryCondition,
                static () => new SystemSchemaProvider()),
            new(
                @"param (moment: datetime); select 1 from #system.dual() where $moment = '01/02/2026'",
                DiagnosticCode.MQ5003_ImplicitTypeConversion,
                static () => new SystemSchemaProvider())
        ];
    }

    private static void AssertEquivalent(Diagnostic expected, Diagnostic actual)
    {
        Assert.AreEqual(expected.Code, actual.Code);
        Assert.AreEqual(expected.Severity, actual.Severity);
        Assert.AreEqual(expected.Phase, actual.Phase);
        Assert.AreEqual(expected.Message, actual.Message);
        Assert.AreEqual(expected.Location.Offset, actual.Location.Offset);
        Assert.AreEqual(expected.EndLocation.Offset, actual.EndLocation.Offset);
        Assert.AreEqual(expected.Span.Start, actual.Span.Start);
        Assert.AreEqual(expected.Span.Length, actual.Span.Length);
        Assert.AreEqual(expected.SourceKind, actual.SourceKind);
        Assert.AreEqual(expected.CorrelationId, actual.CorrelationId);
        CollectionAssert.AreEquivalent(
            expected.Arguments.Select(static argument => $"{argument.Key}={argument.Value}").ToArray(),
            actual.Arguments.Select(static argument => $"{argument.Key}={argument.Value}").ToArray());
        Assert.HasCount(expected.RelatedLocations.Count, actual.RelatedLocations);
        Assert.HasCount(expected.SuggestedFixes.Count, actual.SuggestedFixes);
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics, string query)
    {
        return $"{query}{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))}";
    }

    private sealed record WarningCase(
        string Query,
        DiagnosticCode Code,
        Func<ISchemaProvider> ProviderFactory);

    public sealed class NullableJoinSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            return new NullableJoinSchema();
        }
    }

    public sealed class NullableJoinSchema : SchemaBase
    {
        private const string Items = "items";

        public NullableJoinSchema()
            : base("join", CreateLibrary())
        {
        }

        public override ISchemaTable GetTableByName(
            string name,
            SourceMetadataContext metadataContext,
            params object?[] parameters)
        {
            return new NullableJoinTable();
        }

        public override RowSource<T> GetRowSource<T>(
            string name,
            SourceExecutionContext executionContext,
            params object?[] parameters)
        {
            return EnsureSourceType<T, NullableJoinEntity>(
                name,
                new NullableJoinRowSource());
        }

        public override SchemaMethodInfo[] GetConstructors()
        {
            return TypeHelper.GetSchemaMethodInfosForType<NullableJoinRowSource>(Items);
        }

        public override SchemaMethodInfo[] GetRawConstructors(
            string methodName,
            SourceMetadataContext metadataContext)
        {
            return string.Equals(methodName, Items, StringComparison.OrdinalIgnoreCase)
                ? GetConstructors()
                : [];
        }

        private static MethodsAggregator CreateLibrary()
        {
            var methodsManager = new MethodsManager();
            methodsManager.RegisterLibraries(new EmptyLibrary());
            return new MethodsAggregator(methodsManager);
        }
    }

    public sealed class NullableJoinEntity
    {
        public string? Dummy { get; } = null;
    }

    public sealed class NullableJoinRowSource : RowSourceBase<NullableJoinEntity>
    {
        protected override void CollectChunks(IChunkWriter<NullableJoinEntity> writer)
        {
            writer.Write([new NullableJoinEntity()]);
        }
    }

    public sealed class NullableJoinTable : ISchemaTable
    {
        public ISchemaColumn[] Columns { get; } =
        [
            new SchemaColumn(nameof(NullableJoinEntity.Dummy), 0, typeof(string))
        ];

        public SchemaTableMetadata Metadata { get; } = new(typeof(NullableJoinEntity));

        public ISchemaColumn? GetColumnByName(string name)
        {
            return Columns.SingleOrDefault(column => column.ColumnName == name);
        }

        public ISchemaColumn[] GetColumnsByName(string name)
        {
            return Columns.Where(column => column.ColumnName == name).ToArray();
        }
    }
}
