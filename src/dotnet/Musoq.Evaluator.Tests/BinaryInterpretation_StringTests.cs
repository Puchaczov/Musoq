#nullable enable annotations

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretation_StringTests : BinaryInterpretationTestBase
{
    [TestMethod]
    public void Interpret_String_Utf8_ShouldParseCorrectly()
    {
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

        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual("Hello", name);
    }

    [TestMethod]
    public void Interpret_String_Ascii_ShouldParseCorrectly()
    {
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

        var result = InvokeInterpret(interpreter, data);
        var label = GetPropertyValue<string>(result, "Label");

        Assert.AreEqual("TEST", label);
    }

    [TestMethod]
    public void Interpret_String_WithTrimModifier_ShouldTrimWhitespace()
    {
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

        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual("Hello", name);
    }

    [TestMethod]
    public void Interpret_String_WithRTrimModifier_ShouldTrimTrailingWhitespace()
    {
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

        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual("  Hello", name);
    }

    [TestMethod]
    public void Interpret_String_WithLTrimModifier_ShouldTrimLeadingWhitespace()
    {
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

        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual("Hello   ", name);
    }

    [TestMethod]
    public void Interpret_String_WithNullTermModifier_ShouldStopAtNullByte()
    {
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
        var data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };

        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual("Hello", name);
    }

    [TestMethod]
    public void Interpret_String_WithNullTermAndTrim_ShouldApplyBoth()
    {
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
        var data = new byte[] { 0x20, 0x20, 0x48, 0x69, 0x20, 0x20, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual("Hi", name);
    }

    [TestMethod]
    public void Interpret_String_Utf16Le_ShouldParseCorrectly()
    {
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
        var data = Encoding.Unicode.GetBytes("Test");

        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual("Test", name);
    }

    [TestMethod]
    public void Interpret_String_Utf16Be_ShouldParseCorrectly()
    {
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
        var data = Encoding.BigEndianUnicode.GetBytes("Test");

        var result = InvokeInterpret(interpreter, data);
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual("Test", name);
    }

    [TestMethod]
    public void Interpret_String_Latin1_ShouldParseCorrectly()
    {
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
        var data = new byte[] { 0x63, 0x61, 0x66, 0xE9, 0x73 };

        var result = InvokeInterpret(interpreter, data);
        var text = GetPropertyValue<string>(result, "Text");

        Assert.AreEqual("cafés", text);
    }

    [TestMethod]
    public void Interpret_ByteArray_WithDynamicSize_ShouldUsePreviousField()
    {
        var registry = new SchemaRegistry();
        var lengthField = new FieldDefinitionNode("Length",
            new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian));
        var dataField = new FieldDefinitionNode("Data",
            new ByteArrayTypeNode(new AccessColumnNode("Length", string.Empty, TextSpan.Empty)));

        var fields = new[] { lengthField, dataField };
        var schema = new BinarySchemaNode("Record", fields);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");
        var data = new byte[] { 0x04, 0x00, 0xAA, 0xBB, 0xCC, 0xDD };

        var result = InvokeInterpret(interpreter, data);
        var length = GetPropertyValue<short>(result, "Length");
        var payload = GetPropertyValue<byte[]>(result, "Data");

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
        var data = new byte[] { 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F };

        var result = InvokeInterpret(interpreter, data);
        var nameLen = GetPropertyValue<byte>(result, "NameLen");
        var name = GetPropertyValue<string>(result, "Name");

        Assert.AreEqual((byte)5, nameLen);
        Assert.AreEqual("Hello", name);
    }
}
