#nullable enable annotations

using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretation_PrimitiveTests : BinaryInterpretationTestBase
{
    [TestMethod]
    public void Interpret_ByteArray_ShouldParseFixedSize()
    {
        // Arrange: binary Data { Payload: byte[4] }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(4);
        var byteArrayType = new ByteArrayTypeNode(sizeExpr);
        var fields = new[]
        {
            new FieldDefinitionNode("Payload", byteArrayType)
        };
        var schema = new BinarySchemaNode("Data", fields);
        registry.Register("Data", schema);

        var interpreter = CompileInterpreter(registry, "Data");
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var payload = GetPropertyValue<byte[]>(result, "Payload");

        // Assert
        Assert.IsNotNull(payload);
        Assert.HasCount(4, payload);
        Assert.AreEqual((byte)0x01, payload[0]);
        Assert.AreEqual((byte)0x02, payload[1]);
        Assert.AreEqual((byte)0x03, payload[2]);
        Assert.AreEqual((byte)0x04, payload[3]);
    }

    [TestMethod]
    public void Interpret_InsufficientData_ShouldThrowParseException()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));


        var data = new byte[] { 0x01, 0x02 };


        Assert.Throws<Exception>(() => InvokeInterpret(interpreter, data));
    }

    [TestMethod]
    public void Interpret_Int32LittleEndian_ShouldParseCorrectly()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));

        // 0x12345678 in little-endian
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result, "Value");

        // Assert
        Assert.AreEqual(0x12345678, value);
    }

    [TestMethod]
    public void Interpret_Int32BigEndian_ShouldParseCorrectly()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.BigEndian));

        // 0x12345678 in big-endian
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result, "Value");

        // Assert
        Assert.AreEqual(0x12345678, value);
    }

    [TestMethod]
    public void Interpret_Byte_ShouldParseCorrectly()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Flags", PrimitiveTypeName.Byte, Endianness.NotApplicable));

        var data = new byte[] { 0xAB };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<byte>(result, "Flags");

        // Assert
        Assert.AreEqual((byte)0xAB, value);
    }

    [TestMethod]
    public void Interpret_Int16LittleEndian_ShouldParseCorrectly()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Short, Endianness.LittleEndian));

        // 0x1234 in little-endian
        var data = new byte[] { 0x34, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<short>(result, "Value");

        // Assert
        Assert.AreEqual((short)0x1234, value);
    }

    [TestMethod]
    public void Interpret_Float_ShouldParseCorrectly()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Float, Endianness.LittleEndian));

        // 3.14f in little-endian IEEE 754
        var data = BitConverter.GetBytes(3.14f);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<float>(result, "Value");

        // Assert
        Assert.AreEqual(3.14f, value, 0.001f);
    }

    [TestMethod]
    public void Interpret_Double_ShouldParseCorrectly()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Double, Endianness.LittleEndian));

        // 3.14159 in little-endian IEEE 754
        var data = BitConverter.GetBytes(3.14159);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<double>(result, "Value");

        // Assert
        Assert.AreEqual(3.14159, value, 0.00001);
    }

    [TestMethod]
    public void Interpret_MultipleFields_ShouldParseSequentially()
    {
        // Arrange: binary Header { Magic: int le, Version: short le, Flags: byte }
        var interpreter = CreateAndCompileInterpreter("Header",
            CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian),
            CreatePrimitiveField("Version", PrimitiveTypeName.Short, Endianness.LittleEndian),
            CreatePrimitiveField("Flags", PrimitiveTypeName.Byte, Endianness.NotApplicable));

        // Magic = 0xDEADBEEF, Version = 0x0102, Flags = 0xFF
        var data = new byte[]
        {
            0xEF, 0xBE, 0xAD, 0xDE, // Magic (little-endian)
            0x02, 0x01, // Version (little-endian)
            0xFF // Flags
        };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var magic = GetPropertyValue<int>(result, "Magic");
        var version = GetPropertyValue<short>(result, "Version");
        var flags = GetPropertyValue<byte>(result, "Flags");

        // Assert
        Assert.AreEqual(unchecked((int)0xDEADBEEF), magic);
        Assert.AreEqual((short)0x0102, version);
        Assert.AreEqual((byte)0xFF, flags);
    }

    [TestMethod]
    public void Interpret_AllPrimitiveTypes_ShouldParseCorrectly()
    {
        // Arrange: Test all 10 primitive types
        var interpreter = CreateAndCompileInterpreter("AllTypes",
            CreatePrimitiveField("F1", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("F2", PrimitiveTypeName.SByte, Endianness.NotApplicable),
            CreatePrimitiveField("F3", PrimitiveTypeName.Short, Endianness.LittleEndian),
            CreatePrimitiveField("F4", PrimitiveTypeName.UShort, Endianness.LittleEndian),
            CreatePrimitiveField("F5", PrimitiveTypeName.Int, Endianness.LittleEndian),
            CreatePrimitiveField("F6", PrimitiveTypeName.UInt, Endianness.LittleEndian),
            CreatePrimitiveField("F7", PrimitiveTypeName.Long, Endianness.LittleEndian),
            CreatePrimitiveField("F8", PrimitiveTypeName.ULong, Endianness.LittleEndian),
            CreatePrimitiveField("F9", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("F10", PrimitiveTypeName.Double, Endianness.LittleEndian));

        // Build test data: 1 + 1 + 2 + 2 + 4 + 4 + 8 + 8 + 4 + 8 = 42 bytes
        var ms = new MemoryStream();
        var writer = new BinaryWriter(ms);
        writer.Write((byte)0x01); // F1
        writer.Write((sbyte)-1); // F2
        writer.Write((short)1000); // F3
        writer.Write((ushort)60000); // F4
        writer.Write(123456); // F5
        writer.Write(4000000000); // F6
        writer.Write(123456789012345L); // F7
        writer.Write(18000000000000000000UL); // F8
        writer.Write(1.5f); // F9
        writer.Write(2.5); // F10
        var data = ms.ToArray();

        // Act
        var result = InvokeInterpret(interpreter, data);

        // Assert
        Assert.AreEqual((byte)0x01, GetPropertyValue<byte>(result, "F1"));
        Assert.AreEqual((sbyte)-1, GetPropertyValue<sbyte>(result, "F2"));
        Assert.AreEqual((short)1000, GetPropertyValue<short>(result, "F3"));
        Assert.AreEqual((ushort)60000, GetPropertyValue<ushort>(result, "F4"));
        Assert.AreEqual(123456, GetPropertyValue<int>(result, "F5"));
        Assert.AreEqual(4000000000, GetPropertyValue<uint>(result, "F6"));
        Assert.AreEqual(123456789012345L, GetPropertyValue<long>(result, "F7"));
        Assert.AreEqual(18000000000000000000UL, GetPropertyValue<ulong>(result, "F8"));
        Assert.AreEqual(1.5f, GetPropertyValue<float>(result, "F9"));
        Assert.AreEqual(2.5, GetPropertyValue<double>(result, "F10"));
    }
}
