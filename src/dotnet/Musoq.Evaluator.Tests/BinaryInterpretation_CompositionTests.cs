#nullable enable annotations

using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretation_CompositionTests : BinaryInterpretationTestBase
{
    #region Binary-Text Composition (as clause) Tests

    [TestMethod]
    public void Interpret_StringWithAsClause_ShouldParseWithTextSchema()
    {
        var registry = new SchemaRegistry();

        var textSchema = new TextSchemaNode("KeyValue", [
            new TextFieldDefinitionNode("Key", TextFieldType.Until, ":", null, TextFieldModifier.Trim),
            new TextFieldDefinitionNode("Value", TextFieldType.Rest, null, null, TextFieldModifier.Trim)
        ]);
        registry.Register("KeyValue", textSchema);

        var sizeExpr = new IntegerNode("20");
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.None, "KeyValue");
        var dataField = new FieldDefinitionNode("Data", stringType);
        var binarySchema = new BinarySchemaNode("Config", [dataField]);
        registry.Register("Config", binarySchema);

        var interpreter = CompileInterpreter(registry, "Config");

        var text = "Name: John Doe      ";
        var data = Encoding.UTF8.GetBytes(text);

        var result = InvokeInterpret(interpreter, data);
        var parsedData = GetPropertyValue<object>(result, "Data");

        Assert.IsNotNull(parsedData);
        Assert.AreEqual("Name", GetPropertyValue<string>(parsedData, "Key"));
        Assert.AreEqual("John Doe", GetPropertyValue<string>(parsedData, "Value"));
    }

    [TestMethod]
    public void Interpret_StringWithAsClauseAndModifiers_ShouldTrimThenParse()
    {
        var registry = new SchemaRegistry();

        var textSchema = new TextSchemaNode("SimpleText", [
            new TextFieldDefinitionNode("Content", TextFieldType.Rest)
        ]);
        registry.Register("SimpleText", textSchema);

        var sizeExpr = new IntegerNode("30");
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.Trim, "SimpleText");
        var dataField = new FieldDefinitionNode("Data", stringType);
        var binarySchema = new BinarySchemaNode("Config", [dataField]);
        registry.Register("Config", binarySchema);

        var interpreter = CompileInterpreter(registry, "Config");

        var text = "  Hello World  ".PadRight(30);
        var data = Encoding.UTF8.GetBytes(text);

        var result = InvokeInterpret(interpreter, data);
        var parsedData = GetPropertyValue<object>(result, "Data");

        Assert.IsNotNull(parsedData);
        Assert.AreEqual("Hello World", GetPropertyValue<string>(parsedData, "Content"));
    }

    [TestMethod]
    public void Interpret_StringWithAsClause_ComplexBinarySchema_ShouldParseMixedFields()
    {
        var registry = new SchemaRegistry();

        var textSchema = new TextSchemaNode("KeyValue", [
            new TextFieldDefinitionNode("Key", TextFieldType.Until, "=", null, TextFieldModifier.Trim),
            new TextFieldDefinitionNode("Value", TextFieldType.Rest, null, null, TextFieldModifier.Trim)
        ]);
        registry.Register("KeyValue", textSchema);

        var versionField = CreatePrimitiveField("Version", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var sizeExpr = new IntegerNode("15");
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Ascii, StringModifier.Trim, "KeyValue");
        var configField = new FieldDefinitionNode("Config", stringType);
        var checksumField = CreatePrimitiveField("Checksum", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var binarySchema =
            new BinarySchemaNode("Packet", [versionField, configField, checksumField]);
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        var configText = "Port=8080      ";
        var data = new byte[17];
        data[0] = 0x01;
        Encoding.ASCII.GetBytes(configText).CopyTo(data, 1);
        data[16] = 0xFF;

        var result = InvokeInterpret(interpreter, data);

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
        var registry = new SchemaRegistry();

        var textSchema = new TextSchemaNode("KeyValue", [
            new TextFieldDefinitionNode("Key", TextFieldType.Until, ":"),
            new TextFieldDefinitionNode("Value", TextFieldType.Rest)
        ]);
        registry.Register("KeyValue", textSchema);

        var sizeExpr = new IntegerNode("20");
        var stringType = new StringTypeNode(sizeExpr, StringEncoding.Utf8, StringModifier.NullTerm, "KeyValue");
        var dataField = new FieldDefinitionNode("Data", stringType);
        var binarySchema = new BinarySchemaNode("Config", [dataField]);
        registry.Register("Config", binarySchema);

        var interpreter = CompileInterpreter(registry, "Config");

        var data = new byte[20];
        Encoding.UTF8.GetBytes("Host:localhost").CopyTo(data, 0);
        data[14] = 0x00;
        for (var i = 15; i < 20; i++) data[i] = 0xFF;

        var result = InvokeInterpret(interpreter, data);
        var parsedData = GetPropertyValue<object>(result, "Data");

        Assert.IsNotNull(parsedData);
        Assert.AreEqual("Host", GetPropertyValue<string>(parsedData, "Key"));
        Assert.AreEqual("localhost", GetPropertyValue<string>(parsedData, "Value"));
    }

    #endregion

    #region Inline Schema Tests

    [TestMethod]
    public void Interpret_InlineSchema_SimpleFields_ShouldParseNestedStructure()
    {
        var registry = new SchemaRegistry();

        var magicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var versionField = CreatePrimitiveField("Version", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var inlineSchema = new InlineSchemaTypeNode([magicField, versionField]);

        var headerField = new FieldDefinitionNode("Header", inlineSchema);
        var binarySchema = new BinarySchemaNode("Packet", [headerField]);
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        var data = new byte[] { 0x78, 0x56, 0x34, 0x12, 0x00, 0x01 };

        var result = InvokeInterpret(interpreter, data);
        var header = GetPropertyValue<object>(result, "Header");

        Assert.IsNotNull(header);
        Assert.AreEqual(0x12345678, GetPropertyValue<int>(header, "Magic"));
        Assert.AreEqual((short)0x0100, GetPropertyValue<short>(header, "Version"));
    }

    [TestMethod]
    public void Interpret_InlineSchema_MixedWithRegularFields_ShouldParseAllFields()
    {
        var registry = new SchemaRegistry();

        var preambleField = CreatePrimitiveField("Preamble", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var magicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var versionField = CreatePrimitiveField("Version", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var inlineSchema = new InlineSchemaTypeNode([magicField, versionField]);
        var headerField = new FieldDefinitionNode("Header", inlineSchema);

        var footerField = CreatePrimitiveField("Footer", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var binarySchema =
            new BinarySchemaNode("Packet", [preambleField, headerField, footerField]);
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        var data = new byte[] { 0xAA, 0x78, 0x56, 0x34, 0x12, 0x00, 0x01, 0xBB };

        var result = InvokeInterpret(interpreter, data);

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
        var registry = new SchemaRegistry();

        var magicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var headerInline = new InlineSchemaTypeNode([magicField]);
        var headerField = new FieldDefinitionNode("Header", headerInline);

        var checksumField = CreatePrimitiveField("Checksum", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var footerInline = new InlineSchemaTypeNode([checksumField]);
        var footerField = new FieldDefinitionNode("Footer", footerInline);

        var binarySchema = new BinarySchemaNode("Packet", [headerField, footerField]);
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        var data = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE, 0xFF };

        var result = InvokeInterpret(interpreter, data);

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
        var registry = new SchemaRegistry();

        var aField = CreatePrimitiveField("A", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var bField = CreatePrimitiveField("B", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var inlineSchema = new InlineSchemaTypeNode([aField, bField]);
        var headerField = new FieldDefinitionNode("Header", inlineSchema);

        var binarySchema = new BinarySchemaNode("Packet", [headerField]);
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0xFF, 0xFF };

        _ = InvokeInterpret(interpreter, data);
        var bytesConsumed = GetPropertyValue<int>(interpreter, "BytesConsumed");

        Assert.AreEqual(6, bytesConsumed);
    }

    [TestMethod]
    public void Interpret_InlineSchema_EmptyInline_ShouldNotConsumeBytes()
    {
        var registry = new SchemaRegistry();

        var markerField = CreatePrimitiveField("Marker", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var emptyInline = new InlineSchemaTypeNode([]);
        var emptyField = new FieldDefinitionNode("Empty", emptyInline);

        var trailerField = CreatePrimitiveField("Trailer", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var binarySchema =
            new BinarySchemaNode("Packet", [markerField, emptyField, trailerField]);
        registry.Register("Packet", binarySchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        var data = new byte[] { 0xAA, 0xBB };

        var result = InvokeInterpret(interpreter, data);

        Assert.AreEqual((byte)0xAA, GetPropertyValue<byte>(result, "Marker"));
        Assert.IsNotNull(GetPropertyValue<object>(result, "Empty"));
        Assert.AreEqual((byte)0xBB, GetPropertyValue<byte>(result, "Trailer"));
    }

    #endregion

    #region Binary Edge Cases

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

        var schema = new BinarySchemaNode("TestSchema", [flagsField, fieldA, fieldB]);
        registry.Register("TestSchema", schema);

        var interpreter = CompileInterpreter(registry, "TestSchema");

        var data = new byte[] { 0x03, 0xAA, 0xBB };

        var result = InvokeInterpret(interpreter, data);

        Assert.AreEqual((byte)0x03, GetPropertyValue<byte>(result, "Flags"));
        Assert.AreEqual((byte)0xAA, GetPropertyValue<byte?>(result, "A"));
        Assert.AreEqual((byte)0xBB, GetPropertyValue<byte?>(result, "B"));
    }

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

        var schema = new BinarySchemaNode("TestSchema", [flagsField, fieldA, fieldB]);
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
