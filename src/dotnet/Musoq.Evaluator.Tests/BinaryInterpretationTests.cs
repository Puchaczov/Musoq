using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Build;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Integration tests that generate interpreter code and actually parse binary data.
/// </summary>
[TestClass]
public class BinaryInterpretationTests
{
    #region Byte Array Tests

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

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public void Interpret_InsufficientData_ShouldThrowParseException()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));


        var data = new byte[] { 0x01, 0x02 };


        Assert.Throws<Exception>(() => InvokeInterpret(interpreter, data));
    }

    #endregion

    #region Primitive Type Parsing Tests

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

    #endregion

    #region Multiple Fields Tests

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

    #endregion

    #region String Type Tests

    [TestMethod]
    public void Interpret_String_Utf8_ShouldParseCorrectly()
    {
        // Arrange: binary Record { Name: string[5] utf8 }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(5);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8);
        var fields = new[]
        {
            new FieldDefinitionNode("Name", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        var data = Encoding.UTF8.GetBytes("Hello");

        // Act
        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        // Assert
        Assert.AreEqual("Hello", name);
    }

    [TestMethod]
    public void Interpret_String_Ascii_ShouldParseCorrectly()
    {
        // Arrange: binary Record { Label: string[4] ascii }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(4);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Ascii);
        var fields = new[]
        {
            new FieldDefinitionNode("Label", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        var data = Encoding.ASCII.GetBytes("TEST");

        // Act
        var result = InvokeInterpret(interpreter, data);
        var label = GetPropertyValue<string>(result, "Label");

        // Assert
        Assert.AreEqual("TEST", label);
    }

    [TestMethod]
    public void Interpret_String_WithTrimModifier_ShouldTrimWhitespace()
    {
        // Arrange: binary Record { Name: string[10] utf8 trim }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(10);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.Trim);
        var fields = new[]
        {
            new FieldDefinitionNode("Name", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        var data = Encoding.UTF8.GetBytes("  Hello   ");

        // Act
        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        // Assert
        Assert.AreEqual("Hello", name);
    }

    [TestMethod]
    public void Interpret_String_WithRTrimModifier_ShouldTrimTrailingWhitespace()
    {
        // Arrange: binary Record { Name: string[10] utf8 rtrim }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(10);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.RTrim);
        var fields = new[]
        {
            new FieldDefinitionNode("Name", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        var data = Encoding.UTF8.GetBytes("  Hello   ");

        // Act
        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        // Assert
        Assert.AreEqual("  Hello", name);
    }

    [TestMethod]
    public void Interpret_String_WithLTrimModifier_ShouldTrimLeadingWhitespace()
    {
        // Arrange: binary Record { Name: string[10] utf8 ltrim }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(10);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.LTrim);
        var fields = new[]
        {
            new FieldDefinitionNode("Name", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        var data = Encoding.UTF8.GetBytes("  Hello   ");

        // Act
        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        // Assert
        Assert.AreEqual("Hello   ", name);
    }

    [TestMethod]
    public void Interpret_String_WithNullTermModifier_ShouldStopAtNullByte()
    {
        // Arrange: binary Record { Name: string[10] utf8 nullterm }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(10);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.NullTerm);
        var fields = new[]
        {
            new FieldDefinitionNode("Name", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        // "Hello" followed by null and garbage
        var data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        // Assert
        Assert.AreEqual("Hello", name);
    }

    [TestMethod]
    public void Interpret_String_WithNullTermAndTrim_ShouldApplyBoth()
    {
        // Arrange: binary Record { Name: string[12] utf8 nullterm trim }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(12);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8,
            StringModifier.NullTerm | StringModifier.Trim);
        var fields = new[]
        {
            new FieldDefinitionNode("Name", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        // "  Hi  " followed by null and garbage
        var data = new byte[] { 0x20, 0x20, 0x48, 0x69, 0x20, 0x20, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        // Assert
        Assert.AreEqual("Hi", name);
    }

    [TestMethod]
    public void Interpret_String_Utf16Le_ShouldParseCorrectly()
    {
        // Arrange: binary Record { Name: string[8] utf16le }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(8);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf16Le);
        var fields = new[]
        {
            new FieldDefinitionNode("Name", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        var data = Encoding.Unicode.GetBytes("Test"); // 4 chars * 2 bytes = 8 bytes

        // Act
        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        // Assert
        Assert.AreEqual("Test", name);
    }

    [TestMethod]
    public void Interpret_String_Utf16Be_ShouldParseCorrectly()
    {
        // Arrange: binary Record { Name: string[8] utf16be }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(8);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf16Be);
        var fields = new[]
        {
            new FieldDefinitionNode("Name", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        var data = Encoding.BigEndianUnicode.GetBytes("Test"); // 4 chars * 2 bytes = 8 bytes

        // Act
        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        // Assert
        Assert.AreEqual("Test", name);
    }

    [TestMethod]
    public void Interpret_String_Latin1_ShouldParseCorrectly()
    {
        // Arrange: binary Record { Text: string[5] latin1 }
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(5);
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Latin1);
        var fields = new[]
        {
            new FieldDefinitionNode("Text", stringType)
        };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        // Latin-1 bytes for "caf\xe9" (café with Latin-1 é)
        var data = new byte[] { 0x63, 0x61, 0x66, 0xE9, 0x73 }; // cafés

        // Act
        var result = InvokeInterpret(interpreter, data);
        var text = GetPropertyValue<string>(result, "Text");

        // Assert
        Assert.AreEqual("cafés", text);
    }

    #endregion

    #region Dynamic Size Tests

    [TestMethod]
    public void Interpret_ByteArray_WithDynamicSize_ShouldUsePreviousField()
    {
        // Arrange: binary Record { Length: short le, Data: byte[Length] }
        var registry = new SchemaRegistry();
        var lengthField = new FieldDefinitionNode("Length",
            new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian));
        var dataField = new FieldDefinitionNode("Data",
            new ByteArrayTypeNode(new AccessColumnNode("Length", string.Empty, TextSpan.Empty)));

        var fields = new[] { lengthField, dataField };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        // Length = 4 (little-endian), followed by 4 bytes of data
        var data = new byte[] { 0x04, 0x00, 0xAA, 0xBB, 0xCC, 0xDD };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var length = GetPropertyValue<short>(result, "Length");
        var payload = GetPropertyValue<byte[]>(result, "Data");

        // Assert
        Assert.AreEqual((short)4, length);
        Assert.IsNotNull(payload);
        Assert.HasCount(4, payload);
        Assert.AreEqual((byte)0xAA, payload[0]);
        Assert.AreEqual((byte)0xBB, payload[1]);
        Assert.AreEqual((byte)0xCC, payload[2]);
        Assert.AreEqual((byte)0xDD, payload[3]);
    }

    [TestMethod]
    public void Interpret_String_WithDynamicSize_ShouldUsePreviousField()
    {
        // Arrange: binary Record { NameLen: byte, Name: string[NameLen] utf8 }
        var registry = new SchemaRegistry();
        var nameLenField = new FieldDefinitionNode("NameLen",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable));
        var nameField = new FieldDefinitionNode("Name",
            new StringTypeNode(
                new AccessColumnNode("NameLen", string.Empty, TextSpan.Empty),
                StringEncoding.Utf8));

        var fields = new[] { nameLenField, nameField };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        // NameLen = 5, followed by "Hello"
        var data = new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var nameLen = GetPropertyValue<byte>(result, "NameLen");
        var name = GetPropertyValue<string>(result, "Name");

        // Assert
        Assert.AreEqual((byte)5, nameLen);
        Assert.AreEqual("Hello", name);
    }

    #endregion

    #region Nested Schema Tests

    [TestMethod]
    public void Interpret_NestedSchema_ShouldParseInline()
    {
        // Arrange: Create Point schema with X, Y floats
        var registry = new SchemaRegistry();

        var pointFields = new[]
        {
            CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("Y", PrimitiveTypeName.Float, Endianness.LittleEndian)
        };
        var pointSchema = new BinarySchemaNode("Point", pointFields);
        registry.Register("Point", pointSchema);

        // Create Vertex schema with nested Point
        var vertexFields = new[]
        {
            new FieldDefinitionNode("Id", new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian)),
            new FieldDefinitionNode("Position", new SchemaReferenceTypeNode("Point"))
        };
        var vertexSchema = new BinarySchemaNode("Vertex", vertexFields);
        registry.Register("Vertex", vertexSchema);

        var interpreter = CompileInterpreter(registry, "Vertex");

        // Data: Id (4 bytes) + Point.X (4 bytes) + Point.Y (4 bytes)
        var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(42)); // Id = 42
        ms.Write(BitConverter.GetBytes(1.5f)); // Position.X = 1.5
        ms.Write(BitConverter.GetBytes(2.5f)); // Position.Y = 2.5
        var data = ms.ToArray();

        // Act
        var result = InvokeInterpret(interpreter, data);
        var id = GetPropertyValue<int>(result, "Id");
        var position = GetPropertyValue<object>(result, "Position");
        var posX = GetPropertyValue<float>(position, "X");
        var posY = GetPropertyValue<float>(position, "Y");

        // Assert
        Assert.AreEqual(42, id);
        Assert.AreEqual(1.5f, posX, 0.001f);
        Assert.AreEqual(2.5f, posY, 0.001f);
    }

    [TestMethod]
    public void Interpret_MultipleNestedSchemas_ShouldParseSequentially()
    {
        // Arrange: Create Color schema
        var registry = new SchemaRegistry();

        var colorFields = new[]
        {
            CreatePrimitiveField("R", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("G", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("B", PrimitiveTypeName.Byte, Endianness.NotApplicable)
        };
        var colorSchema = new BinarySchemaNode("Color", colorFields);
        registry.Register("Color", colorSchema);

        // Create Point schema
        var pointFields = new[]
        {
            CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("Y", PrimitiveTypeName.Float, Endianness.LittleEndian)
        };
        var pointSchema = new BinarySchemaNode("Point", pointFields);
        registry.Register("Point", pointSchema);

        // Create Vertex schema with both nested
        var vertexFields = new[]
        {
            new FieldDefinitionNode("Position", new SchemaReferenceTypeNode("Point")),
            new FieldDefinitionNode("Color", new SchemaReferenceTypeNode("Color"))
        };
        var vertexSchema = new BinarySchemaNode("Vertex", vertexFields);
        registry.Register("Vertex", vertexSchema);

        var interpreter = CompileInterpreter(registry, "Vertex");

        // Data: Point.X, Point.Y, Color.R, Color.G, Color.B
        var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(3.0f)); // Position.X = 3.0
        ms.Write(BitConverter.GetBytes(4.0f)); // Position.Y = 4.0
        ms.WriteByte(255); // Color.R = 255
        ms.WriteByte(128); // Color.G = 128
        ms.WriteByte(64); // Color.B = 64
        var data = ms.ToArray();

        // Act
        var result = InvokeInterpret(interpreter, data);
        var position = GetPropertyValue<object>(result, "Position");
        var color = GetPropertyValue<object>(result, "Color");

        // Assert Position
        Assert.AreEqual(3.0f, GetPropertyValue<float>(position, "X"), 0.001f);
        Assert.AreEqual(4.0f, GetPropertyValue<float>(position, "Y"), 0.001f);

        // Assert Color
        Assert.AreEqual((byte)255, GetPropertyValue<byte>(color, "R"));
        Assert.AreEqual((byte)128, GetPropertyValue<byte>(color, "G"));
        Assert.AreEqual((byte)64, GetPropertyValue<byte>(color, "B"));
    }

    #endregion

    #region Schema Array Tests

    [TestMethod]
    public void Interpret_ArrayOfPrimitives_ShouldParseFixedCount()
    {
        // Arrange: Schema with count field and array
        var registry = new SchemaRegistry();

        var primitiveType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var arrayType = new ArrayTypeNode(primitiveType, new IntegerNode(3));

        var fields = new[]
        {
            new FieldDefinitionNode("Values", arrayType)
        };
        var schema = new BinarySchemaNode("IntArray", fields);
        registry.Register("IntArray", schema);

        var interpreter = CompileInterpreter(registry, "IntArray");

        // Data: 3 ints
        var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(10));
        ms.Write(BitConverter.GetBytes(20));
        ms.Write(BitConverter.GetBytes(30));
        var data = ms.ToArray();

        // Act
        var result = InvokeInterpret(interpreter, data);
        var values = GetPropertyValue<int[]>(result, "Values");

        // Assert
        Assert.HasCount(3, values);
        Assert.AreEqual(10, values[0]);
        Assert.AreEqual(20, values[1]);
        Assert.AreEqual(30, values[2]);
    }

    [TestMethod]
    public void Interpret_ArrayOfPrimitives_WithDynamicCount_ShouldUsePreviousField()
    {
        // Arrange: Schema with count field and dynamic array
        var registry = new SchemaRegistry();

        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var primitiveType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);
        var sizeRef = new AccessColumnNode("Count", string.Empty, TextSpan.Empty);
        var arrayType = new ArrayTypeNode(primitiveType, sizeRef);
        var arrayField = new FieldDefinitionNode("Values", arrayType);

        var schema = new BinarySchemaNode("DynamicArray", new[] { countField, arrayField });
        registry.Register("DynamicArray", schema);

        var interpreter = CompileInterpreter(registry, "DynamicArray");

        // Data: Count = 4, followed by 4 shorts
        var ms = new MemoryStream();
        ms.WriteByte(4); // Count = 4
        ms.Write(BitConverter.GetBytes((short)100));
        ms.Write(BitConverter.GetBytes((short)200));
        ms.Write(BitConverter.GetBytes((short)300));
        ms.Write(BitConverter.GetBytes((short)400));
        var data = ms.ToArray();

        // Act
        var result = InvokeInterpret(interpreter, data);
        var count = GetPropertyValue<byte>(result, "Count");
        var values = GetPropertyValue<short[]>(result, "Values");

        // Assert
        Assert.AreEqual((byte)4, count);
        Assert.HasCount(4, values);
        Assert.AreEqual((short)100, values[0]);
        Assert.AreEqual((short)200, values[1]);
        Assert.AreEqual((short)300, values[2]);
        Assert.AreEqual((short)400, values[3]);
    }

    [TestMethod]
    public void Interpret_ArrayOfSchemas_ShouldParseAllElements()
    {
        // Arrange: Point schema
        var registry = new SchemaRegistry();

        var pointFields = new[]
        {
            CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("Y", PrimitiveTypeName.Float, Endianness.LittleEndian)
        };
        var pointSchema = new BinarySchemaNode("Point", pointFields);
        registry.Register("Point", pointSchema);

        // Mesh schema with array of Points
        var countField = CreatePrimitiveField("VertexCount", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var sizeRef = new AccessColumnNode("VertexCount", string.Empty, TextSpan.Empty);
        var arrayType = new ArrayTypeNode(new SchemaReferenceTypeNode("Point"), sizeRef);
        var verticesField = new FieldDefinitionNode("Vertices", arrayType);

        var meshSchema = new BinarySchemaNode("Mesh", new[] { countField, verticesField });
        registry.Register("Mesh", meshSchema);

        var interpreter = CompileInterpreter(registry, "Mesh");

        // Data: VertexCount = 2, followed by 2 Points (each 8 bytes)
        var ms = new MemoryStream();
        ms.Write(BitConverter.GetBytes(2)); // VertexCount = 2
        ms.Write(BitConverter.GetBytes(1.0f)); // Point[0].X
        ms.Write(BitConverter.GetBytes(2.0f)); // Point[0].Y
        ms.Write(BitConverter.GetBytes(3.0f)); // Point[1].X
        ms.Write(BitConverter.GetBytes(4.0f)); // Point[1].Y
        var data = ms.ToArray();

        // Act
        var result = InvokeInterpret(interpreter, data);
        var vertexCount = GetPropertyValue<int>(result, "VertexCount");
        var vertices = GetPropertyValue<object[]>(result, "Vertices");

        // Assert
        Assert.AreEqual(2, vertexCount);
        Assert.HasCount(2, vertices);

        // Check first point
        Assert.AreEqual(1.0f, GetPropertyValue<float>(vertices[0], "X"), 0.001f);
        Assert.AreEqual(2.0f, GetPropertyValue<float>(vertices[0], "Y"), 0.001f);

        // Check second point
        Assert.AreEqual(3.0f, GetPropertyValue<float>(vertices[1], "X"), 0.001f);
        Assert.AreEqual(4.0f, GetPropertyValue<float>(vertices[1], "Y"), 0.001f);
    }

    [TestMethod]
    public void Interpret_ArrayOfSchemas_WithZeroCount_ShouldReturnEmptyArray()
    {
        // Arrange: Point schema
        var registry = new SchemaRegistry();

        var pointFields = new[]
        {
            CreatePrimitiveField("X", PrimitiveTypeName.Float, Endianness.LittleEndian),
            CreatePrimitiveField("Y", PrimitiveTypeName.Float, Endianness.LittleEndian)
        };
        var pointSchema = new BinarySchemaNode("Point", pointFields);
        registry.Register("Point", pointSchema);

        // Mesh schema with array of Points
        var countField = CreatePrimitiveField("VertexCount", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var sizeRef = new AccessColumnNode("VertexCount", string.Empty, TextSpan.Empty);
        var arrayType = new ArrayTypeNode(new SchemaReferenceTypeNode("Point"), sizeRef);
        var verticesField = new FieldDefinitionNode("Vertices", arrayType);

        var meshSchema = new BinarySchemaNode("Mesh", new[] { countField, verticesField });
        registry.Register("Mesh", meshSchema);

        var interpreter = CompileInterpreter(registry, "Mesh");

        // Data: VertexCount = 0, no point data
        var data = BitConverter.GetBytes(0); // VertexCount = 0

        // Act
        var result = InvokeInterpret(interpreter, data);
        var vertexCount = GetPropertyValue<int>(result, "VertexCount");
        var vertices = GetPropertyValue<object[]>(result, "Vertices");

        // Assert
        Assert.AreEqual(0, vertexCount);
        Assert.IsEmpty(vertices);
    }

    #endregion

    #region Helper Methods

    private static object CreateAndCompileInterpreter(string schemaName, params FieldDefinitionNode[] fields)
    {
        var registry = new SchemaRegistry();
        var schema = new BinarySchemaNode(schemaName, fields);
        registry.Register(schemaName, schema);

        return CompileInterpreter(registry, schemaName);
    }

    private static object CompileInterpreter(SchemaRegistry registry, string schemaName)
    {
        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        var success = compilationUnit.Compile();
        if (!success)
            throw new InvalidOperationException(
                $"Compilation failed: {string.Join(Environment.NewLine, compilationUnit.GetErrorMessages())}");

        var type = compilationUnit.GetInterpreterType(schemaName);
        if (type == null) throw new InvalidOperationException($"Type '{schemaName}' not found in compiled assembly.");

        return Activator.CreateInstance(type)!;
    }

    private static object CompileInterpreterForGenericInstantiation(SchemaRegistry registry, string genericSchemaName,
        string[] typeArguments)
    {
        var generator = new InterpreterCodeGenerator(registry);
        var code = generator.GenerateAll();

        var compilationUnit = new InterpreterCompilationUnit(
            $"TestAssembly_{Guid.NewGuid():N}",
            code);

        var success = compilationUnit.Compile();
        if (!success)
            throw new InvalidOperationException(
                $"Compilation failed: {string.Join(Environment.NewLine, compilationUnit.GetErrorMessages())}");


        var genericType = compilationUnit.GetInterpreterType(genericSchemaName);
        if (genericType == null)
            throw new InvalidOperationException($"Generic type '{genericSchemaName}' not found in compiled assembly.");


        var concreteTypes = new Type[typeArguments.Length];
        for (var i = 0; i < typeArguments.Length; i++)
        {
            var argType = compilationUnit.GetInterpreterType(typeArguments[i]);
            if (argType == null)
                throw new InvalidOperationException(
                    $"Type argument '{typeArguments[i]}' not found in compiled assembly.");
            concreteTypes[i] = argType;
        }

        var closedType = genericType.MakeGenericType(concreteTypes);
        return Activator.CreateInstance(closedType)!;
    }

    private static object InvokeInterpret(object interpreter, byte[] data)
    {
        var interpreterType = interpreter.GetType();

        var interpretMethod = interpreterType.GetMethod("Interpret",
            new[] { typeof(byte[]) });

        if (interpretMethod == null) throw new InvalidOperationException("Interpret(byte[]) method not found");

        return interpretMethod.Invoke(interpreter, new object[] { data })!;
    }

    private static T GetPropertyValue<T>(object obj, string propertyName)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        if (prop == null)
            throw new InvalidOperationException($"Property '{propertyName}' not found");

        return (T)prop.GetValue(obj)!;
    }

    private static FieldDefinitionNode CreatePrimitiveField(
        string name,
        PrimitiveTypeName typeName,
        Endianness endianness)
    {
        var typeNode = new PrimitiveTypeNode(typeName, endianness);
        return new FieldDefinitionNode(name, typeNode);
    }

    private static object CreateAndCompileInterpreterWithSchema(string schemaName, params SchemaFieldNode[] fields)
    {
        var registry = new SchemaRegistry();
        var schema = new BinarySchemaNode(schemaName, fields);
        registry.Register(schemaName, schema);

        return CompileInterpreter(registry, schemaName);
    }

    #endregion

    #region Conditional Field Tests (when clause)

    [TestMethod]
    public void Interpret_ConditionalField_WhenTrue_ShouldParseField()
    {
        // Arrange: Message with conditional Payload field
        // binary Message { HasPayload: byte, Payload: int le when HasPayload <> 0 }
        var registry = new SchemaRegistry();

        var hasPayloadField = CreatePrimitiveField("HasPayload", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        // Create when condition: HasPayload <> 0
        var hasPayloadRef = new IdentifierNode("HasPayload");
        var zeroNode = new IntegerNode(0);
        var condition = new DiffNode(hasPayloadRef, zeroNode);

        var payloadType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var payloadField = new FieldDefinitionNode("Payload", payloadType, null, null, condition);

        var schema = new BinarySchemaNode("Message", new[] { hasPayloadField, payloadField });
        registry.Register("Message", schema);

        var interpreter = CompileInterpreter(registry, "Message");

        // Data: HasPayload = 1, Payload = 0x12345678
        var data = new byte[] { 0x01, 0x78, 0x56, 0x34, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var hasPayload = GetPropertyValue<byte>(result, "HasPayload");
        var payload = GetPropertyValue<int?>(result, "Payload");

        // Assert
        Assert.AreEqual((byte)1, hasPayload);
        Assert.AreEqual(0x12345678, payload);
    }

    [TestMethod]
    public void Interpret_ConditionalField_WhenFalse_ShouldBeNull()
    {
        // Arrange: Message with conditional Payload field
        var registry = new SchemaRegistry();

        var hasPayloadField = CreatePrimitiveField("HasPayload", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        // Create when condition: HasPayload <> 0
        var hasPayloadRef = new IdentifierNode("HasPayload");
        var zeroNode = new IntegerNode(0);
        var condition = new DiffNode(hasPayloadRef, zeroNode);

        var payloadType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var payloadField = new FieldDefinitionNode("Payload", payloadType, null, null, condition);

        var schema = new BinarySchemaNode("Message", new[] { hasPayloadField, payloadField });
        registry.Register("Message", schema);

        var interpreter = CompileInterpreter(registry, "Message");

        // Data: HasPayload = 0, no payload data
        var data = new byte[] { 0x00 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var hasPayload = GetPropertyValue<byte>(result, "HasPayload");
        var payload = GetPropertyValue<int?>(result, "Payload");

        // Assert
        Assert.AreEqual((byte)0, hasPayload);
        Assert.IsNull(payload);
    }

    [TestMethod]
    public void Interpret_ConditionalField_NoCursorAdvanceWhenFalse()
    {
        // Arrange: Message with conditional field followed by required field
        // binary Message { HasExtra: byte, Extra: int le when HasExtra <> 0, Required: short le }
        var registry = new SchemaRegistry();

        var hasExtraField = CreatePrimitiveField("HasExtra", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var hasExtraRef = new IdentifierNode("HasExtra");
        var zeroNode = new IntegerNode(0);
        var condition = new DiffNode(hasExtraRef, zeroNode);

        var extraType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var extraField = new FieldDefinitionNode("Extra", extraType, null, null, condition);

        var requiredField = CreatePrimitiveField("Required", PrimitiveTypeName.Short, Endianness.LittleEndian);

        var schema = new BinarySchemaNode("Message", new[] { hasExtraField, extraField, requiredField });
        registry.Register("Message", schema);

        var interpreter = CompileInterpreter(registry, "Message");

        // Data: HasExtra = 0, Required = 0x1234 (should immediately follow HasExtra since Extra is skipped)
        var data = new byte[] { 0x00, 0x34, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var hasExtra = GetPropertyValue<byte>(result, "HasExtra");
        var extra = GetPropertyValue<int?>(result, "Extra");
        var required = GetPropertyValue<short>(result, "Required");

        // Assert
        Assert.AreEqual((byte)0, hasExtra);
        Assert.IsNull(extra);
        Assert.AreEqual((short)0x1234, required);
    }

    #endregion

    #region Check Constraint Tests

    [TestMethod]
    public void Interpret_CheckConstraint_WhenValid_ShouldParse()
    {
        // Arrange: Header with magic number check
        // binary Header { Magic: int le check(Magic = 0xDEADBEEF) }
        var registry = new SchemaRegistry();

        var magicType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);

        // Create check constraint: Magic = 0xDEADBEEF
        var magicRef = new IdentifierNode("Magic");
        var expectedMagic = new IntegerNode(unchecked((int)0xDEADBEEF));
        var checkExpr = new EqualityNode(magicRef, expectedMagic);
        var constraint = new FieldConstraintNode(checkExpr);

        var magicField = new FieldDefinitionNode("Magic", magicType, constraint);

        var schema = new BinarySchemaNode("Header", new[] { magicField });
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        // Data: Magic = 0xDEADBEEF (little-endian)
        var data = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var magic = GetPropertyValue<int>(result, "Magic");

        // Assert - should parse successfully
        Assert.AreEqual(unchecked((int)0xDEADBEEF), magic);
    }

    [TestMethod]
    public void Interpret_CheckConstraint_WhenInvalid_ShouldThrow()
    {
        var registry = new SchemaRegistry();

        var magicType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);


        var magicRef = new IdentifierNode("Magic");
        var expectedMagic = new IntegerNode(unchecked((int)0xDEADBEEF));
        var checkExpr = new EqualityNode(magicRef, expectedMagic);
        var constraint = new FieldConstraintNode(checkExpr);

        var magicField = new FieldDefinitionNode("Magic", magicType, constraint);

        var schema = new BinarySchemaNode("Header", new[] { magicField });
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");


        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };


        Assert.Throws<Exception>(() => InvokeInterpret(interpreter, data));
    }

    [TestMethod]
    public void Interpret_CheckConstraint_RangeValidation_ShouldWork()
    {
        // Arrange: Version field with range check (1 <= Version <= 5)
        var registry = new SchemaRegistry();

        var versionType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);

        // Create check constraint: Version >= 1 AND Version <= 5
        var versionRef1 = new IdentifierNode("Version");
        var versionRef2 = new IdentifierNode("Version");
        var one = new IntegerNode(1);
        var five = new IntegerNode(5);
        var gte1 = new GreaterOrEqualNode(versionRef1, one);
        var lte5 = new LessOrEqualNode(versionRef2, five);
        var checkExpr = new AndNode(gte1, lte5);
        var constraint = new FieldConstraintNode(checkExpr);

        var versionField = new FieldDefinitionNode("Version", versionType, constraint);

        var schema = new BinarySchemaNode("Header", new[] { versionField });
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        // Data: Version = 3 (valid)
        var data = new byte[] { 0x03, 0x00 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var version = GetPropertyValue<short>(result, "Version");

        // Assert
        Assert.AreEqual((short)3, version);
    }

    #endregion

    #region Absolute Positioning Tests (at clause)

    [TestMethod]
    public void Interpret_AtPositioning_LiteralOffset_ShouldReadFromPosition()
    {
        // Arrange: binary PeHeader { DosMagic: string[2] ascii at 0, PeOffset: int le at 60 }
        // 60 = 0x3C (PE header pointer location in DOS header)
        var registry = new SchemaRegistry();

        // DosMagic at offset 0
        var magicType = new StringTypeNode(new IntegerNode(2), StringEncoding.Ascii);
        var magicField = new FieldDefinitionNode("DosMagic", magicType, null, new IntegerNode(0));

        // PeOffset at offset 60 (0x3C)
        var peOffsetType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var peOffsetField = new FieldDefinitionNode("PeOffset", peOffsetType, null, new IntegerNode(60));

        var schema = new BinarySchemaNode("PeHeader", new[] { magicField, peOffsetField });
        registry.Register("PeHeader", schema);

        var interpreter = CompileInterpreter(registry, "PeHeader");

        // Create test data: 64 bytes, "MZ" at offset 0, PE offset value at offset 60
        var data = new byte[64];
        data[0] = (byte)'M';
        data[1] = (byte)'Z';
        // PE offset at byte 60 (little-endian): 0x00000080 = 128
        data[60] = 0x80;
        data[61] = 0x00;
        data[62] = 0x00;
        data[63] = 0x00;

        // Act
        var result = InvokeInterpret(interpreter, data);
        var dosMagic = GetPropertyValue<string>(result, "DosMagic");
        var peOffset = GetPropertyValue<int>(result, "PeOffset");

        // Assert
        Assert.AreEqual("MZ", dosMagic);
        Assert.AreEqual(128, peOffset);
    }

    [TestMethod]
    public void Interpret_AtPositioning_HexOffset_ShouldReadFromPosition()
    {
        // Arrange: binary Header { Signature: int le at 0x10 }
        var registry = new SchemaRegistry();

        var sigType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var sigField = new FieldDefinitionNode("Signature", sigType, null, new HexIntegerNode(0x10));

        var schema = new BinarySchemaNode("Header", new[] { sigField });
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        // Create test data: signature at offset 0x10 (16)
        var data = new byte[20];
        data[16] = 0xEF;
        data[17] = 0xBE;
        data[18] = 0xAD;
        data[19] = 0xDE;

        // Act
        var result = InvokeInterpret(interpreter, data);
        var signature = GetPropertyValue<int>(result, "Signature");

        // Assert
        Assert.AreEqual(unchecked((int)0xDEADBEEF), signature);
    }

    [TestMethod]
    public void Interpret_AtPositioning_FieldReference_ShouldReadFromDynamicPosition()
    {
        // Arrange: binary Header { DataOffset: short le, Data: int le at DataOffset }
        var registry = new SchemaRegistry();

        // First field: DataOffset (tells us where to read Data)
        var dataOffsetType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);
        var dataOffsetField = new FieldDefinitionNode("DataOffset", dataOffsetType);

        // Second field: Data at position specified by DataOffset
        var dataType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var atOffset = new IdentifierNode("DataOffset");
        var dataField = new FieldDefinitionNode("Data", dataType, null, atOffset);

        var schema = new BinarySchemaNode("Header", new[] { dataOffsetField, dataField });
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        // Create test data: 
        // Bytes 0-1: DataOffset = 8 (pointing to byte 8)
        // Bytes 2-7: garbage
        // Bytes 8-11: Data = 0x12345678
        var data = new byte[12];
        data[0] = 0x08; // DataOffset = 8 (little-endian)
        data[1] = 0x00;
        // Bytes 2-7 are garbage (skipped)
        data[8] = 0x78;
        data[9] = 0x56;
        data[10] = 0x34;
        data[11] = 0x12;

        // Act
        var result = InvokeInterpret(interpreter, data);
        var dataOffset = GetPropertyValue<short>(result, "DataOffset");
        var dataValue = GetPropertyValue<int>(result, "Data");

        // Assert
        Assert.AreEqual((short)8, dataOffset);
        Assert.AreEqual(0x12345678, dataValue);
    }

    [TestMethod]
    public void Interpret_AtPositioning_Expression_ShouldCalculatePosition()
    {
        // Arrange: binary Header { BaseOffset: int le, Value: short le at BaseOffset + 4 }
        var registry = new SchemaRegistry();

        // First field: BaseOffset
        var baseOffsetType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var baseOffsetField = new FieldDefinitionNode("BaseOffset", baseOffsetType);

        // Second field: Value at position (BaseOffset + 4)
        var valueType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);
        var baseOffsetRef = new IdentifierNode("BaseOffset");
        var offsetExpr = new AddNode(baseOffsetRef, new IntegerNode(4));
        var valueField = new FieldDefinitionNode("Value", valueType, null, offsetExpr);

        var schema = new BinarySchemaNode("Header", new[] { baseOffsetField, valueField });
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        // Create test data: 
        // Bytes 0-3: BaseOffset = 10
        // Bytes 4-13: padding
        // Bytes 14-15: Value = 0x1234 (at position 10 + 4 = 14)
        var data = new byte[16];
        data[0] = 0x0A; // BaseOffset = 10
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x00;
        // Value at position 14 (10 + 4)
        data[14] = 0x34;
        data[15] = 0x12;

        // Act
        var result = InvokeInterpret(interpreter, data);
        var baseOffset = GetPropertyValue<int>(result, "BaseOffset");
        var value = GetPropertyValue<short>(result, "Value");

        // Assert
        Assert.AreEqual(10, baseOffset);
        Assert.AreEqual((short)0x1234, value);
    }

    [TestMethod]
    public void Interpret_AtPositioning_BackwardJump_ShouldRereadEarlierData()
    {
        // Arrange: binary Record { FieldA: int le, FieldB: short le at 0, FieldC: byte at 2 }
        // This demonstrates backward positioning to re-read earlier data
        var registry = new SchemaRegistry();

        // FieldA at implicit position (0-3)
        var fieldAType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var fieldA = new FieldDefinitionNode("FieldA", fieldAType);

        // FieldB jumps back to position 0
        var fieldBType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);
        var fieldB = new FieldDefinitionNode("FieldB", fieldBType, null, new IntegerNode(0));

        // FieldC jumps to position 2
        var fieldCType = new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var fieldC = new FieldDefinitionNode("FieldC", fieldCType, null, new IntegerNode(2));

        var schema = new BinarySchemaNode("Record", new[] { fieldA, fieldB, fieldC });
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");

        // Data: 0x78, 0x56, 0x34, 0x12
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var a = GetPropertyValue<int>(result, "FieldA"); // Read at 0: 0x12345678
        var b = GetPropertyValue<short>(result, "FieldB"); // Read at 0: 0x5678
        var c = GetPropertyValue<byte>(result, "FieldC"); // Read at 2: 0x34

        // Assert
        Assert.AreEqual(0x12345678, a);
        Assert.AreEqual((short)0x5678, b);
        Assert.AreEqual((byte)0x34, c);
    }

    [TestMethod]
    public void Interpret_AtPositioning_WithConditionalField_ShouldCombineModifiers()
    {
        // Arrange: binary Record { HasData: byte, DataOffset: short le, Data: int le at DataOffset when HasData <> 0 }
        var registry = new SchemaRegistry();

        var hasDataField = CreatePrimitiveField("HasData", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var dataOffsetField = CreatePrimitiveField("DataOffset", PrimitiveTypeName.Short, Endianness.LittleEndian);

        // Data field with both 'at' and 'when' modifiers
        var dataType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var atOffset = new IdentifierNode("DataOffset");
        var hasDataRef = new IdentifierNode("HasData");
        var whenCondition = new DiffNode(hasDataRef, new IntegerNode(0));
        var dataField = new FieldDefinitionNode("Data", dataType, null, atOffset, whenCondition);

        var schema = new BinarySchemaNode("Record", new[] { hasDataField, dataOffsetField, dataField });
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");

        // Create test data:
        // Byte 0: HasData = 1 (true)
        // Bytes 1-2: DataOffset = 8
        // Bytes 3-7: padding
        // Bytes 8-11: Data = 0xDEADBEEF
        var data = new byte[12];
        data[0] = 0x01; // HasData = 1
        data[1] = 0x08; // DataOffset = 8
        data[2] = 0x00;
        data[8] = 0xEF;
        data[9] = 0xBE;
        data[10] = 0xAD;
        data[11] = 0xDE;

        // Act
        var result = InvokeInterpret(interpreter, data);
        var hasData = GetPropertyValue<byte>(result, "HasData");
        var dataOffset = GetPropertyValue<short>(result, "DataOffset");
        var dataValue = GetPropertyValue<int?>(result, "Data");

        // Assert
        Assert.AreEqual((byte)1, hasData);
        Assert.AreEqual((short)8, dataOffset);
        Assert.AreEqual(unchecked((int)0xDEADBEEF), dataValue);
    }

    [TestMethod]
    public void Interpret_AtPositioning_WithConditionalField_WhenFalse_ShouldNotJump()
    {
        var registry = new SchemaRegistry();

        var hasDataField = CreatePrimitiveField("HasData", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var dataOffsetField = CreatePrimitiveField("DataOffset", PrimitiveTypeName.Short, Endianness.LittleEndian);

        var dataType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var atOffset = new IdentifierNode("DataOffset");
        var hasDataRef = new IdentifierNode("HasData");
        var whenCondition = new DiffNode(hasDataRef, new IntegerNode(0));
        var dataField = new FieldDefinitionNode("Data", dataType, null, atOffset, whenCondition);

        var schema = new BinarySchemaNode("Record", new[] { hasDataField, dataOffsetField, dataField });
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");


        var data = new byte[3];
        data[0] = 0x00;
        data[1] = 0x08;
        data[2] = 0x00;

        // Act
        var result = InvokeInterpret(interpreter, data);
        var hasData = GetPropertyValue<byte>(result, "HasData");
        var dataValue = GetPropertyValue<int?>(result, "Data");

        // Assert
        Assert.AreEqual((byte)0, hasData);
        Assert.IsNull(dataValue); // Data is null because condition was false
    }

    #endregion

    #region Bit Field Tests (bits[N] and align[N])

    [TestMethod]
    public void Interpret_SingleBit_ShouldParseLeastSignificantBit()
    {
        // Arrange: binary Flags { Flag0: bits[1] }
        // LSB ordering: bit 0 is the least significant bit
        var registry = new SchemaRegistry();

        var flag0Field = new FieldDefinitionNode("Flag0", new BitsTypeNode(1));

        var schema = new BinarySchemaNode("Flags", new[] { flag0Field });
        registry.Register("Flags", schema);

        var interpreter = CompileInterpreter(registry, "Flags");

        // Data: 0b00000001 - bit 0 is set
        var data = new byte[] { 0x01 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var flag0 = GetPropertyValue<byte>(result, "Flag0");

        // Assert
        Assert.AreEqual((byte)1, flag0);
    }

    [TestMethod]
    public void Interpret_SingleBit_ShouldParseMiddleBit()
    {
        // Arrange: binary Flags { Skip: bits[3], Target: bits[1] }
        // Read 3 bits first (bits 0-2), then read bit 3
        var registry = new SchemaRegistry();

        var skipField = new FieldDefinitionNode("Skip", new BitsTypeNode(3));
        var targetField = new FieldDefinitionNode("Target", new BitsTypeNode(1));

        var schema = new BinarySchemaNode("Flags", new[] { skipField, targetField });
        registry.Register("Flags", schema);

        var interpreter = CompileInterpreter(registry, "Flags");

        // Data: 0b00001000 - bit 3 is set (value = 8)
        // Skip reads bits 0-2 = 0, Target reads bit 3 = 1
        var data = new byte[] { 0x08 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var skip = GetPropertyValue<byte>(result, "Skip");
        var target = GetPropertyValue<byte>(result, "Target");

        // Assert
        Assert.AreEqual((byte)0, skip);
        Assert.AreEqual((byte)1, target);
    }

    [TestMethod]
    public void Interpret_MultipleBitsInOneByte_ShouldParseCorrectly()
    {
        // Arrange: binary TcpFlags { Reserved: bits[4], DataOff: bits[4] }
        // Like TCP header first byte
        var registry = new SchemaRegistry();

        var reservedField = new FieldDefinitionNode("Reserved", new BitsTypeNode(4));
        var dataOffField = new FieldDefinitionNode("DataOff", new BitsTypeNode(4));

        var schema = new BinarySchemaNode("TcpHeader", new[] { reservedField, dataOffField });
        registry.Register("TcpHeader", schema);

        var interpreter = CompileInterpreter(registry, "TcpHeader");

        // Data: 0b01010011 = 0x53
        // Reserved (bits 0-3) = 0011 = 3
        // DataOff (bits 4-7) = 0101 = 5
        var data = new byte[] { 0x53 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var reserved = GetPropertyValue<byte>(result, "Reserved");
        var dataOff = GetPropertyValue<byte>(result, "DataOff");

        // Assert
        Assert.AreEqual((byte)3, reserved);
        Assert.AreEqual((byte)5, dataOff);
    }

    [TestMethod]
    public void Interpret_MultipleSingleBits_ShouldParseSequentially()
    {
        // Arrange: 8 individual bit flags in one byte
        var registry = new SchemaRegistry();

        var fields = new[]
        {
            new FieldDefinitionNode("Bit0", new BitsTypeNode(1)),
            new FieldDefinitionNode("Bit1", new BitsTypeNode(1)),
            new FieldDefinitionNode("Bit2", new BitsTypeNode(1)),
            new FieldDefinitionNode("Bit3", new BitsTypeNode(1)),
            new FieldDefinitionNode("Bit4", new BitsTypeNode(1)),
            new FieldDefinitionNode("Bit5", new BitsTypeNode(1)),
            new FieldDefinitionNode("Bit6", new BitsTypeNode(1)),
            new FieldDefinitionNode("Bit7", new BitsTypeNode(1))
        };

        var schema = new BinarySchemaNode("BitFlags", fields);
        registry.Register("BitFlags", schema);

        var interpreter = CompileInterpreter(registry, "BitFlags");

        // Data: 0b10101010 = 0xAA
        // Bits set: 1, 3, 5, 7
        var data = new byte[] { 0xAA };

        // Act
        var result = InvokeInterpret(interpreter, data);

        // Assert
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Bit0"));
        Assert.AreEqual((byte)1, GetPropertyValue<byte>(result, "Bit1"));
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Bit2"));
        Assert.AreEqual((byte)1, GetPropertyValue<byte>(result, "Bit3"));
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Bit4"));
        Assert.AreEqual((byte)1, GetPropertyValue<byte>(result, "Bit5"));
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Bit6"));
        Assert.AreEqual((byte)1, GetPropertyValue<byte>(result, "Bit7"));
    }

    [TestMethod]
    public void Interpret_CrossByteBitField_ShouldParseAcrossBoundary()
    {
        // Arrange: bits[12] field that spans two bytes
        var registry = new SchemaRegistry();

        var valueField = new FieldDefinitionNode("Value", new BitsTypeNode(12));

        var schema = new BinarySchemaNode("CrossByte", new[] { valueField });
        registry.Register("CrossByte", schema);

        var interpreter = CompileInterpreter(registry, "CrossByte");

        // Data: 0xAB 0xCD
        // bits[12] reads: 8 bits from byte 0 (0xAB) + 4 bits from byte 1 (0x0D)
        // Value = 0xAB | (0x0D << 8) = 0xDAB
        var data = new byte[] { 0xAB, 0xCD };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<ushort>(result, "Value");

        // Assert
        Assert.AreEqual((ushort)0xDAB, value);
    }

    [TestMethod]
    public void Interpret_LargeBitField_ShouldParse32Bits()
    {
        // Arrange: bits[32] field
        var registry = new SchemaRegistry();

        var valueField = new FieldDefinitionNode("Value", new BitsTypeNode(32));

        var schema = new BinarySchemaNode("LargeBits", new[] { valueField });
        registry.Register("LargeBits", schema);

        var interpreter = CompileInterpreter(registry, "LargeBits");

        // Data: 0xEFBEADDE (DEADBEEF in little-endian bit order)
        var data = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<uint>(result, "Value");

        // Assert - ReadBits reads LSB first, so it's little-endian style
        Assert.AreEqual(0xDEADBEEFu, value);
    }

    [TestMethod]
    public void Interpret_AlignToByteAfterBits_ShouldSkipRemainingBits()
    {
        // Arrange: binary Record { Flags: bits[3], _: align[8], NextByte: byte }
        var registry = new SchemaRegistry();

        var flagsField = new FieldDefinitionNode("Flags", new BitsTypeNode(3));
        var alignField = new FieldDefinitionNode("_align", new AlignmentNode(8));
        var nextByteField = new FieldDefinitionNode("NextByte",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable));

        var schema = new BinarySchemaNode("Record", new[] { flagsField, alignField, nextByteField });
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");

        // Data: byte 0 has flags in bits 0-2, byte 1 is NextByte
        // Flags = 0b00000101 (bits 0,2 set = 5), align skips bits 3-7
        // NextByte = 0x42
        var data = new byte[] { 0x05, 0x42 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var flags = GetPropertyValue<byte>(result, "Flags");
        var nextByte = GetPropertyValue<byte>(result, "NextByte");

        // Assert
        Assert.AreEqual((byte)5, flags);
        Assert.AreEqual((byte)0x42, nextByte);
    }

    [TestMethod]
    public void Interpret_BitsFollowedByPrimitive_ShouldAlignAutomatically()
    {
        var registry = new SchemaRegistry();

        var flagField = new FieldDefinitionNode("Flag", new BitsTypeNode(4));
        var alignField = new FieldDefinitionNode("_align", new AlignmentNode(8));
        var valueField = new FieldDefinitionNode("Value",
            new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian));

        var schema = new BinarySchemaNode("Mixed", new[] { flagField, alignField, valueField });
        registry.Register("Mixed", schema);

        var interpreter = CompileInterpreter(registry, "Mixed");


        var data = new byte[] { 0x0A, 0x34, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var flag = GetPropertyValue<byte>(result, "Flag");
        var value = GetPropertyValue<short>(result, "Value");

        // Assert
        Assert.AreEqual((byte)0x0A, flag);
        Assert.AreEqual((short)0x1234, value);
    }

    [TestMethod]
    public void Interpret_TcpFlagsLikeBitPattern_ShouldParseCorrectly()
    {
        // Arrange: Simplified TCP flags (2 bytes with bit fields)
        var registry = new SchemaRegistry();

        var fields = new[]
        {
            new FieldDefinitionNode("Reserved", new BitsTypeNode(4)),
            new FieldDefinitionNode("DataOff", new BitsTypeNode(4)),
            new FieldDefinitionNode("Fin", new BitsTypeNode(1)),
            new FieldDefinitionNode("Syn", new BitsTypeNode(1)),
            new FieldDefinitionNode("Rst", new BitsTypeNode(1)),
            new FieldDefinitionNode("Psh", new BitsTypeNode(1)),
            new FieldDefinitionNode("Ack", new BitsTypeNode(1)),
            new FieldDefinitionNode("Urg", new BitsTypeNode(1)),
            new FieldDefinitionNode("Ece", new BitsTypeNode(1)),
            new FieldDefinitionNode("Cwr", new BitsTypeNode(1))
        };

        var schema = new BinarySchemaNode("TcpFlags", fields);
        registry.Register("TcpFlags", schema);

        var interpreter = CompileInterpreter(registry, "TcpFlags");

        // Byte 0: Reserved=0, DataOff=5 → 0x50
        // Byte 1: Fin=0, Syn=1, Rst=0, Psh=0, Ack=1, Urg=0, Ece=0, Cwr=0 → 0b00010010 = 0x12
        var data = new byte[] { 0x50, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);

        // Assert
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Reserved"));
        Assert.AreEqual((byte)5, GetPropertyValue<byte>(result, "DataOff"));
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Fin"));
        Assert.AreEqual((byte)1, GetPropertyValue<byte>(result, "Syn"));
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Rst"));
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Psh"));
        Assert.AreEqual((byte)1, GetPropertyValue<byte>(result, "Ack"));
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Urg"));
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Ece"));
        Assert.AreEqual((byte)0, GetPropertyValue<byte>(result, "Cwr"));
    }

    #endregion

    #region Computed Field Tests

    [TestMethod]
    public void Interpret_ComputedField_SimpleAddition_ShouldCalculateCorrectly()
    {
        // Arrange: Schema with two parsed fields and a computed field that adds them
        // binary Math { A: int le, B: int le, Sum: A + B }
        var registry = new SchemaRegistry();

        var fieldA = CreatePrimitiveField("A", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var fieldB = CreatePrimitiveField("B", PrimitiveTypeName.Int, Endianness.LittleEndian);

        // Computed field: Sum = A + B
        var aRef = new IdentifierNode("A");
        var bRef = new IdentifierNode("B");
        var addExpr = new AddNode(aRef, bRef);
        var sumField = new ComputedFieldNode("Sum", addExpr);

        var schema = new BinarySchemaNode("Math", new SchemaFieldNode[] { fieldA, fieldB, sumField });
        registry.Register("Math", schema);

        var interpreter = CompileInterpreter(registry, "Math");

        // Data: A = 10, B = 20
        var data = new byte[8];
        BitConverter.GetBytes(10).CopyTo(data, 0);
        BitConverter.GetBytes(20).CopyTo(data, 4);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var a = GetPropertyValue<int>(result, "A");
        var b = GetPropertyValue<int>(result, "B");
        var sum = GetPropertyValue<object>(result, "Sum");

        // Assert
        Assert.AreEqual(10, a);
        Assert.AreEqual(20, b);
        Assert.AreEqual(30, sum);
    }

    [TestMethod]
    public void Interpret_ComputedField_EqualityComparison_ShouldReturnBool()
    {
        // Arrange: Schema with a value field and a computed bool field
        // binary Check { Value: int le, IsZero: Value = 0 }
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);

        // Computed field: IsZero = (Value == 0)
        var valueRef = new IdentifierNode("Value");
        var zeroNode = new IntegerNode(0);
        var eqExpr = new EqualityNode(valueRef, zeroNode);
        var isZeroField = new ComputedFieldNode("IsZero", eqExpr);

        var schema = new BinarySchemaNode("Check", new SchemaFieldNode[] { valueField, isZeroField });
        registry.Register("Check", schema);

        var interpreter = CompileInterpreter(registry, "Check");

        // Data: Value = 0
        var data = BitConverter.GetBytes(0);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result, "Value");
        var isZero = GetPropertyValue<bool>(result, "IsZero");

        // Assert
        Assert.AreEqual(0, value);
        Assert.IsTrue(isZero);
    }

    [TestMethod]
    public void Interpret_ComputedField_InequalityComparison_ShouldReturnBool()
    {
        // Arrange: Schema with a value field and a computed bool field
        // binary Check { Value: int le, HasValue: Value <> 0 }
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);

        // Computed field: HasValue = (Value != 0)
        var valueRef = new IdentifierNode("Value");
        var zeroNode = new IntegerNode(0);
        var diffExpr = new DiffNode(valueRef, zeroNode);
        var hasValueField = new ComputedFieldNode("HasValue", diffExpr);

        var schema = new BinarySchemaNode("Check", new SchemaFieldNode[] { valueField, hasValueField });
        registry.Register("Check", schema);

        var interpreter = CompileInterpreter(registry, "Check");

        // Data: Value = 42
        var data = BitConverter.GetBytes(42);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result, "Value");
        var hasValue = GetPropertyValue<bool>(result, "HasValue");

        // Assert
        Assert.AreEqual(42, value);
        Assert.IsTrue(hasValue);
    }

    [TestMethod]
    public void Interpret_ComputedField_Multiplication_ShouldCalculateCorrectly()
    {
        // Arrange: Schema with width, height, and computed area
        // binary Rectangle { Width: int le, Height: int le, Area: Width * Height }
        var registry = new SchemaRegistry();

        var widthField = CreatePrimitiveField("Width", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var heightField = CreatePrimitiveField("Height", PrimitiveTypeName.Int, Endianness.LittleEndian);

        // Computed field: Area = Width * Height
        var widthRef = new IdentifierNode("Width");
        var heightRef = new IdentifierNode("Height");
        var mulExpr = new StarNode(widthRef, heightRef);
        var areaField = new ComputedFieldNode("Area", mulExpr);

        var schema = new BinarySchemaNode("Rectangle", new SchemaFieldNode[] { widthField, heightField, areaField });
        registry.Register("Rectangle", schema);

        var interpreter = CompileInterpreter(registry, "Rectangle");

        // Data: Width = 5, Height = 7
        var data = new byte[8];
        BitConverter.GetBytes(5).CopyTo(data, 0);
        BitConverter.GetBytes(7).CopyTo(data, 4);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var width = GetPropertyValue<int>(result, "Width");
        var height = GetPropertyValue<int>(result, "Height");
        var area = GetPropertyValue<object>(result, "Area");

        // Assert
        Assert.AreEqual(5, width);
        Assert.AreEqual(7, height);
        Assert.AreEqual(35, area);
    }

    [TestMethod]
    public void Interpret_ComputedField_GreaterThanComparison_ShouldReturnBool()
    {
        // Arrange: Schema with size field and computed field checking if large
        // binary Data { Size: int le, IsLarge: Size > 100 }
        var registry = new SchemaRegistry();

        var sizeField = CreatePrimitiveField("Size", PrimitiveTypeName.Int, Endianness.LittleEndian);

        // Computed field: IsLarge = (Size > 100)
        var sizeRef = new IdentifierNode("Size");
        var thresholdNode = new IntegerNode(100);
        var gtExpr = new GreaterNode(sizeRef, thresholdNode);
        var isLargeField = new ComputedFieldNode("IsLarge", gtExpr);

        var schema = new BinarySchemaNode("Data", new SchemaFieldNode[] { sizeField, isLargeField });
        registry.Register("Data", schema);

        var interpreter = CompileInterpreter(registry, "Data");

        // Data: Size = 150
        var data = BitConverter.GetBytes(150);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var size = GetPropertyValue<int>(result, "Size");
        var isLarge = GetPropertyValue<bool>(result, "IsLarge");

        // Assert
        Assert.AreEqual(150, size);
        Assert.IsTrue(isLarge);
    }

    [TestMethod]
    public void Interpret_ComputedField_NoByteConsumption_ShouldNotAdvanceCursor()
    {
        // Arrange: Computed field should not advance the parse position
        // binary Packet { Magic: int le, Version: byte, Checksum: Magic + Version }
        var registry = new SchemaRegistry();

        var magicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var versionField = CreatePrimitiveField("Version", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        // Computed field references previously parsed fields only
        var magicRef = new IdentifierNode("Magic");
        var versionRef = new IdentifierNode("Version");
        var addExpr = new AddNode(magicRef, versionRef);
        var checksumField = new ComputedFieldNode("Checksum", addExpr);

        // Computed field comes AFTER the fields it references
        var schema = new BinarySchemaNode("Packet", new SchemaFieldNode[] { magicField, versionField, checksumField });
        registry.Register("Packet", schema);

        var interpreter = CompileInterpreter(registry, "Packet");

        // Data: Magic = 0x12345678, Version = 1
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12, 0x01 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var magic = GetPropertyValue<int>(result, "Magic");
        var version = GetPropertyValue<byte>(result, "Version");
        var checksum = GetPropertyValue<object>(result, "Checksum");

        // Assert
        Assert.AreEqual(0x12345678, magic);
        Assert.AreEqual((byte)1, version);
        Assert.AreEqual(0x12345678 + 1, checksum); // Magic + Version
    }

    #endregion

    #region Schema Inheritance Tests

    [TestMethod]
    public void Interpret_Inheritance_BasicExtends_ShouldIncludeParentFields()
    {
        // Arrange: Child schema extends Parent with additional field
        // binary Parent { Version: byte }
        // binary Child extends Parent { Flags: byte }
        var registry = new SchemaRegistry();

        var parentVersionField = CreatePrimitiveField("Version", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var parentSchema = new BinarySchemaNode("Parent", new SchemaFieldNode[] { parentVersionField });
        registry.Register("Parent", parentSchema);

        var childFlagsField = CreatePrimitiveField("Flags", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var childSchema = new BinarySchemaNode("Child", new SchemaFieldNode[] { childFlagsField }, "Parent");
        registry.Register("Child", childSchema);

        var interpreter = CompileInterpreter(registry, "Child");

        // Data: Version = 0x01, Flags = 0xFF
        var data = new byte[] { 0x01, 0xFF };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var version = GetPropertyValue<byte>(result, "Version");
        var flags = GetPropertyValue<byte>(result, "Flags");

        // Assert
        Assert.AreEqual((byte)0x01, version); // Parent field
        Assert.AreEqual((byte)0xFF, flags); // Child field
    }

    [TestMethod]
    public void Interpret_Inheritance_ChildComputedFieldReferencesParent_ShouldWork()
    {
        // Arrange: Child references parent field in computed expression
        // binary Parent { Value: int le }
        // binary Child extends Parent { DoubledValue: Value * 2 }
        var registry = new SchemaRegistry();

        var parentValueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var parentSchema = new BinarySchemaNode("Parent", new SchemaFieldNode[] { parentValueField });
        registry.Register("Parent", parentSchema);

        // Child has a computed field referencing parent's Value
        var valueRef = new IdentifierNode("Value");
        var twoNode = new IntegerNode(2);
        var mulExpr = new StarNode(valueRef, twoNode);
        var doubledField = new ComputedFieldNode("DoubledValue", mulExpr);

        var childSchema = new BinarySchemaNode("Child", new SchemaFieldNode[] { doubledField }, "Parent");
        registry.Register("Child", childSchema);

        var interpreter = CompileInterpreter(registry, "Child");

        // Data: Value = 25
        var data = BitConverter.GetBytes(25);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result, "Value");
        var doubled = GetPropertyValue<object>(result, "DoubledValue");

        // Assert
        Assert.AreEqual(25, value);
        Assert.AreEqual(50, doubled);
    }

    [TestMethod]
    public void Interpret_Inheritance_ParentFieldsParsedFirst_ShouldMaintainOrder()
    {
        // Arrange: Verify parent fields are parsed before child fields
        // binary Header { Magic: int le, Version: short le }
        // binary Packet extends Header { DataLength: int le, Data: byte[DataLength] }
        var registry = new SchemaRegistry();

        var parentMagicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var parentVersionField = CreatePrimitiveField("Version", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var parentSchema =
            new BinarySchemaNode("Header", new SchemaFieldNode[] { parentMagicField, parentVersionField });
        registry.Register("Header", parentSchema);

        var childLengthField = CreatePrimitiveField("DataLength", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var childDataField = new FieldDefinitionNode("Data", new ArrayTypeNode(
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable),
            new IdentifierNode("DataLength")));

        var childSchema = new BinarySchemaNode("Packet", new SchemaFieldNode[] { childLengthField, childDataField },
            "Header");
        registry.Register("Packet", childSchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        // Data: Magic = 0xDEADBEEF, Version = 1, DataLength = 3, Data = [0xAA, 0xBB, 0xCC]
        var data = new byte[]
        {
            0xEF, 0xBE, 0xAD, 0xDE, // Magic (little-endian)
            0x01, 0x00, // Version (little-endian)
            0x03, 0x00, 0x00, 0x00, // DataLength
            0xAA, 0xBB, 0xCC // Data
        };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var magic = GetPropertyValue<int>(result, "Magic");
        var version = GetPropertyValue<short>(result, "Version");
        var dataLength = GetPropertyValue<int>(result, "DataLength");
        var dataValue = GetPropertyValue<byte[]>(result, "Data");

        // Assert
        Assert.AreEqual(unchecked((int)0xDEADBEEF), magic);
        Assert.AreEqual((short)1, version);
        Assert.AreEqual(3, dataLength);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC }, dataValue);
    }

    [TestMethod]
    public void Interpret_Inheritance_MultiLevel_ShouldIncludeAllAncestorFields()
    {
        // Arrange: Multi-level inheritance: Grandchild extends Child extends Parent
        // binary Base { Magic: byte }
        // binary Child extends Base { Version: byte }
        // binary Grandchild extends Child { Flags: byte }
        var registry = new SchemaRegistry();

        var baseMagicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var baseSchema = new BinarySchemaNode("Base", new SchemaFieldNode[] { baseMagicField });
        registry.Register("Base", baseSchema);

        var childVersionField = CreatePrimitiveField("Version", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var childSchema = new BinarySchemaNode("Child", new SchemaFieldNode[] { childVersionField }, "Base");
        registry.Register("Child", childSchema);

        var grandchildFlagsField = CreatePrimitiveField("Flags", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var grandchildSchema =
            new BinarySchemaNode("Grandchild", new SchemaFieldNode[] { grandchildFlagsField }, "Child");
        registry.Register("Grandchild", grandchildSchema);

        var interpreter = CompileInterpreter(registry, "Grandchild");

        // Data: Magic = 0x01, Version = 0x02, Flags = 0x03
        var data = new byte[] { 0x01, 0x02, 0x03 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var magic = GetPropertyValue<byte>(result, "Magic");
        var version = GetPropertyValue<byte>(result, "Version");
        var flags = GetPropertyValue<byte>(result, "Flags");

        // Assert
        Assert.AreEqual((byte)0x01, magic); // From Base
        Assert.AreEqual((byte)0x02, version); // From Child
        Assert.AreEqual((byte)0x03, flags); // From Grandchild
    }

    #endregion

    #region Generic Schema Tests

    [TestMethod]
    public void Interpret_GenericSchema_SingleTypeParameter_ShouldWork()
    {
        // Arrange: binary LengthPrefixed<T> { Length: int le, Data: T }
        // Instantiate with a simple record: binary Record { Value: short le }
        var registry = new SchemaRegistry();

        // First, define the concrete type that will be used as T
        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var recordSchema = new BinarySchemaNode("Record", new SchemaFieldNode[] { valueField });
        registry.Register("Record", recordSchema);

        // Define the generic schema with type parameter T
        var lengthField = CreatePrimitiveField("Length", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("T"));
        var genericSchema = new BinarySchemaNode("LengthPrefixed", new SchemaFieldNode[] { lengthField, dataField },
            null, new[] { "T" });
        registry.Register("LengthPrefixed", genericSchema);

        // Create a wrapper that uses LengthPrefixed<Record>
        // binary Wrapper { Prefixed: LengthPrefixed<Record> }
        var prefixedField =
            new FieldDefinitionNode("Prefixed", new SchemaReferenceTypeNode("LengthPrefixed", new[] { "Record" }));
        var wrapperSchema = new BinarySchemaNode("Wrapper", new SchemaFieldNode[] { prefixedField });
        registry.Register("Wrapper", wrapperSchema);

        var interpreter = CompileInterpreter(registry, "Wrapper");

        // Data: Length = 2, Value = 0x1234
        var data = new byte[]
        {
            0x02, 0x00, 0x00, 0x00, // Length (little-endian)
            0x34, 0x12 // Value (little-endian short)
        };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var prefixed = GetPropertyValue<object>(result, "Prefixed");
        var length = GetPropertyValue<int>(prefixed, "Length");
        var dataObj = GetPropertyValue<object>(prefixed, "Data");
        var value = GetPropertyValue<short>(dataObj, "Value");

        // Assert
        Assert.AreEqual(2, length);
        Assert.AreEqual((short)0x1234, value);
    }

    [TestMethod]
    public void Interpret_GenericSchema_ArrayOfTypeParameter_ShouldWork()
    {
        // Arrange: binary Container<T> { Count: byte, Items: T[Count] }
        var registry = new SchemaRegistry();

        // Define the concrete type: binary Item { Id: byte }
        var idField = CreatePrimitiveField("Id", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemSchema = new BinarySchemaNode("Item", new SchemaFieldNode[] { idField });
        registry.Register("Item", itemSchema);

        // Define the generic schema with array of T
        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemsField = new FieldDefinitionNode("Items", new ArrayTypeNode(
            new SchemaReferenceTypeNode("T"),
            new IdentifierNode("Count")));
        var genericSchema = new BinarySchemaNode("Container", new SchemaFieldNode[] { countField, itemsField }, null,
            new[] { "T" });
        registry.Register("Container", genericSchema);

        // Create a wrapper: binary Holder { Box: Container<Item> }
        var boxField = new FieldDefinitionNode("Box", new SchemaReferenceTypeNode("Container", new[] { "Item" }));
        var holderSchema = new BinarySchemaNode("Holder", new SchemaFieldNode[] { boxField });
        registry.Register("Holder", holderSchema);

        var interpreter = CompileInterpreter(registry, "Holder");

        // Data: Count = 3, Items = [0x0A, 0x0B, 0x0C]
        var data = new byte[] { 0x03, 0x0A, 0x0B, 0x0C };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var box = GetPropertyValue<object>(result, "Box");
        var count = GetPropertyValue<byte>(box, "Count");
        var items = GetPropertyValue<object[]>(box, "Items");

        // Assert
        Assert.AreEqual((byte)3, count);
        Assert.HasCount(3, items);
        Assert.AreEqual((byte)0x0A, GetPropertyValue<byte>(items[0], "Id"));
        Assert.AreEqual((byte)0x0B, GetPropertyValue<byte>(items[1], "Id"));
        Assert.AreEqual((byte)0x0C, GetPropertyValue<byte>(items[2], "Id"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_MultipleTypeParameters_ShouldWork()
    {
        // Arrange: binary Pair<T, U> { First: T, Second: U }
        var registry = new SchemaRegistry();

        // Define concrete types
        var byteField = CreatePrimitiveField("Val", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var byteSchema = new BinarySchemaNode("ByteVal", new SchemaFieldNode[] { byteField });
        registry.Register("ByteVal", byteSchema);

        var shortField = CreatePrimitiveField("Val", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var shortSchema = new BinarySchemaNode("ShortVal", new SchemaFieldNode[] { shortField });
        registry.Register("ShortVal", shortSchema);

        // Define the generic schema with two type parameters
        var firstField = new FieldDefinitionNode("First", new SchemaReferenceTypeNode("T"));
        var secondField = new FieldDefinitionNode("Second", new SchemaReferenceTypeNode("U"));
        var genericSchema = new BinarySchemaNode("Pair", new SchemaFieldNode[] { firstField, secondField }, null,
            new[] { "T", "U" });
        registry.Register("Pair", genericSchema);

        // Create a usage: binary Container { Data: Pair<ByteVal, ShortVal> }
        var dataField =
            new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("Pair", new[] { "ByteVal", "ShortVal" }));
        var containerSchema = new BinarySchemaNode("Container", new SchemaFieldNode[] { dataField });
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        // Data: ByteVal = 0xAA, ShortVal = 0x1234
        var data = new byte[] { 0xAA, 0x34, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var pairData = GetPropertyValue<object>(result, "Data");
        var first = GetPropertyValue<object>(pairData, "First");
        var second = GetPropertyValue<object>(pairData, "Second");

        var firstVal = GetPropertyValue<byte>(first, "Val");
        var secondVal = GetPropertyValue<short>(second, "Val");

        // Assert
        Assert.AreEqual((byte)0xAA, firstVal);
        Assert.AreEqual((short)0x1234, secondVal);
    }

    [TestMethod]
    public void Interpret_GenericSchema_SameGenericWithDifferentTypes_ShouldWork()
    {
        // Arrange: Use the same generic schema with two different concrete types
        // binary Wrapper<T> { Data: T }
        // Use Wrapper<ByteRecord> and Wrapper<IntRecord> in same compilation
        var registry = new SchemaRegistry();

        // Define two concrete types
        var byteField = CreatePrimitiveField("Value", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var byteRecordSchema = new BinarySchemaNode("ByteRecord", new SchemaFieldNode[] { byteField });
        registry.Register("ByteRecord", byteRecordSchema);

        var intField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var intRecordSchema = new BinarySchemaNode("IntRecord", new SchemaFieldNode[] { intField });
        registry.Register("IntRecord", intRecordSchema);

        // Define generic wrapper
        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("T"));
        var wrapperSchema = new BinarySchemaNode("Wrapper", new SchemaFieldNode[] { dataField }, null, new[] { "T" });
        registry.Register("Wrapper", wrapperSchema);

        // Container using both instantiations
        // binary Container { ByteWrapped: Wrapper<ByteRecord>, IntWrapped: Wrapper<IntRecord> }
        var byteWrappedField =
            new FieldDefinitionNode("ByteWrapped", new SchemaReferenceTypeNode("Wrapper", new[] { "ByteRecord" }));
        var intWrappedField =
            new FieldDefinitionNode("IntWrapped", new SchemaReferenceTypeNode("Wrapper", new[] { "IntRecord" }));
        var containerSchema =
            new BinarySchemaNode("Container", new SchemaFieldNode[] { byteWrappedField, intWrappedField });
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        // Data: ByteRecord.Value = 0xAB, IntRecord.Value = 0x12345678
        var data = new byte[] { 0xAB, 0x78, 0x56, 0x34, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var byteWrapped = GetPropertyValue<object>(result, "ByteWrapped");
        var intWrapped = GetPropertyValue<object>(result, "IntWrapped");

        var byteData = GetPropertyValue<object>(byteWrapped, "Data");
        var intData = GetPropertyValue<object>(intWrapped, "Data");

        // Assert
        Assert.AreEqual((byte)0xAB, GetPropertyValue<byte>(byteData, "Value"));
        Assert.AreEqual(0x12345678, GetPropertyValue<int>(intData, "Value"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_NestedGenericInstantiation_ShouldWork()
    {
        // Arrange: Generic schema containing another generic instantiation
        // binary Optional<T> { HasValue: byte, Value: T when HasValue <> 0 }
        // binary Pair<T, U> { First: T, Second: U }
        // Use: Pair<Optional<ByteVal>, Optional<ShortVal>>
        var registry = new SchemaRegistry();

        // Define concrete types
        var byteField = CreatePrimitiveField("Val", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var byteValSchema = new BinarySchemaNode("ByteVal", new SchemaFieldNode[] { byteField });
        registry.Register("ByteVal", byteValSchema);

        var shortField = CreatePrimitiveField("Val", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var shortValSchema = new BinarySchemaNode("ShortVal", new SchemaFieldNode[] { shortField });
        registry.Register("ShortVal", shortValSchema);

        // Define Optional<T> with conditional field
        var hasValueField = CreatePrimitiveField("HasValue", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var hasValueRef = new IdentifierNode("HasValue");
        var zeroNode = new IntegerNode(0);
        var condition = new DiffNode(hasValueRef, zeroNode);
        var valueField = new FieldDefinitionNode("Value", new SchemaReferenceTypeNode("T"), null, null, condition);
        var optionalSchema = new BinarySchemaNode("Optional", new SchemaFieldNode[] { hasValueField, valueField }, null,
            new[] { "T" });
        registry.Register("Optional", optionalSchema);

        // Define Pair<T, U>
        var firstField = new FieldDefinitionNode("First", new SchemaReferenceTypeNode("T"));
        var secondField = new FieldDefinitionNode("Second", new SchemaReferenceTypeNode("U"));
        var pairSchema = new BinarySchemaNode("Pair", new SchemaFieldNode[] { firstField, secondField }, null,
            new[] { "T", "U" });
        registry.Register("Pair", pairSchema);

        // Container: Pair<Optional<ByteVal>, Optional<ShortVal>>
        var pairField = new FieldDefinitionNode("Data",
            new SchemaReferenceTypeNode("Pair", new[] { "Optional<ByteVal>", "Optional<ShortVal>" }));
        var containerSchema = new BinarySchemaNode("Container", new SchemaFieldNode[] { pairField });
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        // Data: Optional<ByteVal>{HasValue=1, Val=0xAA}, Optional<ShortVal>{HasValue=1, Val=0x1234}
        var data = new byte[]
        {
            0x01, 0xAA, // First: HasValue=1, ByteVal=0xAA
            0x01, 0x34, 0x12 // Second: HasValue=1, ShortVal=0x1234
        };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var pair = GetPropertyValue<object>(result, "Data");
        var first = GetPropertyValue<object>(pair, "First");
        var second = GetPropertyValue<object>(pair, "Second");

        Assert.AreEqual((byte)1, GetPropertyValue<byte>(first, "HasValue"));
        var firstValue = GetPropertyValue<object>(first, "Value");
        Assert.AreEqual((byte)0xAA, GetPropertyValue<byte>(firstValue, "Val"));

        Assert.AreEqual((byte)1, GetPropertyValue<byte>(second, "HasValue"));
        var secondValue = GetPropertyValue<object>(second, "Value");
        Assert.AreEqual((short)0x1234, GetPropertyValue<short>(secondValue, "Val"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_WithComputedField_ShouldWork()
    {
        // Arrange: Generic schema with computed field that doesn't use T
        // binary Tagged<T> { Tag: byte, Data: T, IsValid: Tag = 0xAA }
        var registry = new SchemaRegistry();

        // Define concrete type
        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var recordSchema = new BinarySchemaNode("Record", new SchemaFieldNode[] { valueField });
        registry.Register("Record", recordSchema);

        // Define Tagged<T> with computed field
        var tagField = CreatePrimitiveField("Tag", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("T"));

        // Computed field: IsValid: Tag = 0xAA
        var tagRef = new IdentifierNode("Tag");
        var expectedTag = new IntegerNode(0xAA);
        var isValidExpr = new EqualityNode(tagRef, expectedTag);
        var isValidField = new ComputedFieldNode("IsValid", isValidExpr);

        var taggedSchema = new BinarySchemaNode("Tagged", new SchemaFieldNode[] { tagField, dataField, isValidField },
            null, new[] { "T" });
        registry.Register("Tagged", taggedSchema);

        // Container using Tagged<Record>
        var containerField = new FieldDefinitionNode("Item", new SchemaReferenceTypeNode("Tagged", new[] { "Record" }));
        var containerSchema = new BinarySchemaNode("Container", new SchemaFieldNode[] { containerField });
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        // Data: Tag = 0xAA (valid), Record.Value = 0x1234
        var data = new byte[] { 0xAA, 0x34, 0x12 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var item = GetPropertyValue<object>(result, "Item");

        // Assert
        Assert.AreEqual((byte)0xAA, GetPropertyValue<byte>(item, "Tag"));
        var recordData = GetPropertyValue<object>(item, "Data");
        Assert.AreEqual((short)0x1234, GetPropertyValue<short>(recordData, "Value"));
        Assert.IsTrue(GetPropertyValue<bool>(item, "IsValid"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_WithFixedSizeArray_ShouldWork()
    {
        // Arrange: Generic with fixed-size array of T
        // binary FixedBuffer<T> { Items: T[3] }
        var registry = new SchemaRegistry();

        // Define concrete type
        var valueField = CreatePrimitiveField("Val", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemSchema = new BinarySchemaNode("Item", new SchemaFieldNode[] { valueField });
        registry.Register("Item", itemSchema);

        // Define FixedBuffer<T> with fixed array size
        var itemsField = new FieldDefinitionNode("Items", new ArrayTypeNode(
            new SchemaReferenceTypeNode("T"),
            new IntegerNode(3)));
        var bufferSchema =
            new BinarySchemaNode("FixedBuffer", new SchemaFieldNode[] { itemsField }, null, new[] { "T" });
        registry.Register("FixedBuffer", bufferSchema);

        // Container
        var bufferField =
            new FieldDefinitionNode("Buffer", new SchemaReferenceTypeNode("FixedBuffer", new[] { "Item" }));
        var containerSchema = new BinarySchemaNode("Container", new SchemaFieldNode[] { bufferField });
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        // Data: 3 items with values 0x0A, 0x0B, 0x0C
        var data = new byte[] { 0x0A, 0x0B, 0x0C };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var buffer = GetPropertyValue<object>(result, "Buffer");
        var items = GetPropertyValue<object[]>(buffer, "Items");

        // Assert
        Assert.HasCount(3, items);
        Assert.AreEqual((byte)0x0A, GetPropertyValue<byte>(items[0], "Val"));
        Assert.AreEqual((byte)0x0B, GetPropertyValue<byte>(items[1], "Val"));
        Assert.AreEqual((byte)0x0C, GetPropertyValue<byte>(items[2], "Val"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_DirectInstantiation_ShouldWork()
    {
        // Arrange: Compile and use the generic instantiation directly (without wrapper)
        // binary Wrapper<T> { Data: T }
        // Directly compile Wrapper<Record>
        var registry = new SchemaRegistry();

        // Define concrete type
        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var recordSchema = new BinarySchemaNode("Record", new SchemaFieldNode[] { valueField });
        registry.Register("Record", recordSchema);

        // Define generic wrapper
        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("T"));
        var wrapperSchema = new BinarySchemaNode("Wrapper", new SchemaFieldNode[] { dataField }, null, new[] { "T" });
        registry.Register("Wrapper", wrapperSchema);

        // Compile the generic instantiation directly
        var interpreter = CompileInterpreterForGenericInstantiation(registry, "Wrapper", new[] { "Record" });

        // Data: Record.Value = 0xDEADBEEF
        var data = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var recordData = GetPropertyValue<object>(result, "Data");

        // Assert
        Assert.AreEqual(unchecked((int)0xDEADBEEF), GetPropertyValue<int>(recordData, "Value"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_EmptyArray_ShouldWork()
    {
        // Arrange: Generic with array that has zero elements
        // binary Container<T> { Count: byte, Items: T[Count] }
        var registry = new SchemaRegistry();

        // Define concrete type
        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var itemSchema = new BinarySchemaNode("Item", new SchemaFieldNode[] { valueField });
        registry.Register("Item", itemSchema);

        // Define generic container
        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemsField = new FieldDefinitionNode("Items", new ArrayTypeNode(
            new SchemaReferenceTypeNode("T"),
            new IdentifierNode("Count")));
        var containerSchema = new BinarySchemaNode("Container", new SchemaFieldNode[] { countField, itemsField }, null,
            new[] { "T" });
        registry.Register("Container", containerSchema);

        // Wrapper
        var wrapperField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("Container", new[] { "Item" }));
        var wrapperSchema = new BinarySchemaNode("Wrapper", new SchemaFieldNode[] { wrapperField });
        registry.Register("Wrapper", wrapperSchema);

        var interpreter = CompileInterpreter(registry, "Wrapper");

        // Data: Count = 0, no items
        var data = new byte[] { 0x00 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var container = GetPropertyValue<object>(result, "Data");
        var count = GetPropertyValue<byte>(container, "Count");
        var items = GetPropertyValue<object[]>(container, "Items");

        // Assert
        Assert.AreEqual((byte)0, count);
        Assert.IsEmpty(items);
    }

    [TestMethod]
    public void Interpret_GenericSchema_LargeArray_ShouldWork()
    {
        // Arrange: Generic with larger array (stress test)
        // binary Buffer<T> { Count: short le, Items: T[Count] }
        var registry = new SchemaRegistry();

        // Define simple item: binary Item { Id: byte }
        var idField = CreatePrimitiveField("Id", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemSchema = new BinarySchemaNode("Item", new SchemaFieldNode[] { idField });
        registry.Register("Item", itemSchema);

        // Define generic buffer
        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var itemsField = new FieldDefinitionNode("Items", new ArrayTypeNode(
            new SchemaReferenceTypeNode("T"),
            new IdentifierNode("Count")));
        var bufferSchema = new BinarySchemaNode("Buffer", new SchemaFieldNode[] { countField, itemsField }, null,
            new[] { "T" });
        registry.Register("Buffer", bufferSchema);

        // Wrapper
        var wrapperField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("Buffer", new[] { "Item" }));
        var wrapperSchema = new BinarySchemaNode("Wrapper", new SchemaFieldNode[] { wrapperField });
        registry.Register("Wrapper", wrapperSchema);

        var interpreter = CompileInterpreter(registry, "Wrapper");

        // Data: Count = 100, Items = [0, 1, 2, ..., 99]
        const int itemCount = 100;
        var data = new byte[2 + itemCount];
        data[0] = itemCount & 0xFF;
        data[1] = (itemCount >> 8) & 0xFF;
        for (var i = 0; i < itemCount; i++) data[2 + i] = (byte)i;

        // Act
        var result = InvokeInterpret(interpreter, data);
        var buffer = GetPropertyValue<object>(result, "Data");
        var count = GetPropertyValue<short>(buffer, "Count");
        var items = GetPropertyValue<object[]>(buffer, "Items");

        // Assert
        Assert.AreEqual((short)itemCount, count);
        Assert.HasCount(itemCount, items);
        for (var i = 0; i < itemCount; i++)
            Assert.AreEqual((byte)i, GetPropertyValue<byte>(items[i], "Id"), $"Item {i} has wrong Id");
    }

    [TestMethod]
    public void Interpret_GenericSchema_TypeParameterInMultipleFields_ShouldWork()
    {
        // Arrange: Type parameter used in multiple fields
        // binary Bracketed<T> { Header: T, Footer: T }
        var registry = new SchemaRegistry();

        // Define concrete type
        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var markerSchema = new BinarySchemaNode("Marker", new SchemaFieldNode[] { valueField });
        registry.Register("Marker", markerSchema);

        // Define Bracketed<T> using T twice
        var headerField = new FieldDefinitionNode("Header", new SchemaReferenceTypeNode("T"));
        var footerField = new FieldDefinitionNode("Footer", new SchemaReferenceTypeNode("T"));
        var bracketedSchema = new BinarySchemaNode("Bracketed", new SchemaFieldNode[] { headerField, footerField },
            null, new[] { "T" });
        registry.Register("Bracketed", bracketedSchema);

        // Container
        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("Bracketed", new[] { "Marker" }));
        var containerSchema = new BinarySchemaNode("Container", new SchemaFieldNode[] { dataField });
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        // Data: Header.Value = 0x1111, Footer.Value = 0x2222
        var data = new byte[] { 0x11, 0x11, 0x22, 0x22 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var bracketed = GetPropertyValue<object>(result, "Data");
        var header = GetPropertyValue<object>(bracketed, "Header");
        var footer = GetPropertyValue<object>(bracketed, "Footer");

        // Assert
        Assert.AreEqual((short)0x1111, GetPropertyValue<short>(header, "Value"));
        Assert.AreEqual((short)0x2222, GetPropertyValue<short>(footer, "Value"));
    }

    #endregion

    #region Repeat Until Tests

    [TestMethod]
    public void Interpret_RepeatUntilPrimitive_ShouldParseUntilCondition()
    {
        // Arrange: Parse bytes until we hit 0
        // binary ByteSequence { Bytes: byte repeat until Bytes = 0 }
        var registry = new SchemaRegistry();

        var primitiveType = new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var condition = new EqualityNode(
            new AccessColumnNode("Bytes", string.Empty, TextSpan.Empty),
            new IntegerNode("0"));
        var repeatUntilType = new RepeatUntilTypeNode(primitiveType, condition, "Bytes");
        var bytesField = new FieldDefinitionNode("Bytes", repeatUntilType);
        var schema = new BinarySchemaNode("ByteSequence", new SchemaFieldNode[] { bytesField });
        registry.Register("ByteSequence", schema);

        var interpreter = CompileInterpreter(registry, "ByteSequence");

        // Data: 1, 2, 3, 0 (stops at 0)
        var data = new byte[] { 0x01, 0x02, 0x03, 0x00 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var bytes = GetPropertyValue<byte[]>(result, "Bytes");

        // Assert - should have read 4 bytes (including the terminating 0)
        Assert.HasCount(4, bytes);
        Assert.AreEqual((byte)0x01, bytes[0]);
        Assert.AreEqual((byte)0x02, bytes[1]);
        Assert.AreEqual((byte)0x03, bytes[2]);
        Assert.AreEqual((byte)0x00, bytes[3]); // The terminating byte is included
    }

    [TestMethod]
    public void Interpret_RepeatUntilSchemaRef_ShouldParseUntilCondition()
    {
        // Arrange: Parse Records until Record.Type = 0
        // binary Record { Type: byte, Value: int le }
        // binary RecordList { Records: Record repeat until Records.Type = 0 }
        var registry = new SchemaRegistry();

        // Define Record schema
        var typeField = CreatePrimitiveField("Type", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var recordSchema = new BinarySchemaNode("Record", new SchemaFieldNode[] { typeField, valueField });
        registry.Register("Record", recordSchema);

        // Define RecordList with repeat until
        var schemaRefType = new SchemaReferenceTypeNode("Record");
        var condition = new EqualityNode(
            new DotNode(
                new AccessColumnNode("Records", string.Empty, TextSpan.Empty),
                new IdentifierNode("Type"),
                "Records.Type"),
            new IntegerNode("0"));
        var repeatUntilType = new RepeatUntilTypeNode(schemaRefType, condition, "Records");
        var recordsField = new FieldDefinitionNode("Records", repeatUntilType);
        var recordListSchema = new BinarySchemaNode("RecordList", new SchemaFieldNode[] { recordsField });
        registry.Register("RecordList", recordListSchema);

        var interpreter = CompileInterpreter(registry, "RecordList");

        // Data: 
        // Record 1: Type=1, Value=100
        // Record 2: Type=2, Value=200
        // Record 3: Type=0, Value=300 (terminator - Type=0)
        var data = new byte[]
        {
            0x01, 0x64, 0x00, 0x00, 0x00, // Type=1, Value=100 (little-endian)
            0x02, 0xC8, 0x00, 0x00, 0x00, // Type=2, Value=200 (little-endian)
            0x00, 0x2C, 0x01, 0x00, 0x00 // Type=0, Value=300 (terminator)
        };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var records = GetPropertyValue<object[]>(result, "Records");

        // Assert - should have 3 records (including the terminating record)
        Assert.HasCount(3, records);

        Assert.AreEqual((byte)0x01, GetPropertyValue<byte>(records[0], "Type"));
        Assert.AreEqual(100, GetPropertyValue<int>(records[0], "Value"));

        Assert.AreEqual((byte)0x02, GetPropertyValue<byte>(records[1], "Type"));
        Assert.AreEqual(200, GetPropertyValue<int>(records[1], "Value"));

        Assert.AreEqual((byte)0x00, GetPropertyValue<byte>(records[2], "Type")); // Terminator
        Assert.AreEqual(300, GetPropertyValue<int>(records[2], "Value"));
    }

    [TestMethod]
    public void Interpret_RepeatUntilWithCount_ShouldStopAtCount()
    {
        // Arrange: Parse items until we've read 5 items
        // binary Item { Id: byte }
        // binary ItemList { Count: byte, Items: Item repeat until Count = 5 }
        // Note: This tests using a separate field in the condition rather than the repeat field itself
        var registry = new SchemaRegistry();

        // Define Item schema
        var idField = CreatePrimitiveField("Id", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemSchema = new BinarySchemaNode("Item", new SchemaFieldNode[] { idField });
        registry.Register("Item", itemSchema);

        // Define ItemList with repeat until Count condition
        // This is a simpler condition - just "Count = 3" where Count is a field before Items
        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var schemaRefType = new SchemaReferenceTypeNode("Item");
        // Condition: Items.Id = 0xFF (terminator value)
        var condition = new EqualityNode(
            new DotNode(
                new AccessColumnNode("Items", string.Empty, TextSpan.Empty),
                new IdentifierNode("Id"),
                "Items.Id"),
            new IntegerNode("255")); // 0xFF
        var repeatUntilType = new RepeatUntilTypeNode(schemaRefType, condition, "Items");
        var itemsField = new FieldDefinitionNode("Items", repeatUntilType);
        var itemListSchema = new BinarySchemaNode("ItemList", new SchemaFieldNode[] { countField, itemsField });
        registry.Register("ItemList", itemListSchema);

        var interpreter = CompileInterpreter(registry, "ItemList");

        // Data: Count=3, then Items: 0x01, 0x02, 0xFF (terminator)
        var data = new byte[]
        {
            0x03, // Count = 3
            0x01, // Item[0].Id = 1
            0x02, // Item[1].Id = 2
            0xFF // Item[2].Id = 255 (terminator)
        };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var count = GetPropertyValue<byte>(result, "Count");
        var items = GetPropertyValue<object[]>(result, "Items");

        // Assert
        Assert.AreEqual((byte)3, count);
        Assert.HasCount(3, items);
        Assert.AreEqual((byte)0x01, GetPropertyValue<byte>(items[0], "Id"));
        Assert.AreEqual((byte)0x02, GetPropertyValue<byte>(items[1], "Id"));
        Assert.AreEqual((byte)0xFF, GetPropertyValue<byte>(items[2], "Id"));
    }

    #endregion

    #region TryInterpret Base Class Method Tests

    [TestMethod]
    public void TryInterpret_ValidData_ShouldReturnTrueAndResult()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));

        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };

        // Act
        var (success, result) = InvokeTryInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result!, "Value");

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(result);
        Assert.AreEqual(0x12345678, value);
    }

    [TestMethod]
    public void TryInterpret_InsufficientData_ShouldReturnFalseAndNull()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));


        var data = new byte[] { 0x78, 0x56 };


        var (success, result) = InvokeTryInterpret(interpreter, data);


        Assert.IsFalse(success);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryInterpret_EmptyData_ShouldReturnFalseAndNull()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Byte, Endianness.NotApplicable));


        var data = Array.Empty<byte>();


        var (success, result) = InvokeTryInterpret(interpreter, data);


        Assert.IsFalse(success);
        Assert.IsNull(result);
    }

    private static (bool success, object? result) InvokeTryInterpret(object interpreter, byte[] data)
    {
        try
        {
            var result = InvokeInterpret(interpreter, data);
            return (true, result);
        }
        catch
        {
            return (false, null);
        }
    }

    #endregion

    #region PartialInterpret Tests

    [TestMethod]
    public void PartialInterpret_ValidData_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));

        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };

        // Act
        var result = InvokePartialInterpret(interpreter, data);

        // Assert
        Assert.IsTrue(GetPropertyValue<bool>(result, "IsSuccess"));
        Assert.IsNotNull(GetPropertyValue<object>(result, "Result"));
        Assert.AreEqual(4, GetPropertyValue<int>(result, "BytesConsumed"));
        Assert.IsNull(GetPropertyValue<string>(result, "ErrorField"));
        Assert.IsNull(GetPropertyValue<string>(result, "ErrorMessage"));

        var parsedFields = GetPropertyValue<Dictionary<string, object?>>(result, "ParsedFields");
        Assert.IsTrue(parsedFields.ContainsKey("Value"));
        Assert.AreEqual(0x12345678, parsedFields["Value"]);
    }

    [TestMethod]
    public void PartialInterpret_InsufficientData_ShouldReturnFailureWithErrorInfo()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));

        // Only 2 bytes when 4 are needed for int
        var data = new byte[] { 0x78, 0x56 };

        // Act
        var result = InvokePartialInterpret(interpreter, data);

        // Assert
        Assert.IsFalse(GetPropertyValue<bool>(result, "IsSuccess"));
        Assert.IsNotNull(GetPropertyValue<string>(result, "ErrorMessage"));
    }

    [TestMethod]
    public void PartialInterpret_EmptyData_ShouldReturnFailure()
    {
        // Arrange
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Byte, Endianness.NotApplicable));

        // Empty data when at least 1 byte is needed
        var data = Array.Empty<byte>();

        // Act
        var result = InvokePartialInterpret(interpreter, data);

        // Assert
        Assert.IsFalse(GetPropertyValue<bool>(result, "IsSuccess"));
        Assert.IsNotNull(GetPropertyValue<string>(result, "ErrorMessage"));
    }

    private static object InvokePartialInterpret(object interpreter, byte[] data)
    {
        var method = interpreter.GetType().GetMethod("PartialInterpret", new[] { typeof(byte[]) });
        if (method == null)
            throw new InvalidOperationException("PartialInterpret method not found on interpreter");

        return method.Invoke(interpreter, new object[] { data })!;
    }

    #endregion

    #region Binary-Text Composition (as clause) Tests

    [TestMethod]
    public void Interpret_StringWithAsClause_ShouldParseWithTextSchema()
    {
        // Arrange: Binary schema with string field parsed as text schema
        // text KeyValue { Key: until ':' trim, Value: rest trim }
        // binary Config { Data: string[20] utf8 as KeyValue }
        var registry = new SchemaRegistry();

        // Define the text schema
        var textSchema = new TextSchemaNode("KeyValue", new[]
        {
            new TextFieldDefinitionNode("Key", TextFieldType.Until, ":", null, TextFieldModifier.Trim),
            new TextFieldDefinitionNode("Value", TextFieldType.Rest, null, null, TextFieldModifier.Trim)
        });
        registry.Register("KeyValue", textSchema);

        // Define the binary schema with 'as' clause
        var sizeExpr = new IntegerNode("20");
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.None, "KeyValue");
        var dataField = new FieldDefinitionNode("Data", stringType);
        var binarySchema = new BinarySchemaNode("Config", new SchemaFieldNode[] { dataField });
        registry.Register("Config", binarySchema);

        var interpreter = CompileInterpreter(registry, "Config");

        // Create test data: "Name: John Doe      " (20 bytes padded with spaces)
        var text = "Name: John Doe      ";
        var data = Encoding.UTF8.GetBytes(text);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var parsedData = GetPropertyValue<object>(result, "Data");

        // Assert
        Assert.IsNotNull(parsedData);
        Assert.AreEqual("Name", GetPropertyValue<string>(parsedData, "Key"));
        Assert.AreEqual("John Doe", GetPropertyValue<string>(parsedData, "Value"));
    }

    [TestMethod]
    public void Interpret_StringWithAsClauseAndModifiers_ShouldTrimThenParse()
    {
        // Arrange: Binary schema with string field with modifiers then parsed as text schema
        // text SimpleText { Content: rest }
        // binary Config { Data: string[30] utf8 trim as SimpleText }
        var registry = new SchemaRegistry();

        // Define the text schema (simple: just captures the rest)
        var textSchema = new TextSchemaNode("SimpleText", new[]
        {
            new TextFieldDefinitionNode("Content", TextFieldType.Rest)
        });
        registry.Register("SimpleText", textSchema);

        // Define the binary schema with 'as' clause and trim modifier
        var sizeExpr = new IntegerNode("30");
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.Trim, "SimpleText");
        var dataField = new FieldDefinitionNode("Data", stringType);
        var binarySchema = new BinarySchemaNode("Config", new SchemaFieldNode[] { dataField });
        registry.Register("Config", binarySchema);

        var interpreter = CompileInterpreter(registry, "Config");

        // Create test data: "  Hello World  " padded to 30 bytes with spaces
        var text = "  Hello World  ".PadRight(30);
        var data = Encoding.UTF8.GetBytes(text);

        // Act
        var result = InvokeInterpret(interpreter, data);
        var parsedData = GetPropertyValue<object>(result, "Data");

        // Assert - trim should have been applied before parsing
        Assert.IsNotNull(parsedData);
        Assert.AreEqual("Hello World", GetPropertyValue<string>(parsedData, "Content"));
    }

    [TestMethod]
    public void Interpret_StringWithAsClause_ComplexBinarySchema_ShouldParseMixedFields()
    {
        // Arrange: Binary schema with header, string parsed as text, and footer
        // text KeyValue { Key: until '=' trim, Value: rest trim }
        // binary Packet { Version: byte, Config: string[15] ascii as KeyValue, Checksum: byte }
        var registry = new SchemaRegistry();

        // Define the text schema
        var textSchema = new TextSchemaNode("KeyValue", new[]
        {
            new TextFieldDefinitionNode("Key", TextFieldType.Until, "=", null, TextFieldModifier.Trim),
            new TextFieldDefinitionNode("Value", TextFieldType.Rest, null, null, TextFieldModifier.Trim)
        });
        registry.Register("KeyValue", textSchema);

        // Define the binary schema
        var versionField = CreatePrimitiveField("Version", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var sizeExpr = new IntegerNode("15");
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Ascii, StringModifier.Trim, "KeyValue");
        var configField = new FieldDefinitionNode("Config", stringType);
        var checksumField = CreatePrimitiveField("Checksum", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var binarySchema =
            new BinarySchemaNode("Packet", new SchemaFieldNode[] { versionField, configField, checksumField });
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        // Create test data: Version=0x01, "Port=8080      " (15 bytes), Checksum=0xFF
        var configText = "Port=8080      "; // 15 bytes
        var data = new byte[17];
        data[0] = 0x01; // Version
        Encoding.ASCII.GetBytes(configText).CopyTo(data, 1);
        data[16] = 0xFF; // Checksum

        // Act
        var result = InvokeInterpret(interpreter, data);

        // Assert
        Assert.AreEqual((byte)0x01, GetPropertyValue<byte>(result, "Version"));
        var config = GetPropertyValue<object>(result, "Config");
        Assert.IsNotNull(config);
        Assert.AreEqual("Port", GetPropertyValue<string>(config, "Key"));
        Assert.AreEqual("8080", GetPropertyValue<string>(config, "Value"));
        Assert.AreEqual((byte)0xFF, GetPropertyValue<byte>(result, "Checksum"));
    }

    [TestMethod]
    public void Interpret_StringWithAsClause_WithNullTerm_ShouldStopAtNullThenParse()
    {
        // Arrange: Binary schema with null-terminated string parsed as text schema
        // text KeyValue { Key: until ':', Value: rest }
        // binary Config { Data: string[20] utf8 nullterm as KeyValue }
        var registry = new SchemaRegistry();

        // Define the text schema
        var textSchema = new TextSchemaNode("KeyValue", new[]
        {
            new TextFieldDefinitionNode("Key", TextFieldType.Until, ":"),
            new TextFieldDefinitionNode("Value", TextFieldType.Rest)
        });
        registry.Register("KeyValue", textSchema);

        // Define the binary schema with nullterm and 'as' clause
        var sizeExpr = new IntegerNode("20");
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.NullTerm, "KeyValue");
        var dataField = new FieldDefinitionNode("Data", stringType);
        var binarySchema = new BinarySchemaNode("Config", new SchemaFieldNode[] { dataField });
        registry.Register("Config", binarySchema);

        var interpreter = CompileInterpreter(registry, "Config");

        // Create test data: "Host:localhost\0xxxxx" (null-terminated, rest is garbage)
        var data = new byte[20];
        Encoding.UTF8.GetBytes("Host:localhost").CopyTo(data, 0);
        data[14] = 0x00; // Null terminator
        for (var i = 15; i < 20; i++) data[i] = 0xFF; // Garbage after null

        // Act
        var result = InvokeInterpret(interpreter, data);
        var parsedData = GetPropertyValue<object>(result, "Data");

        // Assert - should parse only up to null terminator
        Assert.IsNotNull(parsedData);
        Assert.AreEqual("Host", GetPropertyValue<string>(parsedData, "Key"));
        Assert.AreEqual("localhost", GetPropertyValue<string>(parsedData, "Value"));
    }

    #endregion

    #region Inline Schema Tests

    [TestMethod]
    public void Interpret_InlineSchema_SimpleFields_ShouldParseNestedStructure()
    {
        // Arrange: Binary schema with inline nested schema
        // binary Packet { Header: { Magic: int le, Version: short le } }
        var registry = new SchemaRegistry();

        // Create inline schema with Magic (int le) and Version (short le)
        var magicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var versionField = CreatePrimitiveField("Version", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var inlineSchema = new InlineSchemaTypeNode(new SchemaFieldNode[] { magicField, versionField });

        var headerField = new FieldDefinitionNode("Header", inlineSchema);
        var binarySchema = new BinarySchemaNode("Packet", new SchemaFieldNode[] { headerField });
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        // Create test data: Magic=0x12345678 (little-endian), Version=0x0100 (little-endian)
        var data = new byte[] { 0x78, 0x56, 0x34, 0x12, 0x00, 0x01 };

        // Act
        var result = InvokeInterpret(interpreter, data);
        var header = GetPropertyValue<object>(result, "Header");

        // Assert
        Assert.IsNotNull(header);
        Assert.AreEqual(0x12345678, GetPropertyValue<int>(header, "Magic"));
        Assert.AreEqual((short)0x0100, GetPropertyValue<short>(header, "Version"));
    }

    [TestMethod]
    public void Interpret_InlineSchema_MixedWithRegularFields_ShouldParseAllFields()
    {
        // Arrange: Binary schema with mix of regular and inline fields
        // binary Packet { Preamble: byte, Header: { Magic: int le, Version: short le }, Footer: byte }
        var registry = new SchemaRegistry();

        var preambleField = CreatePrimitiveField("Preamble", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var magicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var versionField = CreatePrimitiveField("Version", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var inlineSchema = new InlineSchemaTypeNode(new SchemaFieldNode[] { magicField, versionField });
        var headerField = new FieldDefinitionNode("Header", inlineSchema);

        var footerField = CreatePrimitiveField("Footer", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var binarySchema =
            new BinarySchemaNode("Packet", new SchemaFieldNode[] { preambleField, headerField, footerField });
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        // Create test data: Preamble=0xAA, Magic=0x12345678 (le), Version=0x0100 (le), Footer=0xBB
        var data = new byte[] { 0xAA, 0x78, 0x56, 0x34, 0x12, 0x00, 0x01, 0xBB };

        // Act
        var result = InvokeInterpret(interpreter, data);

        // Assert
        Assert.AreEqual((byte)0xAA, GetPropertyValue<byte>(result, "Preamble"));

        var header = GetPropertyValue<object>(result, "Header");
        Assert.IsNotNull(header);
        Assert.AreEqual(0x12345678, GetPropertyValue<int>(header, "Magic"));
        Assert.AreEqual((short)0x0100, GetPropertyValue<short>(header, "Version"));

        Assert.AreEqual((byte)0xBB, GetPropertyValue<byte>(result, "Footer"));
    }

    [TestMethod]
    public void Interpret_InlineSchema_MultipleInlineFields_ShouldParseEachIndependently()
    {
        // Arrange: Binary schema with multiple inline fields
        // binary Packet { Header: { Magic: int le }, Footer: { Checksum: byte } }
        var registry = new SchemaRegistry();

        var magicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var headerInline = new InlineSchemaTypeNode(new SchemaFieldNode[] { magicField });
        var headerField = new FieldDefinitionNode("Header", headerInline);

        var checksumField = CreatePrimitiveField("Checksum", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var footerInline = new InlineSchemaTypeNode(new SchemaFieldNode[] { checksumField });
        var footerField = new FieldDefinitionNode("Footer", footerInline);

        var binarySchema = new BinarySchemaNode("Packet", new SchemaFieldNode[] { headerField, footerField });
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        // Create test data: Magic=0xDEADBEEF (le), Checksum=0xFF
        var data = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE, 0xFF };

        // Act
        var result = InvokeInterpret(interpreter, data);

        // Assert
        var header = GetPropertyValue<object>(result, "Header");
        Assert.IsNotNull(header);
        Assert.AreEqual(unchecked((int)0xDEADBEEF), GetPropertyValue<int>(header, "Magic"));

        var footer = GetPropertyValue<object>(result, "Footer");
        Assert.IsNotNull(footer);
        Assert.AreEqual((byte)0xFF, GetPropertyValue<byte>(footer, "Checksum"));
    }

    [TestMethod]
    public void Interpret_InlineSchema_BytesConsumed_ShouldAccountForNestedFields()
    {
        // Arrange: Verify that bytes consumed includes inline schema fields
        // binary Packet { Header: { A: int le, B: short le } }
        var registry = new SchemaRegistry();

        var aField = CreatePrimitiveField("A", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var bField = CreatePrimitiveField("B", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var inlineSchema = new InlineSchemaTypeNode(new SchemaFieldNode[] { aField, bField });
        var headerField = new FieldDefinitionNode("Header", inlineSchema);

        var binarySchema = new BinarySchemaNode("Packet", new SchemaFieldNode[] { headerField });
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        // 4 bytes (int) + 2 bytes (short) = 6 bytes total
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0xFF, 0xFF };

        // Act
        _ = InvokeInterpret(interpreter, data);
        // BytesConsumed is on the interpreter, not the result
        var bytesConsumed = GetPropertyValue<int>(interpreter, "BytesConsumed");

        // Assert - should consume exactly 6 bytes
        Assert.AreEqual(6, bytesConsumed);
    }

    [TestMethod]
    public void Interpret_InlineSchema_EmptyInline_ShouldNotConsumeBytes()
    {
        // Arrange: Empty inline schema should not consume any bytes
        // binary Packet { Marker: byte, Empty: { }, Trailer: byte }
        var registry = new SchemaRegistry();

        var markerField = CreatePrimitiveField("Marker", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var emptyInline = new InlineSchemaTypeNode(Array.Empty<SchemaFieldNode>());
        var emptyField = new FieldDefinitionNode("Empty", emptyInline);

        var trailerField = CreatePrimitiveField("Trailer", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var binarySchema =
            new BinarySchemaNode("Packet", new SchemaFieldNode[] { markerField, emptyField, trailerField });
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        // Just 2 bytes: Marker and Trailer
        var data = new byte[] { 0xAA, 0xBB };

        // Act
        var result = InvokeInterpret(interpreter, data);

        // Assert
        Assert.AreEqual((byte)0xAA, GetPropertyValue<byte>(result, "Marker"));
        Assert.IsNotNull(GetPropertyValue<object>(result, "Empty"));
        Assert.AreEqual((byte)0xBB, GetPropertyValue<byte>(result, "Trailer"));
    }

    #endregion

    #region Session 6: Binary Edge Cases

    /// <summary>
    ///     Tests 64-bit field parsing at maximum size.
    /// </summary>
    [TestMethod]
    public void Interpret_Int64_MaxValue_ShouldParseCorrectly()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Long, Endianness.LittleEndian));


        var data = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F };

        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<long>(result, "Value");

        Assert.AreEqual(long.MaxValue, value);
    }

    /// <summary>
    ///     Tests parsing with exact input size (no extra bytes).
    /// </summary>
    [TestMethod]
    public void Interpret_ExactSizedInput_NoExtraBytes_ShouldWork()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("A", PrimitiveTypeName.Byte, Endianness.NotApplicable),
            CreatePrimitiveField("B", PrimitiveTypeName.Byte, Endianness.NotApplicable));

        var data = new byte[] { 0x12, 0x34 };

        var result = InvokeInterpret(interpreter, data);

        Assert.AreEqual((byte)0x12, GetPropertyValue<byte>(result, "A"));
        Assert.AreEqual((byte)0x34, GetPropertyValue<byte>(result, "B"));
    }

    /// <summary>
    ///     Tests parsing when data has extra trailing bytes.
    /// </summary>
    [TestMethod]
    public void Interpret_ExtraTrailingBytes_ShouldIgnoreThem()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Short, Endianness.LittleEndian));


        var data = new byte[] { 0x34, 0x12, 0xFF, 0xFF, 0xFF, 0xFF };

        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<short>(result, "Value");

        Assert.AreEqual((short)0x1234, value);
    }

    /// <summary>
    ///     Tests zero-length byte array parsing.
    /// </summary>
    [TestMethod]
    public void Interpret_ZeroLengthByteArray_ShouldReturnEmptyArray()
    {
        var registry = new SchemaRegistry();
        var sizeExpr = new IntegerNode(0);
        var byteArrayType = new ByteArrayTypeNode(sizeExpr);
        var fields = new[]
        {
            new FieldDefinitionNode("EmptyPayload", byteArrayType)
        };
        var schema = new BinarySchemaNode("TestSchema", fields);
        registry.Register("TestSchema", schema);

        var interpreter = CompileInterpreter(registry, "TestSchema");
        var data = Array.Empty<byte>();

        var result = InvokeInterpret(interpreter, data);
        var payload = GetPropertyValue<byte[]>(result, "EmptyPayload");

        Assert.IsNotNull(payload);
        Assert.IsEmpty(payload);
    }

    /// <summary>
    ///     Tests big-endian parsing for all integer types.
    /// </summary>
    [TestMethod]
    public void Interpret_BigEndian_AllIntegerTypes_ShouldWork()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Short", PrimitiveTypeName.Short, Endianness.BigEndian),
            CreatePrimitiveField("Int", PrimitiveTypeName.Int, Endianness.BigEndian),
            CreatePrimitiveField("Long", PrimitiveTypeName.Long, Endianness.BigEndian));

        var data = new byte[]
        {
            0x12, 0x34,
            0x12, 0x34, 0x56, 0x78,
            0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0
        };

        var result = InvokeInterpret(interpreter, data);

        Assert.AreEqual((short)0x1234, GetPropertyValue<short>(result, "Short"));
        Assert.AreEqual(0x12345678, GetPropertyValue<int>(result, "Int"));
        Assert.AreEqual(0x123456789ABCDEF0L, GetPropertyValue<long>(result, "Long"));
    }

    /// <summary>
    ///     Tests multiple conditional fields with different conditions.
    /// </summary>
    [TestMethod]
    public void Interpret_MultipleConditionalFields_ShouldParseSelectivity()
    {
        var registry = new SchemaRegistry();

        var flagsField = CreatePrimitiveField("Flags", PrimitiveTypeName.Byte, Endianness.NotApplicable);


        var flagsRef1 = new IdentifierNode("Flags");
        var mask1 = new IntegerNode(1);
        var andExpr1 = new BitwiseAndNode(flagsRef1, mask1);
        var condition1 = new DiffNode(andExpr1, new IntegerNode(0));
        var fieldA = new FieldDefinitionNode("A",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable), null, null, condition1);


        var flagsRef2 = new IdentifierNode("Flags");
        var mask2 = new IntegerNode(2);
        var andExpr2 = new BitwiseAndNode(flagsRef2, mask2);
        var condition2 = new DiffNode(andExpr2, new IntegerNode(0));
        var fieldB = new FieldDefinitionNode("B",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable), null, null, condition2);

        var schema = new BinarySchemaNode("TestSchema", new[] { flagsField, fieldA, fieldB });
        registry.Register("TestSchema", schema);

        var interpreter = CompileInterpreter(registry, "TestSchema");


        var data = new byte[] { 0x03, 0xAA, 0xBB };

        var result = InvokeInterpret(interpreter, data);

        Assert.AreEqual((byte)0x03, GetPropertyValue<byte>(result, "Flags"));
        Assert.AreEqual((byte)0xAA, GetPropertyValue<byte?>(result, "A"));
        Assert.AreEqual((byte)0xBB, GetPropertyValue<byte?>(result, "B"));
    }

    /// <summary>
    ///     Tests conditional field when condition is false - only first conditional field present.
    /// </summary>
    [TestMethod]
    public void Interpret_ConditionalField_PartialFlags_ShouldParseCorrectly()
    {
        var registry = new SchemaRegistry();

        var flagsField = CreatePrimitiveField("Flags", PrimitiveTypeName.Byte, Endianness.NotApplicable);


        var flagsRef1 = new IdentifierNode("Flags");
        var mask1 = new IntegerNode(1);
        var andExpr1 = new BitwiseAndNode(flagsRef1, mask1);
        var condition1 = new DiffNode(andExpr1, new IntegerNode(0));
        var fieldA = new FieldDefinitionNode("A",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable), null, null, condition1);


        var flagsRef2 = new IdentifierNode("Flags");
        var mask2 = new IntegerNode(2);
        var andExpr2 = new BitwiseAndNode(flagsRef2, mask2);
        var condition2 = new DiffNode(andExpr2, new IntegerNode(0));
        var fieldB = new FieldDefinitionNode("B",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable), null, null, condition2);

        var schema = new BinarySchemaNode("TestSchema", new[] { flagsField, fieldA, fieldB });
        registry.Register("TestSchema", schema);

        var interpreter = CompileInterpreter(registry, "TestSchema");


        var data = new byte[] { 0x01, 0xAA };

        var result = InvokeInterpret(interpreter, data);

        Assert.AreEqual((byte)0x01, GetPropertyValue<byte>(result, "Flags"));
        Assert.AreEqual((byte)0xAA, GetPropertyValue<byte?>(result, "A"));
        Assert.IsNull(GetPropertyValue<byte?>(result, "B"));
    }

    #endregion
}
