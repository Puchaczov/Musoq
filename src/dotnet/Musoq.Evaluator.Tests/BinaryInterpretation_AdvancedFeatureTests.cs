#nullable enable annotations

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretation_AdvancedFeatureTests : BinaryInterpretationTestBase
{
    #region Repeat Until Tests

    [TestMethod]
    public void Interpret_RepeatUntilPrimitive_ShouldParseUntilCondition()
    {
        var registry = new SchemaRegistry();

        var primitiveType = new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var condition = new EqualityNode(
            new AccessColumnNode("Bytes", string.Empty, TextSpan.Empty),
            new IntegerNode("0"));
        var repeatUntilType = new RepeatUntilTypeNode(primitiveType, condition, "Bytes");
        var bytesField = new FieldDefinitionNode("Bytes", repeatUntilType);
        var schema = new BinarySchemaNode("ByteSequence", [bytesField]);
        registry.Register("ByteSequence", schema);

        var interpreter = CompileInterpreter(registry, "ByteSequence");

        var data = new byte[] { 0x01, 0x02, 0x03, 0x00 };

        var result = InvokeInterpret(interpreter, data);
        var bytes = GetPropertyValue<byte[]>(result, "Bytes");

        Assert.HasCount(4, bytes);
        Assert.AreEqual((byte)0x01, bytes[0]);
        Assert.AreEqual((byte)0x02, bytes[1]);
        Assert.AreEqual((byte)0x03, bytes[2]);
        Assert.AreEqual((byte)0x00, bytes[3]);
    }

    [TestMethod]
    public void Interpret_RepeatUntilSchemaRef_ShouldParseUntilCondition()
    {
        var registry = new SchemaRegistry();

        var typeField = CreatePrimitiveField("Type", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var recordSchema = new BinarySchemaNode("Record", [typeField, valueField]);
        registry.Register("Record", recordSchema);

        var schemaRefType = new SchemaReferenceTypeNode("Record");
        var condition = new EqualityNode(
            new DotNode(
                new AccessColumnNode("Records", string.Empty, TextSpan.Empty),
                new IdentifierNode("Type"),
                "Records.Type"),
            new IntegerNode("0"));
        var repeatUntilType = new RepeatUntilTypeNode(schemaRefType, condition, "Records");
        var recordsField = new FieldDefinitionNode("Records", repeatUntilType);
        var recordListSchema = new BinarySchemaNode("RecordList", [recordsField]);
        registry.Register("RecordList", recordListSchema);

        var interpreter = CompileInterpreter(registry, "RecordList");

        var data = new byte[]
        {
            0x01, 0x64, 0x00, 0x00, 0x00,
            0x02, 0xC8, 0x00, 0x00, 0x00,
            0x00, 0x2C, 0x01, 0x00, 0x00
        };

        var result = InvokeInterpret(interpreter, data);
        var records = GetPropertyValue<object[]>(result, "Records");

        Assert.HasCount(3, records);

        Assert.AreEqual((byte)0x01, GetPropertyValue<byte>(records[0], "Type"));
        Assert.AreEqual(100, GetPropertyValue<int>(records[0], "Value"));

        Assert.AreEqual((byte)0x02, GetPropertyValue<byte>(records[1], "Type"));
        Assert.AreEqual(200, GetPropertyValue<int>(records[1], "Value"));

        Assert.AreEqual((byte)0x00, GetPropertyValue<byte>(records[2], "Type"));
        Assert.AreEqual(300, GetPropertyValue<int>(records[2], "Value"));
    }

    [TestMethod]
    public void Interpret_RepeatUntilWithCount_ShouldStopAtCount()
    {
        var registry = new SchemaRegistry();

        var idField = CreatePrimitiveField("Id", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemSchema = new BinarySchemaNode("Item", [idField]);
        registry.Register("Item", itemSchema);

        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var schemaRefType = new SchemaReferenceTypeNode("Item");
        var condition = new EqualityNode(
            new DotNode(
                new AccessColumnNode("Items", string.Empty, TextSpan.Empty),
                new IdentifierNode("Id"),
                "Items.Id"),
            new IntegerNode("255"));
        var repeatUntilType = new RepeatUntilTypeNode(schemaRefType, condition, "Items");
        var itemsField = new FieldDefinitionNode("Items", repeatUntilType);
        var itemListSchema = new BinarySchemaNode("ItemList", [countField, itemsField]);
        registry.Register("ItemList", itemListSchema);

        var interpreter = CompileInterpreter(registry, "ItemList");

        var data = new byte[]
        {
            0x03,
            0x01,
            0x02,
            0xFF
        };

        var result = InvokeInterpret(interpreter, data);
        var count = GetPropertyValue<byte>(result, "Count");
        var items = GetPropertyValue<object[]>(result, "Items");

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
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));

        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };

        var (success, result) = InvokeTryInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result!, "Value");

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

    #endregion

    #region PartialInterpret Tests

    [TestMethod]
    public void PartialInterpret_ValidData_ShouldReturnSuccessfulResult()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));

        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };

        var result = InvokePartialInterpret(interpreter, data);

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
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian));

        var data = new byte[] { 0x78, 0x56 };

        var result = InvokePartialInterpret(interpreter, data);

        Assert.IsFalse(GetPropertyValue<bool>(result, "IsSuccess"));
        Assert.IsNotNull(GetPropertyValue<string>(result, "ErrorMessage"));
    }

    [TestMethod]
    public void PartialInterpret_EmptyData_ShouldReturnFailure()
    {
        var interpreter = CreateAndCompileInterpreter("TestSchema",
            CreatePrimitiveField("Value", PrimitiveTypeName.Byte, Endianness.NotApplicable));

        var data = Array.Empty<byte>();

        var result = InvokePartialInterpret(interpreter, data);

        Assert.IsFalse(GetPropertyValue<bool>(result, "IsSuccess"));
        Assert.IsNotNull(GetPropertyValue<string>(result, "ErrorMessage"));
    }

    #endregion
}
