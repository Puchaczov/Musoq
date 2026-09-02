using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticCouple038SourceRuntimeSettingsTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void CoupledAliases_WithDifferentProfiles_KeepSettingsIsolatedAcrossSourcePhases()
    {
        var provider = new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true);
        var resolver = new ProfileSettingsResolver();
        var options = new CompilationOptions(sourceRuntimeSettingsResolver: resolver);
        const string query =
            "table Shape { Token: string };" +
            "couple #settings.items with table Shape and settings prod as ProdItems;" +
            "couple #settings.items with settings staging as StagingItems;" +
            "select p.Token, s.Token from ProdItems() p inner join StagingItems() s on 1 = 1;";

        var compiled = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider,
            new TestsLoggerResolver(),
            options);

        var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("p.Token", typeof(string)),
            ("s.Token", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [new object?[] { "prod-token", "staging-token" }]);

        Assert.HasCount(2, resolver.Requests);
        Assert.IsTrue(resolver.Requests.Any(request =>
            request.ProfileName == "prod" && request.Alias == "p"));
        Assert.IsTrue(resolver.Requests.Any(request =>
            request.ProfileName == "staging" && request.Alias == "s"));
        Assert.HasCount(
            2,
            resolver.Requests.Select(request => request.SourceContextId).Distinct(StringComparer.Ordinal));

        AssertSettingsReachedPhase(provider.Schema.MetadataSettings, "prod-token");
        AssertSettingsReachedPhase(provider.Schema.MetadataSettings, "staging-token");
        AssertSettingsReachedPhase(provider.Schema.PlanSettings, "prod-token");
        AssertSettingsReachedPhase(provider.Schema.PlanSettings, "staging-token");
        AssertSettingsReachedPhase(provider.Schema.ExecutionSettings, "prod-token");
        AssertSettingsReachedPhase(provider.Schema.ExecutionSettings, "staging-token");
    }

    [TestMethod]
    public void CoupledAlias_WhenRequiredSettingIsMissing_ReportsStructuredRedactedDiagnostic()
    {
        const string query =
            "couple #settings.items with settings prod as ProdItems;" +
            "select Token from proditems();";

        var result = new QueryAnalyzer(
                new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true),
                compilationOptions: CompilationOptions)
            .Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ3067_MissingSourceRuntimeSetting,
            "missing coupled source runtime setting");
        var expectedSpan = SpanOf(query, "()");

        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.Contains("TOKEN", diagnostic.Message);
        Assert.DoesNotContain("prod-token", diagnostic.Message);
        Assert.DoesNotContain("secret", diagnostic.Message, StringComparison.OrdinalIgnoreCase);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(DiagnosticCode.MQ3067_MissingSourceRuntimeSetting, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.AreEqual(expectedSpan.Start, envelope.Offset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    public void DescSettings_CoupledAliasIsCaseInsensitiveAndNeverDisclosesResolvedValues()
    {
        var resolver = new ProfileSettingsResolver();
        var compiled = InstanceCreator.CompileForExecution(
            "couple #settings.items with settings prod as ProdItems;desc settings proditems;",
            Guid.NewGuid().ToString(),
            new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true),
            new TestsLoggerResolver(),
            new CompilationOptions(sourceRuntimeSettingsResolver: resolver));

        var table = compiled.Run();

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Required", typeof(bool)),
            ("Secret", typeof(bool)),
            ("Phases", typeof(string)),
            ("Status", typeof(string)),
            ("Description", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            [
                ["OPTIONAL_TOKEN", false, false, "All", "Provided", "Optional token override."],
                ["TOKEN", true, true, "All", "Provided", "Token used by the settings source."]
            ]);

        Assert.IsTrue(resolver.Requests.Any(request => request.ProfileName == "prod"));
        Assert.IsFalse(table.Rows.SelectMany(row => row.Values).Any(value =>
            Equals(value, "prod-token") || Equals(value, "prod-optional")));
    }

    private static void AssertSettingsReachedPhase(
        IEnumerable<IReadOnlyDictionary<string, string>> settingsByInvocation,
        string expectedToken)
    {
        Assert.IsTrue(
            settingsByInvocation.Any(settings =>
                settings.TryGetValue("TOKEN", out var token) && token == expectedToken),
            $"Expected TOKEN={expectedToken} in source settings.");
    }

    private static TextSpan SpanOf(string query, string text)
    {
        var start = query.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in '{query}'.");
        return new TextSpan(start, text.Length);
    }

    private sealed class ProfileSettingsResolver : ISourceRuntimeSettingsResolver
    {
        public List<(string? ProfileName, string SourceContextId, string Alias)> Requests { get; } = [];

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            Requests.Add((request.ProfileName, request.Identity.SourceContextId, request.Identity.Alias));
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TOKEN"] = $"{request.ProfileName}-token",
                ["OPTIONAL_TOKEN"] = $"{request.ProfileName}-optional"
            };
        }
    }
}
