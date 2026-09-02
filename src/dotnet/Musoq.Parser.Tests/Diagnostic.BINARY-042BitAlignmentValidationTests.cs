using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticBinary042BitAlignmentValidationTests : SchemaParserTestsBase
{
    [TestMethod]
    public void BitAndAlignmentAnnotations_BoundariesAndIntegerLiteralBases_ShouldParse()
    {
        const string schema =
            "binary Boundaries { Low: bits[1], High: bits[64], Hex: bits[0x40], Wide: align[0x80] }";

        var result = ParseBinarySchema(schema);

        Assert.AreEqual(4, result.Fields.Length);
        Assert.AreEqual(1, ((FieldDefinitionNode)result.Fields[0]).TypeAnnotation is BitsTypeNode low
            ? low.BitCount
            : -1);
        Assert.AreEqual(64, ((FieldDefinitionNode)result.Fields[1]).TypeAnnotation is BitsTypeNode high
            ? high.BitCount
            : -1);
        Assert.AreEqual(64, ((FieldDefinitionNode)result.Fields[2]).TypeAnnotation is BitsTypeNode hex
            ? hex.BitCount
            : -1);
        Assert.AreEqual(128, ((FieldDefinitionNode)result.Fields[3]).TypeAnnotation is AlignmentNode wide
            ? wide.AlignmentBits
            : -1);
    }

    [TestMethod]
    [DataRow("bits[0]", "0")]
    [DataRow("bits[65]", "65")]
    [DataRow("bits[-1]", "-1")]
    [DataRow("bits[foo]", "foo")]
    [DataRow("bits[1.5]", "1.5")]
    [DataRow("align[0]", "0")]
    [DataRow("align[-1]", "-1")]
    [DataRow("align[foo]", "foo")]
    public void InvalidBitOrAlignmentAnnotation_ShouldReportExactMq4001Span(string annotation, string offendingText)
    {
        var schema = $"binary Packet {{ Value: {annotation} }}";
        var expectedSpan = SpanOf(schema, offendingText);

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4001_InvalidBinarySchemaField,
            expectedSpan,
            annotation,
            () => ParseBinarySchema(schema));
    }

    [TestMethod]
    public void ValueValidationLiterals_ShouldPreserveSourceSpansAndRejectIncompatibleValues()
    {
        const string validSchema = "binary Packet { Value: byte oneOf [1, 2, 3] }";
        var valid = ParseBinarySchema(validSchema);
        var values = ((FieldDefinitionNode)valid.Fields.Single()).ValueValidation!.Values;

        Assert.AreEqual(SpanOf(validSchema, "1"), values[0].Span);
        Assert.AreEqual(SpanOf(validSchema, "2"), values[1].Span);
        Assert.AreEqual(SpanOf(validSchema, "3"), values[2].Span);

        const string invalidSchema = "binary Packet { Value: byte[2] const [1, 256] }";
        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(invalidSchema));

        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.Code);
        Assert.AreEqual(SpanOf(invalidSchema, "256"), exception.Span!.Value);
    }

    [TestMethod]
    [DataRow("string[4] ascii const 1", "1")]
    [DataRow("byte oneOf ['text']", "'text'")]
    [DataRow("byte at 0 const 1", "const")]
    [DataRow("byte check Value > 0 const 1", "const")]
    public void InvalidValueValidationShapeOrOrdering_ShouldReportExactMq4006Span(
        string fieldDefinition,
        string offendingText)
    {
        var schema = $"binary Packet {{ Value: {fieldDefinition} }}";
        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));

        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.Code);
        Assert.AreEqual(SpanOf(schema, offendingText), exception.Span!.Value);
    }

    private static void AssertExactSchemaDiagnostic(
        string schema,
        DiagnosticCode expectedCode,
        TextSpan expectedSpan,
        string context,
        Action parse)
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(parse, context);

        Assert.AreEqual(expectedCode, exception.Code, context);
        Assert.IsTrue(exception.Span.HasValue, context);
        Assert.IsNotNull(exception.Span, context);
        Assert.AreEqual(expectedSpan, exception.Span.Value, context);

        var diagnostic = exception.ToDiagnostic(new SourceText(schema));
        Assert.AreEqual(expectedCode, diagnostic.Code, context);
        Assert.AreEqual(expectedSpan, diagnostic.Span, context);
        Assert.AreEqual(DiagnosticPhase.Schema, diagnostic.Phase, context);
        Assert.AreEqual(DiagnosticSourceKind.Schema, diagnostic.SourceKind, context);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Explanation), context);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.DocsReference), context);
        Assert.IsNotEmpty(diagnostic.SuggestedFixes, context);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, schema);
        Assert.AreEqual(expectedCode, envelope.Code, context);
        Assert.AreEqual(DiagnosticPhase.Schema, envelope.Phase, context);
        Assert.AreEqual(DiagnosticSourceKind.Schema, envelope.SourceKind, context);
        Assert.AreEqual(expectedSpan.Start, envelope.Offset, context);
        Assert.AreEqual(expectedSpan.Length, envelope.Length, context);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation), context);
        Assert.IsNotEmpty(envelope.SuggestedFixes, context);
        Assert.IsNotEmpty(envelope.Actions, context);
    }

    private static TextSpan SpanOf(string source, string text)
    {
        var start = source.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start, $"'{text}' was not found in '{source}'.");
        return new TextSpan(start, text.Length);
    }
}
