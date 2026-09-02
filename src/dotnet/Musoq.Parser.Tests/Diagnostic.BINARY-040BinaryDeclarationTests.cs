using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticBinary040BinaryDeclarationTests : SchemaParserTestsBase
{
    [TestMethod]
    public void BinaryDeclaration_AllPrimitiveTypes_ShouldPreserveTypesAndByteOrder()
    {
        const string schema =
            "binary PrimitiveRecord {" +
            " ByteValue: byte," +
            " SByteValue: sbyte," +
            " ShortValue: short le," +
            " UShortValue: ushort be," +
            " IntValue: int le," +
            " UIntValue: uint be," +
            " LongValue: long le," +
            " ULongValue: ulong be," +
            " FloatValue: float le," +
            " DoubleValue: double be," +
            "}";

        var result = ParseBinarySchema(schema);

        Assert.HasCount(10, result.Fields);
        AssertPrimitive(result, "ByteValue", PrimitiveTypeName.Byte, Endianness.NotApplicable, 1);
        AssertPrimitive(result, "SByteValue", PrimitiveTypeName.SByte, Endianness.NotApplicable, 1);
        AssertPrimitive(result, "ShortValue", PrimitiveTypeName.Short, Endianness.LittleEndian, 2);
        AssertPrimitive(result, "UShortValue", PrimitiveTypeName.UShort, Endianness.BigEndian, 2);
        AssertPrimitive(result, "IntValue", PrimitiveTypeName.Int, Endianness.LittleEndian, 4);
        AssertPrimitive(result, "UIntValue", PrimitiveTypeName.UInt, Endianness.BigEndian, 4);
        AssertPrimitive(result, "LongValue", PrimitiveTypeName.Long, Endianness.LittleEndian, 8);
        AssertPrimitive(result, "ULongValue", PrimitiveTypeName.ULong, Endianness.BigEndian, 8);
        AssertPrimitive(result, "FloatValue", PrimitiveTypeName.Float, Endianness.LittleEndian, 4);
        AssertPrimitive(result, "DoubleValue", PrimitiveTypeName.Double, Endianness.BigEndian, 8);
    }

    [TestMethod]
    public void BinaryDeclaration_EmptyAndTrailingComma_ShouldBeValidBoundaries()
    {
        var empty = ParseBinarySchema("binary Empty { }");
        var trailing = ParseBinarySchema("binary Packet { Flag: byte, }");

        Assert.IsEmpty(empty.Fields);
        Assert.HasCount(1, trailing.Fields);
        Assert.AreEqual("Flag", trailing.Fields[0].Name);
    }

    [TestMethod]
    public void BinaryDeclaration_SizedBytesAndStrings_ShouldPreserveExpressionsAndModifiers()
    {
        const string schema =
            "binary Framed {" +
            " Length: byte," +
            " Payload: byte[Length]," +
            " Name: string[8] utf8 nullterm trim," +
            " Tail: byte" +
            "}";

        var result = ParseBinarySchema(schema);
        var payload = Field(result, "Payload").TypeAnnotation;
        var name = Field(result, "Name").TypeAnnotation as StringTypeNode;

        Assert.IsInstanceOfType<ByteArrayTypeNode>(payload);
        var byteArray = (ByteArrayTypeNode)payload;
        Assert.IsInstanceOfType<IdentifierNode>(byteArray.SizeExpression);
        Assert.AreEqual("Length", ((IdentifierNode)byteArray.SizeExpression).Name);

        Assert.IsNotNull(name);
        Assert.AreEqual(StringEncoding.Utf8, name.Encoding);
        Assert.AreEqual(StringModifier.NullTerm | StringModifier.Trim, name.Modifiers);
        Assert.IsTrue(name.IsFixedSize);
        Assert.AreEqual(8, name.FixedSizeBytes);
    }

    [TestMethod]
    public void BinaryDeclaration_MissingEndianness_ShouldReportExactSchemaDiagnostic()
    {
        const string schema = "binary Packet { Value: int }";
        var expectedSpan = new TextSpan(schema.IndexOf('}'), 1);

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4005_InvalidEndianness,
            expectedSpan,
            "missing multi-byte endianness",
            () => ParseBinarySchema(schema));
    }

    [TestMethod]
    public void BinaryDeclaration_InvalidEndianness_ShouldReportTheInvalidToken()
    {
        const string schema = "binary Packet { Value: int middle }";
        var expectedSpan = SpanOf(schema, "middle");

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4005_InvalidEndianness,
            expectedSpan,
            "invalid multi-byte endianness",
            () => ParseBinarySchema(schema));
    }

    [TestMethod]
    public void BinaryDeclaration_SingleByteEndianness_ShouldReportTheModifier()
    {
        const string schema = "binary Packet { Flag: byte le }";
        var expectedSpan = SpanOf(schema, "le");

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4005_InvalidEndianness,
            expectedSpan,
            "endianness on a single-byte field",
            () => ParseBinarySchema(schema));
    }

    [TestMethod]
    public void BinaryDeclaration_NegativeStringSize_ShouldFailAtTheSizeExpression()
    {
        const string schema = "binary Packet { Name: string[-5] utf8 }";
        var expectedSpan = SpanOf(schema, "-5");

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4001_InvalidBinarySchemaField,
            expectedSpan,
            "negative fixed string size",
            () => ParseBinarySchema(schema));
    }

    [TestMethod]
    public void BinaryDeclaration_InvalidStringEncoding_ShouldReportExactSchemaDiagnostic()
    {
        const string schema = "binary Packet { Name: string[5] klingon }";
        var expectedSpan = SpanOf(schema, "klingon");

        AssertExactSchemaDiagnostic(
            schema,
            DiagnosticCode.MQ4001_InvalidBinarySchemaField,
            expectedSpan,
            "invalid string encoding",
            () => ParseBinarySchema(schema));
    }

    private static FieldDefinitionNode Field(BinarySchemaNode schema, string name)
    {
        var field = schema.Fields.Single(item => item.Name == name) as FieldDefinitionNode;
        Assert.IsNotNull(field);
        return field;
    }

    private static void AssertPrimitive(
        BinarySchemaNode schema,
        string name,
        PrimitiveTypeName expectedType,
        Endianness expectedEndianness,
        int expectedSize)
    {
        var type = Field(schema, name).TypeAnnotation as PrimitiveTypeNode;
        Assert.IsNotNull(type, $"Expected primitive type for '{name}'.");
        Assert.AreEqual(expectedType, type.TypeName);
        Assert.AreEqual(expectedEndianness, type.Endianness);
        Assert.AreEqual(expectedSize, type.FixedSizeBytes);
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
