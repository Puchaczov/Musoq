using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticBinary043RepetitionSemanticTests
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void BinaryRepeatUntil_UnknownConditionReference_ShouldReportExactStructuredDiagnostic()
    {
        const string query =
            "binary Packet { Items: byte repeat until Missing = 0 };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "unknown repeat-until condition reference");
        var expectedStart = query.IndexOf("Missing", StringComparison.Ordinal);

        Assert.AreEqual(new TextSpan(expectedStart, "Missing".Length), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual("Missing".Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    public void BinaryRepeatUntil_NonBooleanCondition_ShouldReportExactStructuredMq4006()
    {
        const string query =
            "binary Packet { Items: byte repeat until Items };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ4006_InvalidFieldConstraint,
            "non-boolean repeat-until condition");
        var expectedStart = query.LastIndexOf("Items", StringComparison.Ordinal);

        Assert.AreEqual(new TextSpan(expectedStart, "Items".Length), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Schema, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Schema, diagnostic.SourceKind);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual("Items".Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    public void BinaryRepeatUntil_ForwardElementSchemaReference_ShouldFailBeforeCodeGeneration()
    {
        const string query =
            "binary Outer { Items: Inner repeat until Items[-1] = 0 };" +
            "binary Inner { Value: byte };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "forward repeat element schema reference");
        var expectedStart = query.IndexOf("Inner", StringComparison.Ordinal);

        Assert.AreEqual(new TextSpan(expectedStart, "Inner".Length), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);
    }
}
