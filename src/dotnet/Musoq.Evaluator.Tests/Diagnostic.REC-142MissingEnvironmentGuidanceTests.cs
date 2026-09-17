using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticREC142MissingEnvironmentGuidanceTests : BasicEntityTestBase
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void MissingSchemaAndSourceMetadata_ShouldStayBindDiagnosticsWithObservationGuidance()
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#known"] = [new BasicEntity("fixture")]
            });

        const string unknownSchemaQuery = "select Name from #missing.entities()";
        var unknownSchema = new QueryAnalyzer(provider, compilationOptions: CompilationOptions)
            .Analyze(unknownSchemaQuery);
        var schemaDiagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            unknownSchema,
            DiagnosticCode.MQ3010_UnknownSchema,
            "missing schema metadata");
        var schemaEnvelope = MusoqErrorEnvelope.FromDiagnostic(schemaDiagnostic, unknownSchemaQuery);

        Assert.AreEqual(DiagnosticPhase.Bind, schemaEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, schemaEnvelope.SourceKind);
        Assert.IsTrue(schemaEnvelope.Message.Contains("missing", StringComparison.OrdinalIgnoreCase));
        AssertSafeObservationGuidance(schemaEnvelope, "provider");

        const string unknownSourceQuery = "select Name from #known.missing()";
        var unknownSource = new QueryAnalyzer(provider, compilationOptions: CompilationOptions)
            .Analyze(unknownSourceQuery);
        var sourceDiagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            unknownSource,
            DiagnosticCode.MQ3085_UnknownSource,
            "missing source metadata");
        var sourceEnvelope = MusoqErrorEnvelope.FromDiagnostic(sourceDiagnostic, unknownSourceQuery);

        Assert.AreEqual(DiagnosticPhase.Bind, sourceEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, sourceEnvelope.SourceKind);
        Assert.IsTrue(sourceEnvelope.Message.Contains("missing", StringComparison.OrdinalIgnoreCase));
        AssertSafeObservationGuidance(sourceEnvelope, "DESC");
    }

    [TestMethod]
    public void MissingRuntimeSetting_ShouldNameConfigurationGapWithoutDisclosingSecretValues()
    {
        const string query =
            "couple #settings.items with settings prod as ProdItems;" +
            "select Token from ProdItems();";

        var result = new QueryAnalyzer(
                new SourceRuntimeSettingsLifecycleTests.SettingsSchemaProvider(declareRequirement: true),
                compilationOptions: CompilationOptions)
            .Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ3067_MissingSourceRuntimeSetting,
            "missing source runtime setting");
        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);

        Assert.AreEqual(DiagnosticPhase.Bind, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, envelope.SourceKind);
        Assert.IsTrue(envelope.Message.Contains("TOKEN", StringComparison.Ordinal));
        Assert.DoesNotContain("prod-token", envelope.Message);
        Assert.DoesNotContain("secret", envelope.Message, StringComparison.OrdinalIgnoreCase);
        AssertSafeObservationGuidance(envelope, "resolver", "DESC SETTINGS");
    }

    [TestMethod]
    public void SyntaxConfigurationAndCompatibility_ShouldRemainSeparateRecoveryBoundaries()
    {
        var provider = new BasicSchemaProvider<BasicEntity>(
            new Dictionary<string, IEnumerable<BasicEntity>>
            {
                ["#known"] = [new BasicEntity("fixture")]
            });

        const string syntaxQuery =
            "select case when Population > 0 then 'positive' end from #known.entities()";
        var syntax = new QueryAnalyzer(provider, compilationOptions: CompilationOptions).Analyze(syntaxQuery);
        var syntaxDiagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            syntax,
            DiagnosticCode.MQ2001_UnexpectedToken,
            "syntax boundary");
        Assert.AreEqual(DiagnosticPhase.Parse, syntaxDiagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, syntaxDiagnostic.SourceKind);

        var original = new InvalidOperationException("secret-provider-argument");
        var lifecycle = Assert.Throws<DataSourceLifecycleException>(() =>
            DataSourceLifecycle.OpenSchema(
                new ThrowingProvider(original),
                "#provider",
                "items",
                "rows",
                "ctx-142"));
        var compatibilityEnvelope = MusoqErrorEnvelope.FromDiagnostic(lifecycle.ToDiagnostic());

        Assert.AreEqual(DiagnosticCode.MQ7010_DataSourceOpenFailed, compatibilityEnvelope.Code);
        Assert.AreEqual(DiagnosticPhase.DataSource, compatibilityEnvelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.DataSource, compatibilityEnvelope.SourceKind);
        Assert.DoesNotContain("secret-provider-argument", compatibilityEnvelope.Message);
        Assert.IsTrue(
            compatibilityEnvelope.Explanation?.Contains("arguments", StringComparison.OrdinalIgnoreCase) == true,
            "A provider/runtime failure must preserve the unknown-cause boundary instead of naming a unique repair.");
        AssertSafeObservationGuidance(compatibilityEnvelope, "Check", "verbose");
    }

    private static void AssertSafeObservationGuidance(
        MusoqErrorEnvelope envelope,
        params string[] requiredTerms)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
        Assert.IsTrue(
            envelope.Actions.All(action => action.TextEdit == null),
            "Environment recovery must request observation or configuration; it must not apply an automatic query edit.");

        var guidance = string.Join(
            " ",
            new[] { envelope.Explanation ?? string.Empty }
                .Concat(envelope.SuggestedFixes)
                .Concat(envelope.Actions.Select(action => action.Title)));
        foreach (var requiredTerm in requiredTerms)
            Assert.IsTrue(guidance.Contains(requiredTerm, StringComparison.OrdinalIgnoreCase),
                $"Expected recovery guidance to contain '{requiredTerm}': {guidance}");

        Assert.IsFalse(ContainsDestructiveRecoveryWord(guidance));
    }

    private static bool ContainsDestructiveRecoveryWord(string value)
    {
        return value.Contains("reset", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("reinstall", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("force", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingProvider(Exception exception) : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw exception;
        }
    }
}
