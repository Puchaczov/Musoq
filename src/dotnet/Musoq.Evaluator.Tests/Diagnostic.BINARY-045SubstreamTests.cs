using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tests.Schema.Unknown;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;
using BinaryParseException = Musoq.Schema.Interpreters.ParseException;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class DiagnosticBinary045SubstreamTests : BinaryInterpretationTestBase
{
    private static readonly CompilationOptions CompilationOptions =
        new(usePrimitiveTypeValidation: false);

    [TestMethod]
    public void BinarySubstream_UnknownSizeReference_ShouldReportExactStructuredDiagnostic()
    {
        const string query =
            "binary Packet { Payload: substream[Missing] raw };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "unknown substream size reference");
        var expectedSpan = new TextSpan(query.IndexOf("Missing", StringComparison.Ordinal), "Missing".Length);

        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
        Assert.AreEqual(DiagnosticSourceKind.Query, diagnostic.SourceKind);
    }

    [TestMethod]
    public void BinarySubstream_ForwardSizeReference_ShouldFailBeforeCodeGeneration()
    {
        const string query =
            "binary Packet { Payload: substream[Later] raw, Later: byte };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "forward substream size reference");
        var expectedSpan = new TextSpan(query.IndexOf("Later", StringComparison.Ordinal), "Later".Length);

        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
    }

    [TestMethod]
    public void BinarySubstream_UnknownNestedSchemaReference_ShouldReportReferenceSpan()
    {
        const string query =
            "binary Packet { Length: byte, Payload: substream[Length] as MissingBody };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "unknown nested substream schema reference");
        var expectedSpan = new TextSpan(query.IndexOf("MissingBody", StringComparison.Ordinal), "MissingBody".Length);

        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
    }

    [TestMethod]
    public void BinarySubstream_InlineNestedSizeReference_ShouldReportReferenceSpan()
    {
        const string query =
            "binary Packet { Length: byte, Payload: substream[Length] as { Data: byte[Missing] } };" +
            "select 1 from #test.files();";

        var result = Analyze(query);
        var diagnostic = DiagnosticContractTestAssertions.AssertSingleError(
            result,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            "unknown inline substream size reference");
        var expectedSpan = new TextSpan(query.IndexOf("Missing", StringComparison.Ordinal), "Missing".Length);

        Assert.AreEqual(expectedSpan, diagnostic.Span);
        Assert.AreEqual(DiagnosticPhase.Parse, diagnostic.Phase);
    }

    [TestMethod]
    public void BinarySubstream_StructuredOverread_ShouldRemainBoundedAndStructured()
    {
        var registry = CreateBodyAndPacketRegistry(
            new SubstreamTypeNode(new IdentifierNode("Length"), SubstreamMode.Exact, new SchemaReferenceTypeNode("Body")));
        var interpreter = CompileInterpreter(registry, "Packet");

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, [1, 0xAA]));
        var exception = AssertParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
        Assert.AreEqual("Body", exception.SchemaName);
        Assert.AreEqual(1, exception.Position);
    }

    [TestMethod]
    public void BinarySubstream_LargeRawLength_ShouldReportInsufficientDataWithoutOverflow()
    {
        var interpreter = CreateAndCompileInterpreter(
            "Packet",
            new FieldDefinitionNode("Prefix", ByteType()),
            new FieldDefinitionNode(
                "Payload",
                new SubstreamTypeNode(new IntegerNode(int.MaxValue), SubstreamMode.Raw, null)));

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, [0x01]));
        var exception = AssertParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.InsufficientData, exception.ErrorCode);
        Assert.AreEqual("Packet", exception.SchemaName);
        Assert.AreEqual(1, exception.Position);
    }

    [TestMethod]
    public void BinarySubstream_ExactMode_ShouldReportFieldSpecificUnderConsumption()
    {
        var registry = CreateBodyAndPacketRegistry(
            new SubstreamTypeNode(new IdentifierNode("Length"), SubstreamMode.Exact, new SchemaReferenceTypeNode("Body")));
        var interpreter = CompileInterpreter(registry, "Packet");

        var wrapper = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeInterpret(interpreter, [3, 0xAA, 0xBB, 0xCC]));
        var exception = AssertParseException(wrapper);

        Assert.AreEqual(ParseErrorCode.ValidationFailed, exception.ErrorCode);
        Assert.AreEqual("Packet", exception.SchemaName);
        Assert.AreEqual("Payload", exception.FieldName);
        Assert.AreEqual(1, exception.Position);
        StringAssert.Contains(exception.Details, "declared 3 bytes");
        StringAssert.Contains(exception.Details, "consumed only 2");
    }

    [TestMethod]
    public void BinarySubstream_SwitchTarget_ShouldFailClosedWithMq4016()
    {
        var switchType = new BinarySwitchTypeNode(
            "Kind",
            [new BinarySwitchCaseNode(
                new IntegerNode(1),
                "Value",
                ByteType())]);
        var registry = new SchemaRegistry();
        registry.Register("Packet", new BinarySchemaNode(
            "Packet",
            [
                new FieldDefinitionNode("Kind", ByteType()),
                new FieldDefinitionNode(
                    "Payload",
                    new SubstreamTypeNode(new IntegerNode(1), SubstreamMode.Exact, switchType))
            ]));

        var exception = Assert.ThrowsExactly<ConstructionNotYetSupported>(
            () => new InterpreterCodeGenerator(registry).GenerateAll());

        Assert.AreEqual(DiagnosticCode.MQ4016_UnsupportedSchemaConstruction, exception.Code);
        StringAssert.Contains(exception.Message, "Packet");
        StringAssert.Contains(exception.Message, "Payload");
        StringAssert.Contains(exception.Message, "substream target");
    }

    private static QueryAnalysisResult Analyze(string query)
    {
        return new QueryAnalyzer(
                new UnknownSchemaProvider(Array.Empty<dynamic>()),
                compilationOptions: CompilationOptions)
            .Analyze(query);
    }

    private static SchemaRegistry CreateBodyAndPacketRegistry(SubstreamTypeNode payloadType)
    {
        var registry = new SchemaRegistry();
        registry.Register("Body", new BinarySchemaNode(
            "Body",
            [
                new FieldDefinitionNode("A", ByteType()),
                new FieldDefinitionNode("B", ByteType())
            ]));
        registry.Register("Packet", new BinarySchemaNode(
            "Packet",
            [
                new FieldDefinitionNode("Length", ByteType()),
                new FieldDefinitionNode("Payload", payloadType),
                new FieldDefinitionNode("Tail", ByteType())
            ]));
        return registry;
    }

    private static PrimitiveTypeNode ByteType()
    {
        return new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);
    }

    private static BinaryParseException AssertParseException(TargetInvocationException wrapper)
    {
        Assert.IsNotNull(wrapper.InnerException);
        Assert.IsInstanceOfType<BinaryParseException>(wrapper.InnerException);
        return (BinaryParseException)wrapper.InnerException!;
    }
}
