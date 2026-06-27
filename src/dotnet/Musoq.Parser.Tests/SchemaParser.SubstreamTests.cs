using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public class SchemaParserSubstreamTests : SchemaParserTestsBase
{
    private static SubstreamTypeNode ParseSubstream(string payloadField)
    {
        var schema = $@"binary Packet {{
            Length: uint le,
            Payload: {payloadField}
        }}";

        var result = ParseBinarySchema(schema);
        var payload = (FieldDefinitionNode)result.Fields[1];
        var substream = payload.TypeAnnotation as SubstreamTypeNode;
        Assert.IsNotNull(substream, "Expected SubstreamTypeNode");
        return substream;
    }

    [TestMethod]
    public void Substream_Raw_ShouldParseAsRawMode()
    {
        var substream = ParseSubstream("substream[Length] raw");

        Assert.AreEqual(SubstreamMode.Raw, substream.Mode);
    }

    [TestMethod]
    public void Substream_Raw_ShouldHaveNullTarget()
    {
        var substream = ParseSubstream("substream[Length] raw");

        Assert.IsNull(substream.Target);
    }

    [TestMethod]
    public void Substream_Raw_ShouldExposeByteArrayClrType()
    {
        var substream = ParseSubstream("substream[Length] raw");

        Assert.AreEqual(typeof(byte[]), substream.ClrType);
    }

    [TestMethod]
    public void Substream_AsSchemaReference_ShouldDefaultToExactMode()
    {
        var substream = ParseSubstream("substream[Length] as PayloadBody");

        Assert.AreEqual(SubstreamMode.Exact, substream.Mode);
    }

    [TestMethod]
    public void Substream_AsSchemaReference_ShouldCaptureSchemaTarget()
    {
        var substream = ParseSubstream("substream[Length] as PayloadBody");

        Assert.IsInstanceOfType<SchemaReferenceTypeNode>(substream.Target);
    }

    [TestMethod]
    public void Substream_AsSchemaReferenceExact_ShouldParseExactMode()
    {
        var substream = ParseSubstream("substream[Length] as PayloadBody exact");

        Assert.AreEqual(SubstreamMode.Exact, substream.Mode);
    }

    [TestMethod]
    public void Substream_AsSchemaReferenceLax_ShouldParseLaxMode()
    {
        var substream = ParseSubstream("substream[Length] as PayloadBody lax");

        Assert.AreEqual(SubstreamMode.Lax, substream.Mode);
    }

    [TestMethod]
    public void Substream_AsByteArray_ShouldCaptureByteArrayTarget()
    {
        var substream = ParseSubstream("substream[Length] as byte[Length]");

        Assert.IsInstanceOfType<ByteArrayTypeNode>(substream.Target);
    }

    [TestMethod]
    public void Substream_AsString_ShouldCaptureStringTarget()
    {
        var substream = ParseSubstream("substream[Length] as string[Length] ascii");

        Assert.IsInstanceOfType<StringTypeNode>(substream.Target);
    }

    [TestMethod]
    public void Substream_AsInlineSchema_ShouldCaptureInlineSchemaTarget()
    {
        var substream = ParseSubstream("substream[Length] as { Tag: byte, Value: short le }");

        Assert.IsInstanceOfType<InlineSchemaTypeNode>(substream.Target);
    }

    [TestMethod]
    public void Substream_AsSwitch_ShouldCaptureSwitchTarget()
    {
        var substream = ParseSubstream(
            "substream[Length] as switch Kind { 1 => A: ABody, _ => Raw: byte[Length] }");

        Assert.IsInstanceOfType<BinarySwitchTypeNode>(substream.Target);
    }

    [TestMethod]
    public void Substream_AsRepeatUntil_ShouldCaptureRepeatUntilTarget()
    {
        var substream = ParseSubstream("substream[Length] as Record repeat until Records.Tag = 0");

        Assert.IsInstanceOfType<RepeatUntilTypeNode>(substream.Target);
    }

    [TestMethod]
    public void Substream_AsRepeatUntilEof_ShouldCaptureEndOfInputTarget()
    {
        var substream = ParseSubstream("substream[Length] as Entry repeat until eof");

        Assert.IsInstanceOfType<RepeatUntilTypeNode>(substream.Target);
        var repeat = (RepeatUntilTypeNode)substream.Target!;
        Assert.AreEqual(RepeatUntilStopKind.EndOfInput, repeat.StopKind);
    }

    [TestMethod]
    public void Substream_AsRepeatUntilEof_WithLaxMode_ShouldCaptureModeAndEof()
    {
        var substream = ParseSubstream("substream[Length] as Entry repeat until eof lax");

        Assert.AreEqual(SubstreamMode.Lax, substream.Mode);
        Assert.IsInstanceOfType<RepeatUntilTypeNode>(substream.Target);
        var repeat = (RepeatUntilTypeNode)substream.Target!;
        Assert.AreEqual(RepeatUntilStopKind.EndOfInput, repeat.StopKind);
    }

    [TestMethod]
    public void Substream_ShouldNeverBeFixedSize()
    {
        var substream = ParseSubstream("substream[Length] raw");

        Assert.IsFalse(substream.IsFixedSize);
    }

    [TestMethod]
    public void Substream_Raw_ToString_ShouldRoundTripModeKeyword()
    {
        var substream = ParseSubstream("substream[Length] raw");

        StringAssert.Contains(substream.ToString(), "raw");
    }

    [TestMethod]
    public void Substream_AsLax_ToString_ShouldRoundTripModeKeyword()
    {
        var substream = ParseSubstream("substream[Length] as PayloadBody lax");

        StringAssert.Contains(substream.ToString(), "lax");
    }

    [TestMethod]
    public void Substream_AsExact_ToString_ShouldEmitExactKeyword()
    {
        var substream = ParseSubstream("substream[Length] as PayloadBody");

        StringAssert.Contains(substream.ToString(), "exact");
    }

    [TestMethod]
    public void Substream_MissingSize_ShouldThrow()
    {
        Assert.ThrowsExactly<SyntaxException>(() => ParseSubstream("substream raw"));
    }

    [TestMethod]
    public void Substream_MissingRawOrAs_ShouldReportModifierDiagnostic()
    {
        var schema = @"binary Packet {
            Length: uint le,
            Payload: substream[Length],
            Checksum: uint le
        }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));
        Assert.AreEqual(DiagnosticCode.MQ4014_InvalidSubstreamModifier, exception.Code);
    }

    [TestMethod]
    public void Substream_InvalidMode_ShouldReportModifierDiagnostic()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => ParseSubstream("substream[Length] as PayloadBody bogus"));

        Assert.AreEqual(DiagnosticCode.MQ4014_InvalidSubstreamModifier, exception.Code);
    }

    [TestMethod]
    public void Substream_NegativeConstantSize_ShouldReportInvalidFieldDiagnostic()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => ParseSubstream("substream[-1] raw"));

        Assert.AreEqual(DiagnosticCode.MQ4001_InvalidBinarySchemaField, exception.Code);
    }

    [TestMethod]
    public void Substream_EmptyTargetAfterAs_ShouldReportTargetDiagnostic()
    {
        var schema = @"binary Packet {
            Length: uint le,
            Payload: substream[Length] as
        }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));
        Assert.AreEqual(DiagnosticCode.MQ4015_InvalidSubstreamTarget, exception.Code);
    }
}
