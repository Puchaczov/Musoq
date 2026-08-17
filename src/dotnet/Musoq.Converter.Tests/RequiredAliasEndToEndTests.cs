using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Exceptions;
using Musoq.Converter.Tests.Components;
using Musoq.Converter.Tests.Schema;
using Musoq.Evaluator;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Schema;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class RequiredAliasEndToEndTests
{
    private const string MissingAliasQuery =
        "select 1 from #system.dual() source cross apply source.Column take 10";

    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    public void MissingRequiredAlias_ShouldReachSyntaxAnalysisAndStopBeforeSchemaBinding()
    {
        var syntaxProvider = new CountingSchemaProvider();
        var syntax = new QueryAnalyzer(syntaxProvider).ValidateSyntax(MissingAliasQuery);
        AssertRequiredAliasDiagnostic(syntax.Diagnostics, MissingAliasQuery);
        Assert.AreEqual(0, syntaxProvider.Calls);

        var analysisProvider = new CountingSchemaProvider();
        var analysis = new QueryAnalyzer(analysisProvider).Analyze(MissingAliasQuery);
        AssertRequiredAliasDiagnostic(analysis.Diagnostics, MissingAliasQuery);
        Assert.IsTrue(analysis.HasErrors);
        Assert.AreEqual(0, analysisProvider.Calls);

        var buildProvider = new CountingSchemaProvider();
        var build = InstanceCreator.CompileWithDiagnostics(
            MissingAliasQuery,
            $"RequiredAliasStop_{Guid.NewGuid():N}",
            buildProvider,
            _loggerResolver,
            new CompilationOptions());

        AssertRequiredAliasDiagnostic(build.Errors, MissingAliasQuery);
        Assert.IsFalse(build.Succeeded);
        Assert.IsNull(build.CompiledQuery);
        Assert.IsEmpty(build.Warnings);
        Assert.HasCount(1, build.ToEnvelopes());
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, build.ToEnvelopes()[0].Code);
        Assert.AreEqual(0, buildProvider.Calls);
    }

    [TestMethod]
    public async Task MissingRequiredAlias_ShouldHaveTheSameContractAcrossCompilationSurfaces()
    {
        var defaultBuild = InstanceCreator.CompileWithDiagnostics(
            MissingAliasQuery,
            $"RequiredAliasDefault_{Guid.NewGuid():N}",
            new CountingSchemaProvider(),
            _loggerResolver);
        var optionsBuild = InstanceCreator.CompileWithDiagnostics(
            MissingAliasQuery,
            $"RequiredAliasOptions_{Guid.NewGuid():N}",
            new CountingSchemaProvider(),
            _loggerResolver,
            new CompilationOptions());
        var asyncBuild = await InstanceCreator.CompileWithDiagnosticsAsync(
            MissingAliasQuery,
            $"RequiredAliasAsync_{Guid.NewGuid():N}",
            new CountingSchemaProvider(),
            _loggerResolver,
            new CompilationOptions());
        var analyzeException = Assert.Throws<AstValidationException>(
            () => InstanceCreator.CreateForAnalyze(
                MissingAliasQuery,
                $"RequiredAliasAnalyze_{Guid.NewGuid():N}",
                new CountingSchemaProvider(),
                _loggerResolver,
                new CompilationOptions()));
        var analyzeSyntaxException = analyzeException.InnerException as SyntaxException;
        Assert.IsNotNull(analyzeSyntaxException, analyzeException.ToString());
        var analyzeDiagnostic = analyzeSyntaxException.ToDiagnostic(new SourceText(MissingAliasQuery));

        var expected = defaultBuild.Errors.Single();
        AssertRequiredAliasDiagnostic([expected], MissingAliasQuery);
        AssertRequiredAliasDiagnostic(optionsBuild.Errors, MissingAliasQuery);
        AssertRequiredAliasDiagnostic(asyncBuild.Errors, MissingAliasQuery);
        AssertRequiredAliasDiagnostic([analyzeDiagnostic], MissingAliasQuery);

        AssertEquivalent(expected, optionsBuild.Errors.Single());
        AssertEquivalent(expected, asyncBuild.Errors.Single());
        AssertEquivalent(expected, analyzeDiagnostic);
    }

    [TestMethod]
    public void CompileForExecution_WhenRequiredAliasIsMissing_ShouldPreserveTheExistingExceptionSurface()
    {
        var exception = Assert.Throws<MusoqQueryException>(
            () => InstanceCreator.CompileForExecution(
                MissingAliasQuery,
                $"RequiredAliasExecution_{Guid.NewGuid():N}",
                new CountingSchemaProvider(),
                _loggerResolver));

        Assert.HasCount(1, exception.Envelopes);
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, exception.PrimaryEnvelope.Code);
        Assert.Contains("CROSS APPLY", exception.PrimaryEnvelope.Message);
        Assert.Contains("TAKE", exception.PrimaryEnvelope.Message);
    }

    [TestMethod]
    public void CompileForInspection_WhenRequiredAliasIsMissing_ShouldPreserveTheSyntaxDiagnosticBeforePlanning()
    {
        var exception = Assert.Throws<AstValidationException>(
            () => InstanceCreator.CompileForInspection(
                MissingAliasQuery,
                $"RequiredAliasInspectionFailure_{Guid.NewGuid():N}",
                new CountingSchemaProvider(),
                _loggerResolver,
                new CompilationOptions()));
        var syntaxException = exception.InnerException as SyntaxException;
        Assert.IsNotNull(syntaxException, exception.ToString());

        var diagnostic = syntaxException.ToDiagnostic(new SourceText(MissingAliasQuery));
        AssertRequiredAliasDiagnostic([diagnostic], MissingAliasQuery);
    }

    [TestMethod]
    public void RepeatedMalformedCompilation_ShouldReplayTheSameDiagnosticWithoutChangingTheFailureContract()
    {
        var first = InstanceCreator.CompileWithDiagnostics(
            MissingAliasQuery,
            $"RequiredAliasCache_{Guid.NewGuid():N}",
            new CountingSchemaProvider(),
            _loggerResolver,
            new CompilationOptions());
        var second = InstanceCreator.CompileWithDiagnostics(
            MissingAliasQuery,
            $"RequiredAliasCache_{Guid.NewGuid():N}",
            new CountingSchemaProvider(),
            _loggerResolver,
            new CompilationOptions());

        AssertRequiredAliasDiagnostic(first.Errors, MissingAliasQuery);
        AssertRequiredAliasDiagnostic(second.Errors, MissingAliasQuery);
        AssertEquivalent(first.Errors.Single(), second.Errors.Single());
        Assert.IsNull(first.CompiledQuery);
        Assert.IsNull(second.CompiledQuery);
        Assert.IsEmpty(first.Warnings);
        Assert.IsEmpty(second.Warnings);
    }

    [TestMethod]
    public void ValidJoinAndApplyForms_ShouldCompileAndExecuteWithoutRequiredAliasDiagnostics()
    {
        foreach (var testCase in CreateValidCases())
        {
            var build = InstanceCreator.CompileWithDiagnostics(
                testCase.Query,
                $"RequiredAliasValid_{testCase.Name}_{Guid.NewGuid():N}",
                testCase.ProviderFactory(),
                _loggerResolver,
                new CompilationOptions());

            Assert.IsTrue(build.Succeeded, FormatDiagnostics(build.Diagnostics, testCase.Query));
            Assert.IsFalse(build.Errors.Any(static diagnostic =>
                diagnostic.Code == DiagnosticCode.MQ2035_MissingRequiredAlias),
                FormatDiagnostics(build.Diagnostics, testCase.Query));
            Assert.IsNotNull(build.CompiledQuery);

            using var table = build.CompiledQuery.Run();
            Assert.IsNotNull(table, testCase.Name);
            Assert.IsTrue(table.Count >= 0, testCase.Name);
        }
    }

    [TestMethod]
    public void ValidCorrectedApplyForms_ShouldHaveStableInspectionAndRuntimeContracts()
    {
        var queries = new[]
        {
            "select i.Name, n.Value from #apply.items() i cross apply i.Numbers n order by i.Name, n.Value",
            "select i.Name, n.Value from #apply.items() i cross apply i.Numbers AS n order by i.Name, n.Value",
            "select i.Name, n.Value from #apply.items() i cross apply i.Numbers [n] order by i.Name, n.Value"
        };

        foreach (var query in queries)
        {
            var analysis = new QueryAnalyzer(CreateApplyProvider()).Analyze(query);
            Assert.IsFalse(analysis.HasErrors, FormatDiagnostics(analysis.Diagnostics, query));
            Assert.IsEmpty(analysis.Warnings, FormatDiagnostics(analysis.Diagnostics, query));

            var build = InstanceCreator.CompileWithDiagnostics(
                query,
                $"RequiredAliasCorrected_{Guid.NewGuid():N}",
                CreateApplyProvider(),
                _loggerResolver,
                new CompilationOptions());
            var inspection = InstanceCreator.CompileForInspection(
                query,
                $"RequiredAliasInspection_{Guid.NewGuid():N}",
                CreateApplyProvider(),
                _loggerResolver,
                new CompilationOptions());

            Assert.IsTrue(build.Succeeded, FormatDiagnostics(build.Diagnostics, query));
            Assert.IsEmpty(build.Diagnostics, FormatDiagnostics(build.Diagnostics, query));
            Assert.IsEmpty(inspection.Diagnostics, FormatDiagnostics(inspection.Diagnostics, query));
            Assert.IsFalse(string.IsNullOrWhiteSpace(inspection.GeneratedCSharpCode));

            using var table = build.CompiledQuery!.Run();
            CollectionAssert.AreEqual(
                new[] { "left:1", "left:2", "right:3" },
                table.Select(row => $"{row[0]}:{row[1]}").ToArray());
        }
    }

    private static IReadOnlyList<ValidAliasCase> CreateValidCases()
    {
        return
        [
            new("inner-join", "select a.Dummy from #system.dual() a inner join #system.dual() b on a.Dummy = b.Dummy", static () => new SystemSchemaProvider()),
            new("left-join", "select a.Dummy from #system.dual() a left outer join #system.dual() b on a.Dummy = b.Dummy", static () => new SystemSchemaProvider()),
            new("right-join", "select a.Dummy from #system.dual() a right outer join #system.dual() b on a.Dummy = b.Dummy", static () => new SystemSchemaProvider()),
            new("full-join", "select a.Dummy from #system.dual() a full outer join #system.dual() b on a.Dummy = b.Dummy", static () => new SystemSchemaProvider()),
            new("cross-join", "select a.Dummy from #system.dual() a cross join #system.dual() b", static () => new SystemSchemaProvider()),
            new("semi-join", "select a.Dummy from #system.dual() a semi join #system.dual() b on a.Dummy = b.Dummy", static () => new SystemSchemaProvider()),
            new("anti-join", "select a.Dummy from #system.dual() a anti join #system.dual() b on a.Dummy = b.Dummy", static () => new SystemSchemaProvider()),
            new("asof-join", "select a.Dummy from #system.dual() a asof join #system.dual() b on a.Dummy >= b.Dummy", static () => new SystemSchemaProvider()),
            new("asof-left-join", "select a.Dummy from #system.dual() a asof left join #system.dual() b on a.Dummy >= b.Dummy", static () => new SystemSchemaProvider()),
            new("cross-apply-property", "select i.Name, n.Value from #apply.items() i cross apply i.Numbers n", CreateApplyProvider),
            new("outer-apply-property", "select i.Name, n.Value from #apply.items() i outer apply i.Numbers n", CreateApplyProvider),
            new("cross-apply-schema", "select i.Name, r.Line from #apply.items() i cross apply #apply.related(i.Name) r", CreateApplyProvider),
            new("outer-apply-schema", "select i.Name, r.Line from #apply.items() i outer apply #apply.related(i.Numbers) r", CreateApplyProvider),
            new("apply-with-ordinality", "select i.Name, n.Value, n.Ordinal from #apply.items() i cross apply i.Numbers n with ordinality", CreateApplyProvider),
            new("function-apply", @"
                text LogLine {
                    Level: until ' ',
                    Message: rest
                };
                select i.Name, l.Level from #apply.items() i cross apply Parse<LogLine>(i.Line) l", CreateApplyProvider),
            new("derived-and-values", "select sub.Dummy, marker.Label from (select d.Dummy from #system.dual() d) sub cross join values { { Label: 'x' } } marker", static () => new SystemSchemaProvider()),
            new("values-and-schema", "select seed.Value, d.Dummy from values { { Value: 1 } } seed cross join #system.dual() d", static () => new SystemSchemaProvider()),
            new("natural-cte", "with source as (select d.Dummy from #system.dual() d) select source.Dummy, rightSource.Dummy from source cross join #system.dual() rightSource", static () => new SystemSchemaProvider()),
            new("chained-apply", "select i.Name, n.Value, m.Value from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m", CreateApplyProvider)
        ];
    }

    private static ISchemaProvider CreateApplyProvider()
    {
        return new ApplyCandidateSchemaProvider(
        [
            new ApplyCandidateEntity { Name = "left", Line = "INFO ready", Numbers = [1, 2] },
            new ApplyCandidateEntity { Name = "right", Line = "WARN retry", Numbers = [3] }
        ]);
    }

    private static void AssertRequiredAliasDiagnostic(IEnumerable<Diagnostic> diagnostics, string query)
    {
        var materialized = diagnostics.ToArray();
        Assert.HasCount(1, materialized, FormatDiagnostics(materialized, query));
        var diagnostic = materialized[0];
        Assert.AreEqual(DiagnosticCode.MQ2035_MissingRequiredAlias, diagnostic.Code);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual("The CROSS APPLY source requires an alias before TAKE.", diagnostic.Message);

        var sourceEnd = query.IndexOf("source.Column", StringComparison.Ordinal) + "source.Column".Length;
        Assert.AreEqual(sourceEnd, diagnostic.Location.Offset);
        Assert.AreEqual(sourceEnd, diagnostic.EndLocation.Offset);
        Assert.AreEqual(0, diagnostic.Span.Length);
        Assert.IsFalse(materialized.Any(static item =>
            item.Code is DiagnosticCode.MQ2001_UnexpectedToken or DiagnosticCode.MQ2030_UnsupportedSyntax));
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
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics, string query)
    {
        return $"{query}{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToDetailedString()))}";
    }

    private sealed record ValidAliasCase(
        string Name,
        string Query,
        Func<ISchemaProvider> ProviderFactory);

    private sealed class CountingSchemaProvider : ISchemaProvider
    {
        public int Calls { get; private set; }

        public ISchema GetSchema(string schema)
        {
            Calls++;
            throw new InvalidOperationException($"Schema lookup should not occur for malformed syntax: {schema}");
        }
    }
}
