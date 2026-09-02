using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;
using BinaryParseException = Musoq.Schema.Interpreters.ParseException;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticBinary042BitAlignmentValidationTests : BinaryInterpretationTestBase
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void BinaryInterpretation_AlignmentToNonByteBoundary_ShouldUseAbsoluteBitPosition()
    {
        var fields = new SchemaFieldNode[]
        {
            new FieldDefinitionNode("Prefix", new BitsTypeNode(4)),
            new FieldDefinitionNode("_align", new AlignmentNode(3)),
            new FieldDefinitionNode("Value", new BitsTypeNode(2)),
            new FieldDefinitionNode("Tail", new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable))
        };
        var registry = new SchemaRegistry();
        registry.Register("Packet", new BinarySchemaNode("Packet", fields));

        var interpreter = CompileInterpreter(registry, "Packet");
        var result = InvokeInterpret(interpreter, [0xC0, 0xA5]);

        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Prefix"));
        Assert.AreEqual((byte)3, GetPropertyValue<byte>(result, "Value"));
        Assert.AreEqual((byte)0xA5, GetPropertyValue<byte>(result, "Tail"));
    }

    [TestMethod]
    public void BinaryInterpretation_AlignmentLargerThanBitFieldLimit_ShouldRemainValid()
    {
        var fields = new SchemaFieldNode[]
        {
            new FieldDefinitionNode("_align", new AlignmentNode(128)),
            new FieldDefinitionNode("Value", new BitsTypeNode(1))
        };
        var registry = new SchemaRegistry();
        registry.Register("WideBoundary", new BinarySchemaNode("WideBoundary", fields));

        var interpreter = CompileInterpreter(registry, "WideBoundary");
        var result = InvokeInterpret(interpreter, [0x01]);

        Assert.AreEqual((byte)1, GetPropertyValue<byte>(result, "Value"));
    }

    [TestMethod]
    public void BinaryInterpretation_AtAfterBitField_ShouldResetBitOffset()
    {
        var fields = new SchemaFieldNode[]
        {
            new FieldDefinitionNode("Prefix", new BitsTypeNode(3)),
            new FieldDefinitionNode("Reread", new BitsTypeNode(3), null, new IntegerNode(1)),
            new FieldDefinitionNode("_align", new AlignmentNode(8)),
            new FieldDefinitionNode("Tail", new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable))
        };
        var registry = new SchemaRegistry();
        registry.Register("PositionedBits", new BinarySchemaNode("PositionedBits", fields));

        var interpreter = CompileInterpreter(registry, "PositionedBits");
        var result = InvokeInterpret(interpreter, [0x07, 0x05, 0xA6]);

        Assert.AreEqual((byte)7, GetPropertyValue<byte>(result, "Prefix"));
        Assert.AreEqual((byte)5, GetPropertyValue<byte>(result, "Reread"));
        Assert.AreEqual((byte)0xA6, GetPropertyValue<byte>(result, "Tail"));
    }

    [TestMethod]
    public void BinaryInterpretation_NegativeAtPosition_ShouldRaiseStructuredInvalidPosition()
    {
        var field = new FieldDefinitionNode(
            "Value",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable),
            null,
            new IntegerNode(-1));
        var registry = new SchemaRegistry();
        registry.Register("InvalidPosition", new BinarySchemaNode("InvalidPosition", [field]));

        var interpreter = CompileInterpreter(registry, "InvalidPosition");
        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, [0x01]));

        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<BinaryParseException>(wrapper.InnerException);
        var exception = (BinaryParseException)wrapper.InnerException;
        Assert.AreEqual(ParseErrorCode.InvalidPosition, exception.ErrorCode);
        Assert.AreEqual("InvalidPosition", exception.SchemaName);
        Assert.IsNull(exception.FieldName);
        Assert.AreEqual(-1, exception.Position);
        StringAssert.Contains(exception.Details, "negative position");
    }

    [TestMethod]
    public void BinaryInterpretation_CheckConstraint_ShouldValidateAfterCurrentFieldRead()
    {
        var check = new FieldConstraintNode(
            new GreaterNode(new IdentifierNode("Value"), new IntegerNode(0)));
        var field = new FieldDefinitionNode(
            "Value",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable),
            check);
        var registry = new SchemaRegistry();
        registry.Register("Checked", new BinarySchemaNode("Checked", [field]));

        var interpreter = CompileInterpreter(registry, "Checked");
        var result = InvokeInterpret(interpreter, [0x01]);
        Assert.AreEqual((byte)1, GetPropertyValue<byte>(result, "Value"));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, [0x00]));
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<BinaryParseException>(wrapper.InnerException);
        var exception = (BinaryParseException)wrapper.InnerException;
        Assert.AreEqual(ParseErrorCode.ValidationFailed, exception.ErrorCode);
        Assert.AreEqual("Checked", exception.SchemaName);
        Assert.AreEqual("Value", exception.FieldName);
        Assert.AreEqual(1, exception.Position);
        StringAssert.Contains(exception.Details, "Check constraint failed");
    }

    [TestMethod]
    public void BinaryInterpretation_ValueValidation_ShouldApplyAfterStringModifiers()
    {
        var interpreter = CompileFromSchemaText(
            "binary Trimmed { Value: string[4] ascii trim const 'OK', Tail: byte }",
            "Trimmed");

        var result = InvokeInterpret(interpreter, [0x4F, 0x4B, 0x20, 0x20, 0xA5]);
        Assert.AreEqual("OK", GetPropertyValue<string>(result, "Value"));
        Assert.AreEqual((byte)0xA5, GetPropertyValue<byte>(result, "Tail"));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, [0x4E, 0x4F, 0x20, 0x20, 0xA5]));
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<BinaryParseException>(wrapper.InnerException);
        Assert.AreEqual(ParseErrorCode.ValidationFailed, ((BinaryParseException)wrapper.InnerException).ErrorCode);
    }

    [TestMethod]
    public void BinaryInterpretation_MagicAndOneOf_ShouldReportFieldSpecificValidationFailures()
    {
        var interpreter = CompileFromSchemaText(
            "binary Header { Signature: byte[2] magic [0xAA, 0x55], Kind: byte oneOf [1, 2] }",
            "Header");

        var valid = InvokeInterpret(interpreter, [0xAA, 0x55, 0x02]);
        Assert.AreEqual((byte)2, GetPropertyValue<byte>(valid, "Kind"));

        var signatureWrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, [0xAA, 0x54, 0x02]));
        Assert.IsNotNull(signatureWrapper.InnerException);
        Assert.IsInstanceOfType<BinaryParseException>(signatureWrapper.InnerException);
        var signatureException = (BinaryParseException)signatureWrapper.InnerException;
        Assert.AreEqual(ParseErrorCode.ValidationFailed, signatureException.ErrorCode);
        Assert.AreEqual("Signature", signatureException.FieldName);
        Assert.AreEqual(2, signatureException.Position);

        var kindWrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, [0xAA, 0x55, 0x03]));
        Assert.IsNotNull(kindWrapper.InnerException);
        Assert.IsInstanceOfType<BinaryParseException>(kindWrapper.InnerException);
        var kindException = (BinaryParseException)kindWrapper.InnerException;
        Assert.AreEqual(ParseErrorCode.ValidationFailed, kindException.ErrorCode);
        Assert.AreEqual("Kind", kindException.FieldName);
        Assert.AreEqual(3, kindException.Position);
    }

    [TestMethod]
    public void BinarySchema_NonBooleanCheck_ShouldReportExactStructuredMq4006()
    {
        const string query =
            "binary Packet { Value: byte check Value };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ4006_InvalidFieldConstraint,
            "non-boolean current-field check expression");
        var expectedStart = query.LastIndexOf("Value", StringComparison.Ordinal);

        Assert.AreEqual(new TextSpan(expectedStart, "Value".Length), diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Schema, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Schema, diagnostic.SourceKind);

        var envelope = MusoqErrorEnvelope.FromDiagnostic(diagnostic, query);
        Assert.AreEqual(expectedStart, envelope.Offset);
        Assert.AreEqual("Value".Length, envelope.Length);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Explanation));
        Assert.IsNotEmpty(envelope.SuggestedFixes);
        Assert.IsNotEmpty(envelope.Actions);
    }

    [TestMethod]
    public void BinarySchema_UnknownCheckReference_ShouldReportExactStructuredMq2030()
    {
        const string query =
            "binary Packet { Value: byte check Missing > 0 };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "unknown check field reference");
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

    private static object CompileFromSchemaText(string schemaText, string schemaName)
    {
        var lexer = new Lexer(schemaText, true);
        var parser = new SchemaParser(lexer);
        var schema = (BinarySchemaNode)parser.ParseSchema();
        var registry = new SchemaRegistry();
        registry.Register(schemaName, schema);
        return CompileInterpreter(registry, schemaName);
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);
    }
}
