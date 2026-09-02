using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticBinary041NestedArrayComputedTests : SchemaParserTestsBase
{
    [TestMethod]
    public void BinaryDeclaration_NestedArraysConditionsAndComputedFields_ShouldPreserveAst()
    {
        const string schema =
            "binary Packet {" +
            " Count: byte," +
            " Child: Inner," +
            " Items: Inner[Count]," +
            " Values: int le[Count]," +
            " Names: string[2] ascii[Count]," +
            " Present: byte when Count <> 0," +
            " Total: Count + 1" +
            "}";

        var result = ParseBinarySchema(schema);

        Assert.HasCount(7, result.Fields);
        Assert.IsInstanceOfType<SchemaReferenceTypeNode>(Field(result, "Child").TypeAnnotation);

        var items = Field(result, "Items").TypeAnnotation as ArrayTypeNode;
        Assert.IsNotNull(items);
        Assert.IsInstanceOfType<SchemaReferenceTypeNode>(items.ElementType);
        Assert.IsInstanceOfType<IdentifierNode>(items.SizeExpression);

        var values = Field(result, "Values").TypeAnnotation as ArrayTypeNode;
        Assert.IsNotNull(values);
        Assert.IsInstanceOfType<PrimitiveTypeNode>(values.ElementType);
        Assert.AreEqual(Endianness.LittleEndian, ((PrimitiveTypeNode)values.ElementType).Endianness);

        var names = Field(result, "Names").TypeAnnotation as ArrayTypeNode;
        Assert.IsNotNull(names);
        Assert.IsInstanceOfType<StringTypeNode>(names.ElementType);
        Assert.AreEqual(StringEncoding.Ascii, ((StringTypeNode)names.ElementType).Encoding);

        var present = Field(result, "Present");
        Assert.IsNotNull(present.WhenCondition);
        Assert.IsInstanceOfType<ComputedFieldNode>(result.Fields[^1]);
        Assert.IsInstanceOfType<AddNode>(((ComputedFieldNode)result.Fields[^1]).Expression);
    }

    [TestMethod]
    public void BinaryDeclaration_ComputedDisambiguation_ShouldKeepKnownSchemaAndTypeFormsParsed()
    {
        const string schema =
            "binary Packet {" +
            " Count: byte," +
            " Typed: int le," +
            " Buffer: byte[2]," +
            " Nested: KnownSchema," +
            " Parenthesized: (Count)," +
            " Flag: (Count & 1) <> 0" +
            "}";

        var result = ParseBinarySchema(schema);

        Assert.IsInstanceOfType<FieldDefinitionNode>(Field(result, "Typed"));
        Assert.IsInstanceOfType<ByteArrayTypeNode>(ParsedField(result, "Buffer").TypeAnnotation);
        Assert.IsInstanceOfType<SchemaReferenceTypeNode>(ParsedField(result, "Nested").TypeAnnotation);
        Assert.IsInstanceOfType<ComputedFieldNode>(SchemaField(result, "Parenthesized"));
        Assert.IsInstanceOfType<ComputedFieldNode>(SchemaField(result, "Flag"));
    }

    [TestMethod]
    public void BinaryDeclaration_NegativeNamedSchemaArraySize_ShouldReportExactSchemaDiagnostic()
    {
        const string schema = "binary Packet { Items: Inner[-1] }";

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4001_InvalidBinarySchemaField,
            SpanOf(schema, "-1"),
            () => ParseBinarySchema(schema));
    }

    [TestMethod]
    public void BinaryDeclaration_NegativeInlineSchemaArraySize_ShouldReportExactSchemaDiagnostic()
    {
        const string schema = "binary Packet { Items: { Value: byte }[-1] }";

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4001_InvalidBinarySchemaField,
            SpanOf(schema, "-1"),
            () => ParseBinarySchema(schema));
    }

    private static FieldDefinitionNode Field(BinarySchemaNode schema, string name)
    {
        return ParsedField(schema, name);
    }

    private static FieldDefinitionNode ParsedField(BinarySchemaNode schema, string name)
    {
        var field = schema.Fields.Single(item => item.Name == name) as FieldDefinitionNode;
        Assert.IsNotNull(field);
        return field;
    }

    private static SchemaFieldNode SchemaField(BinarySchemaNode schema, string name)
    {
        return schema.Fields.Single(item => item.Name == name);
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
        Assert.AreEqual(expectedSpan.Start, envelope.Offset);
        Assert.AreEqual(expectedSpan.Length, envelope.Length);
        Assert.IsNotEmpty(envelope.Actions);
    }

    private static TextSpan SpanOf(string source, string text)
    {
        var start = source.IndexOf(text, StringComparison.Ordinal);
        Assert.IsGreaterThanOrEqualTo(0, start);
        return new TextSpan(start, text.Length);
    }
}
