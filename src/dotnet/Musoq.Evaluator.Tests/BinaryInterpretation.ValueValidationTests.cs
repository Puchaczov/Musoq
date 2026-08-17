using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretationValueValidationTests : BinaryInterpretationTestBase
{
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static object CompileFromSchemaText(string schemaText, string schemaName)
    {
        var lexer = new Lexer(schemaText, true);
        var parser = new Musoq.Parser.SchemaParser(lexer);
        var schema = (BinarySchemaNode)parser.ParseSchema();

        var registry = new SchemaRegistry();
        registry.Register(schemaName, schema);
        return CompileInterpreter(registry, schemaName);
    }

    [TestMethod]
    public void ScalarConst_WhenValueMatches_ShouldParse()
    {
        var interpreter = CompileFromSchemaText("binary V { Version: byte const 1 }", "V");

        var result = InvokeInterpret(interpreter, [0x01]);

        Assert.AreEqual((byte)1, GetPropertyValue<byte>(result, "Version"));
    }

    [TestMethod]
    public void ScalarConst_WhenValueDiffers_ShouldThrow()
    {
        var interpreter = CompileFromSchemaText("binary V { Version: byte const 1 }", "V");

        Assert.Throws<Exception>(() => InvokeInterpret(interpreter, [0x02]));
    }

    [TestMethod]
    public void MagicByteList_WhenSignatureMatches_ShouldParse()
    {
        var interpreter = CompileFromSchemaText(
            "binary Png { Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A] }",
            "Png");

        var result = InvokeInterpret(interpreter, PngSignature);

        CollectionAssert.AreEqual(PngSignature, GetPropertyValue<byte[]>(result, "Signature"));
    }

    [TestMethod]
    public void MagicByteList_WhenSignatureDiffers_ShouldThrow()
    {
        var interpreter = CompileFromSchemaText(
            "binary Png { Signature: byte[8] magic [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A] }",
            "Png");

        byte[] corrupted = [0x00, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        Assert.Throws<Exception>(() => InvokeInterpret(interpreter, corrupted));
    }

    [TestMethod]
    public void OneOf_WhenValueIsAllowed_ShouldParse()
    {
        var interpreter = CompileFromSchemaText("binary K { Kind: byte oneOf [1, 2] }", "K");

        var result = InvokeInterpret(interpreter, [0x02]);

        Assert.AreEqual((byte)2, GetPropertyValue<byte>(result, "Kind"));
    }

    [TestMethod]
    public void OneOf_WhenValueIsNotAllowed_ShouldThrow()
    {
        var interpreter = CompileFromSchemaText("binary K { Kind: byte oneOf [1, 2] }", "K");

        Assert.Throws<Exception>(() => InvokeInterpret(interpreter, [0x03]));
    }

    [TestMethod]
    public void StringOneOf_WhenChunkTypeIsAllowed_ShouldParse()
    {
        var interpreter = CompileFromSchemaText(
            "binary C { ChunkType: string[4] ascii oneOf ['IHDR', 'IDAT', 'IEND'] }",
            "C");

        var result = InvokeInterpret(interpreter, [(byte)'I', (byte)'D', (byte)'A', (byte)'T']);

        Assert.AreEqual("IDAT", GetPropertyValue<string>(result, "ChunkType"));
    }

    [TestMethod]
    public void StringOneOf_WhenChunkTypeIsNotAllowed_ShouldThrow()
    {
        var interpreter = CompileFromSchemaText(
            "binary C { ChunkType: string[4] ascii oneOf ['IHDR', 'IDAT', 'IEND'] }",
            "C");

        Assert.Throws<Exception>(() =>
            InvokeInterpret(interpreter, [(byte)'Z', (byte)'Z', (byte)'Z', (byte)'Z']));
    }

    [TestMethod]
    public void Validation_WhenConditionIsFalse_ShouldSkipValidation()
    {
        var interpreter = CompileFromSchemaText(
            "binary W { Flag: byte, Value: byte const 5 when Flag = 1 }",
            "W");

        var result = InvokeInterpret(interpreter, [0x00]);

        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Flag"));
    }

    [TestMethod]
    public void Validation_WhenConditionIsTrueAndValueDiffers_ShouldThrow()
    {
        var interpreter = CompileFromSchemaText(
            "binary W { Flag: byte, Value: byte const 5 when Flag = 1 }",
            "W");

        Assert.Throws<Exception>(() => InvokeInterpret(interpreter, [0x01, 0x09]));
    }

    [TestMethod]
    public void Validation_WhenCoexistsWithCheck_ShouldApplyBoth()
    {
        var interpreter = CompileFromSchemaText(
            "binary M { Marker: byte const 7 check Marker > 0 }",
            "M");

        var result = InvokeInterpret(interpreter, [0x07]);

        Assert.AreEqual((byte)7, GetPropertyValue<byte>(result, "Marker"));
    }
}
