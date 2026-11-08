using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretationRepeatUntilEofTests : BinaryInterpretationTestBase
{
    [TestMethod]
    public void GenerateEofRepeat_ShouldUseWhileLoopNotDoWhile()
    {
        var registry = BuildBytePrimitiveEofRegistry();
        var generator = new InterpreterCodeGenerator(registry);

        var code = generator.GenerateAll();

        StringAssert.Contains(code, "while (!IsAtEnd(data))");
        StringAssert.Contains(code, "EnsureRepeatMadeProgress(\"Bytes\"");
    }

    [TestMethod]
    public void Interpret_EofRepeatBytes_WithEmptyInput_ShouldReturnEmptyArray()
    {
        var registry = BuildBytePrimitiveEofRegistry();
        var interpreter = CompileInterpreter(registry, "ByteStream");

        var result = InvokeInterpret(interpreter, Array.Empty<byte>());
        var bytes = GetPropertyValue<byte[]>(result, "Bytes");

        Assert.IsEmpty(bytes);
    }

    [TestMethod]
    public void Interpret_EofRepeatBytes_ShouldReadAllBytes()
    {
        var registry = BuildBytePrimitiveEofRegistry();
        var interpreter = CompileInterpreter(registry, "ByteStream");

        var data = new byte[] { 0x0A, 0x0B, 0x0C, 0x0D };
        var result = InvokeInterpret(interpreter, data);
        var bytes = GetPropertyValue<byte[]>(result, "Bytes");

        CollectionAssert.AreEqual(data, bytes);
    }

    [TestMethod]
    public void Interpret_EofRepeatFixedWidthString_ShouldReadAllChunks()
    {
        var registry = new SchemaRegistry();
        var stringType = new StringTypeNode(new IntegerNode("2"), StringEncoding.Ascii);
        var repeat = RepeatUntilTypeNode.EndOfInput(stringType, "Chunks");
        var field = new FieldDefinitionNode("Chunks", repeat);
        registry.Register("StringStream", new BinarySchemaNode("StringStream", [field]));

        var interpreter = CompileInterpreter(registry, "StringStream");

        var data = new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D' };
        var result = InvokeInterpret(interpreter, data);
        var chunks = GetPropertyValue<string[]>(result, "Chunks");

        Assert.HasCount(2, chunks);
        Assert.AreEqual("AB", chunks[0]);
        Assert.AreEqual("CD", chunks[1]);
    }

    [TestMethod]
    public void Interpret_EofRepeatFixedWidthString_WithPartialTrailingChunk_ShouldThrow()
    {
        var registry = new SchemaRegistry();
        var stringType = new StringTypeNode(new IntegerNode("2"), StringEncoding.Ascii);
        var repeat = RepeatUntilTypeNode.EndOfInput(stringType, "Chunks");
        var field = new FieldDefinitionNode("Chunks", repeat);
        registry.Register("StringStream", new BinarySchemaNode("StringStream", [field]));

        var interpreter = CompileInterpreter(registry, "StringStream");

        var data = new byte[] { (byte)'A', (byte)'B', (byte)'C' };

        Assert.ThrowsExactly<System.Reflection.TargetInvocationException>(
            () => InvokeInterpret(interpreter, data));
    }

    [TestMethod]
    public void Interpret_EofRepeatBits_ShouldConsumeUntilByteEnd()
    {
        var registry = new SchemaRegistry();
        var bitsType = new BitsTypeNode(2);
        var repeat = RepeatUntilTypeNode.EndOfInput(bitsType, "Pairs");
        var field = new FieldDefinitionNode("Pairs", repeat);
        registry.Register("BitStream", new BinarySchemaNode("BitStream", [field]));

        var interpreter = CompileInterpreter(registry, "BitStream");

        var data = new byte[] { 0b11_10_01_00 };
        var result = InvokeInterpret(interpreter, data);
        var pairs = GetPropertyValue<byte[]>(result, "Pairs");

        Assert.HasCount(4, pairs);
        Assert.AreEqual((byte)0b00, pairs[0]);
        Assert.AreEqual((byte)0b01, pairs[1]);
        Assert.AreEqual((byte)0b10, pairs[2]);
        Assert.AreEqual((byte)0b11, pairs[3]);
    }

    [TestMethod]
    public void Interpret_EofRepeatZeroByteSchemaReference_ShouldThrowProgressException()
    {
        var registry = new SchemaRegistry();

        registry.Register("Empty", new BinarySchemaNode("Empty", []));

        var schemaRef = new SchemaReferenceTypeNode("Empty");
        var repeat = RepeatUntilTypeNode.EndOfInput(schemaRef, "Items");
        var itemsField = new FieldDefinitionNode("Items", repeat);
        registry.Register("Outer", new BinarySchemaNode("Outer", [itemsField]));

        var interpreter = CompileInterpreter(registry, "Outer");

        var data = new byte[] { 0x01, 0x02 };

        var exception = Assert.ThrowsExactly<System.Reflection.TargetInvocationException>(
            () => InvokeInterpret(interpreter, data));
        Assert.IsInstanceOfType<Musoq.Schema.Interpreters.ParseException>(exception.InnerException);
    }

    private static SchemaRegistry BuildBytePrimitiveEofRegistry()
    {
        var registry = new SchemaRegistry();
        var byteType = new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var repeat = RepeatUntilTypeNode.EndOfInput(byteType, "Bytes");
        var field = new FieldDefinitionNode("Bytes", repeat);
        registry.Register("ByteStream", new BinarySchemaNode("ByteStream", [field]));
        return registry;
    }
}
