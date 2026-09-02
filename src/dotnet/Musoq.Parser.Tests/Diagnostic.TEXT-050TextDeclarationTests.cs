using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticText050TextDeclarationTests : SchemaParserTestsBase
{
    [TestMethod]
    public void TextSchema_BackreferencePattern_ShouldReportUnsupportedPatternDiagnostic()
    {
        const string schema = @"text Log { Value: pattern '(?<word>[a-z]+)\k<word>' }";

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            SpanOf(schema, @"'(?<word>[a-z]+)\k<word>'"),
            () => ParseTextSchema(schema));
    }

    [TestMethod]
    public void TextSchema_MalformedCapturePattern_ShouldReportPatternLiteralSpan()
    {
        const string schema = @"text Log { Value: pattern '(?<word>[a-z]+' }";

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            SpanOf(schema, "'(?<word>[a-z]+'"),
            () => ParseTextSchema(schema));
    }

    [TestMethod]
    public void TextSchema_DuplicateCaptureGroupRequest_ShouldRemainSourceLocated()
    {
        const string schema = "text Log { Value: pattern '(?<word>[a-z]+)' capture (word, word) }";

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4002_InvalidTextSchemaField,
            SpanOf(schema, "'(?<word>[a-z]+)'"),
            () => ParseTextSchema(schema));
    }

    private static void AssertExactSchemaDiagnostic(
        string schema,
        DiagnosticCode expectedCode,
        TextSpan expectedSpan,
        Action parse)
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(parse);

        Assert.AreEqual(expectedCode, exception.Code);
        Assert.IsTrue(exception.Span.HasValue);
        Assert.IsNotNull(exception.Span);
        Assert.AreEqual(expectedSpan, exception.Span.Value);

        var diagnostic = exception.ToDiagnostic(new SourceText(schema));
        Assert.AreEqual(expectedCode, diagnostic.Code);
        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Schema, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Schema, diagnostic.SourceKind);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference));
        Assert.IsNotEmpty(diagnostic.SuggestedFixes);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, schema);
        Assert.AreEqual(expectedCode, envelope.Code);
        Assert.AreEqual(DiagnosticPhase.Schema, envelope.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Schema, envelope.SourceKind);
        Assert.AreEqual(expectedSpan.Start, envelope.Offset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.IsNotEmpty(envelope.Actions);
    }

    private static TextSpan SpanOf(string source, string text)
    {
        var start = source.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in '{source}'.");
        return new TextSpan(start, text.Length);
    }
}
