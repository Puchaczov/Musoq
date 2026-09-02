using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCouple036CoupleContractTests : UnknownQueryTestsBase
{
    [TestMethod]
    public void CoupleTableOnly_ShouldUseTheDeclaredTableShape()
    {
        const string query =
            "table Shape { Token: string };" +
            "couple #settings.items with table Shape as Source;" +
            "select Token from Source();";

        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: false),
            LoggerResolver,
            TestCompilationOptions);

        var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Token", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [string.Empty]);
    }

    [TestMethod]
    public void CoupleSettingsOnly_ShouldInferTheUnderlyingTableAndResolveTheProfile()
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new Couple036ProfileResolver();
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

        var compiled = InstanceCreator.CompileForExecution(
            "couple #settings.items with settings blue as Source; select Token from Source();",
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            options);

        var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Token", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["blue-token"]);
        CollectionAssert.AreEqual(new[] { "blue" }, resolver.ProfileNames.ToArray());
    }

    [TestMethod]
    [DataRow(
        "table Shape { Token: string }; couple #settings.items with table Shape and settings blue as Source;",
        "blue")]
    [DataRow(
        "table Shape { Token: string }; couple #settings.items with settings red and table Shape as Source;",
        "red")]
    public void CoupleTableAndSettings_BothOptionOrders_ShouldUseShapeAndProfile(
        string prefix,
        string expectedProfile)
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new Couple036ProfileResolver();
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);

        var compiled = InstanceCreator.CompileForExecution(
            prefix + " select Token from Source();",
            Guid.NewGuid().ToString(),
            provider,
            LoggerResolver,
            options);

        var table = compiled.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Token", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [$"{expectedProfile}-token"]);
        CollectionAssert.AreEqual(new[] { expectedProfile }, resolver.ProfileNames.ToArray());
    }

    [TestMethod]
    public void CoupleBeforeCteAndQuery_ShouldPreserveTheDeclaredSourceColumns()
    {
        const string query =
            "table Shape { Value: int };" +
            "couple #test.whatever with table Shape as Source;" +
            "with Filtered as (select Value from Source())" +
            "select Value from Filtered;";

        var table = CreateAndRunVirtualMachine(
                query,
                [new Dictionary<string, object?> { ["Value"] = 7 }])
            .Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, [7]);
    }

    [TestMethod]
    public void CoupleAliases_ShouldBeUniqueIgnoringCaseAndHighlightTheDuplicateDefinition()
    {
        const string query =
            "table Shape { Value: int };" +
            "couple #test.whatever with table Shape as Source;" +
            "couple #test.whatever with table Shape as source;" +
            "select 1 from #test.whatever();";

        AssertCoupleDiagnostic(
            query,
            DiagnosticCode.MQ3021_DuplicateAlias,
            SpanOf(query, "couple #test.whatever with table Shape as source"),
            "duplicate COUPLE alias");
    }

    [TestMethod]
    public void CoupledAliasUsedBeforeItsCouple_ShouldReportInvalidStatementOrder()
    {
        const string query =
            "select 1 from Source();" +
            "couple #test.whatever with settings profile as Source;";

        AssertCoupleDiagnostic(
            query,
            DiagnosticCode.MQ3102_InvalidStatementOrder,
            SpanOf(query, "couple #test.whatever with settings profile as Source"),
            "alias visibility");
    }

    [TestMethod]
    public void CoupleAfterCte_ShouldReportInvalidStatementOrder()
    {
        const string query =
            "with Data as (select 1 from #test.whatever()) select 1 from Data;" +
            "couple #test.whatever with settings profile as Source;" +
            "select 1 from #test.whatever();";

        AssertCoupleDiagnostic(
            query,
            DiagnosticCode.MQ3102_InvalidStatementOrder,
            SpanOf(query, "couple #test.whatever with settings profile as Source"),
            "COUPLE after CTE");
    }

    [TestMethod]
    public void TableAfterCouple_ShouldReportInvalidStatementOrder()
    {
        const string query =
            "couple #test.whatever with settings profile as Source;" +
            "table Later { Value: int };" +
            "select 1 from #test.whatever();";

        AssertCoupleDiagnostic(
            query,
            DiagnosticCode.MQ3102_InvalidStatementOrder,
            SpanOf(query, "table Later { Value: int }"),
            "TABLE after COUPLE");
    }

    private static void AssertCoupleDiagnostic(
        string query,
        DiagnosticCode expectedCode,
        TextSpan expectedSpan,
        string context)
    {
        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(result, expectedCode, context);

        Assert.AreEqual(expectedSpan, diagnostic.Span, context);
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedCode, envelope.Code, context);
        Assert.AreEqual(DiagnosticPhaseMapping.FromCode(expectedCode), envelope.Phase, context);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind, context);
        Assert.AreEqual(expectedSpan.Start, envelope.Offset, context);
        Assert.AreEqual(expectedSpan.Length, envelope.Length, context);
        Assert.IsNotEmpty(envelope.SuggestedFixes, context);
        Assert.IsNotEmpty(envelope.Actions, context);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: new CompilationOptions(usePrimitiveTypeValidation: false))
            .Analyze(query);
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in '{query}'.");
        return new TextSpan(start, text.Length);
    }

    private sealed class Couple036ProfileResolver : ISourceRuntimeSettingsResolver
    {
        public List<string?> ProfileNames { get; } = [];

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            ProfileNames.Add(request.ProfileName);
            return new Dictionary<string, string>
            {
                ["TOKEN"] = $"{request.ProfileName}-token"
            };
        }
    }
}
