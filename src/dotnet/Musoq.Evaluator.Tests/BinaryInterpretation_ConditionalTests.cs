#nullable enable annotations

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretation_ConditionalTests : BinaryInterpretationTestBase
{
    [TestMethod]
    public void Interpret_ConditionalField_WhenTrue_ShouldParseField()
    {
        var registry = new SchemaRegistry();

        var hasPayloadField = CreatePrimitiveField("HasPayload", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var hasPayloadRef = new IdentifierNode("HasPayload");
        var zeroNode = new IntegerNode(0);
        var condition = new DiffNode(hasPayloadRef, zeroNode);

        var payloadType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var payloadField = new FieldDefinitionNode("Payload", payloadType, null, null, condition);

        var schema = new BinarySchemaNode("Message", [hasPayloadField, payloadField]);
        registry.Register("Message", schema);

        var interpreter = CompileInterpreter(registry, "Message");

        var data = new byte[] { 0x01, 0x78, 0x56, 0x34, 0x12 };

        var result = InvokeInterpret(interpreter, data);
        var hasPayload = GetPropertyValue<byte>(result, "HasPayload");
        var payload = GetPropertyValue<int?>(result, "Payload");

        Assert.AreEqual((byte)1, hasPayload);
        Assert.AreEqual(0x12345678, payload);
    }

    [TestMethod]
    public void Interpret_ConditionalField_WhenFalse_ShouldBeNull()
    {
        var registry = new SchemaRegistry();

        var hasPayloadField = CreatePrimitiveField("HasPayload", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var hasPayloadRef = new IdentifierNode("HasPayload");
        var zeroNode = new IntegerNode(0);
        var condition = new DiffNode(hasPayloadRef, zeroNode);

        var payloadType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var payloadField = new FieldDefinitionNode("Payload", payloadType, null, null, condition);

        var schema = new BinarySchemaNode("Message", [hasPayloadField, payloadField]);
        registry.Register("Message", schema);

        var interpreter = CompileInterpreter(registry, "Message");

        var data = new byte[] { 0x00 };

        var result = InvokeInterpret(interpreter, data);
        var hasPayload = GetPropertyValue<byte>(result, "HasPayload");
        var payload = GetPropertyValue<int?>(result, "Payload");

        Assert.AreEqual((byte)0, hasPayload);
        Assert.IsNull(payload);
    }

    [TestMethod]
    public void Interpret_ConditionalField_NoCursorAdvanceWhenFalse()
    {
        var registry = new SchemaRegistry();

        var hasExtraField = CreatePrimitiveField("HasExtra", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var hasExtraRef = new IdentifierNode("HasExtra");
        var zeroNode = new IntegerNode(0);
        var condition = new DiffNode(hasExtraRef, zeroNode);

        var extraType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var extraField = new FieldDefinitionNode("Extra", extraType, null, null, condition);

        var requiredField = CreatePrimitiveField("Required", PrimitiveTypeName.Short, Endianness.LittleEndian);

        var schema = new BinarySchemaNode("Message", [hasExtraField, extraField, requiredField]);
        registry.Register("Message", schema);

        var interpreter = CompileInterpreter(registry, "Message");

        var data = new byte[] { 0x00, 0x34, 0x12 };

        var result = InvokeInterpret(interpreter, data);
        var hasExtra = GetPropertyValue<byte>(result, "HasExtra");
        var extra = GetPropertyValue<int?>(result, "Extra");
        var required = GetPropertyValue<short>(result, "Required");

        Assert.AreEqual((byte)0, hasExtra);
        Assert.IsNull(extra);
        Assert.AreEqual((short)0x1234, required);
    }

    [TestMethod]
    public void Interpret_CheckConstraint_WhenValid_ShouldParse()
    {
        var registry = new SchemaRegistry();

        var magicType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);

        var magicRef = new IdentifierNode("Magic");
        var expectedMagic = new IntegerNode(unchecked((int)0xDEADBEEF));
        var checkExpr = new EqualityNode(magicRef, expectedMagic);
        var constraint = new FieldConstraintNode(checkExpr);

        var magicField = new FieldDefinitionNode("Magic", magicType, constraint);

        var schema = new BinarySchemaNode("Header", [magicField]);
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        var data = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE };

        var result = InvokeInterpret(interpreter, data);
        var magic = GetPropertyValue<int>(result, "Magic");

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

        var schema = new BinarySchemaNode("Header", [magicField]);
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };

        Assert.Throws<System.Exception>(() => InvokeInterpret(interpreter, data));
    }

    [TestMethod]
    public void Interpret_CheckConstraint_RangeValidation_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var versionType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);

        var versionRef1 = new IdentifierNode("Version");
        var versionRef2 = new IdentifierNode("Version");
        var one = new IntegerNode(1);
        var five = new IntegerNode(5);
        var gte1 = new GreaterOrEqualNode(versionRef1, one);
        var lte5 = new LessOrEqualNode(versionRef2, five);
        var checkExpr = new AndNode(gte1, lte5);
        var constraint = new FieldConstraintNode(checkExpr);

        var versionField = new FieldDefinitionNode("Version", versionType, constraint);

        var schema = new BinarySchemaNode("Header", [versionField]);
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        var data = new byte[] { 0x03, 0x00 };

        var result = InvokeInterpret(interpreter, data);
        var version = GetPropertyValue<short>(result, "Version");

        Assert.AreEqual((short)3, version);
    }

    [TestMethod]
    public void Interpret_AtPositioning_LiteralOffset_ShouldReadFromPosition()
    {
        var registry = new SchemaRegistry();

        var magicType = new StringTypeNode(new IntegerNode(2), StringEncoding.Ascii);
        var magicField = new FieldDefinitionNode("DosMagic", magicType, null, new IntegerNode(0));

        var peOffsetType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var peOffsetField = new FieldDefinitionNode("PeOffset", peOffsetType, null, new IntegerNode(60));

        var schema = new BinarySchemaNode("PeHeader", [magicField, peOffsetField]);
        registry.Register("PeHeader", schema);

        var interpreter = CompileInterpreter(registry, "PeHeader");

        var data = new byte[64];
        data[0] = (byte)'M';
        data[1] = (byte)'Z';
        data[60] = 0x80;
        data[61] = 0x00;
        data[62] = 0x00;
        data[63] = 0x00;

        var result = InvokeInterpret(interpreter, data);
        var dosMagic = GetPropertyValue<string>(result, "DosMagic");
        var peOffset = GetPropertyValue<int>(result, "PeOffset");

        Assert.AreEqual("MZ", dosMagic);
        Assert.AreEqual(128, peOffset);
    }

    [TestMethod]
    public void Interpret_AtPositioning_HexOffset_ShouldReadFromPosition()
    {
        var registry = new SchemaRegistry();

        var sigType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var sigField = new FieldDefinitionNode("Signature", sigType, null, new HexIntegerNode(0x10));

        var schema = new BinarySchemaNode("Header", [sigField]);
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        var data = new byte[20];
        data[16] = 0xEF;
        data[17] = 0xBE;
        data[18] = 0xAD;
        data[19] = 0xDE;

        var result = InvokeInterpret(interpreter, data);
        var signature = GetPropertyValue<int>(result, "Signature");

        Assert.AreEqual(unchecked((int)0xDEADBEEF), signature);
    }

    [TestMethod]
    public void Interpret_AtPositioning_FieldReference_ShouldReadFromDynamicPosition()
    {
        var registry = new SchemaRegistry();

        var dataOffsetType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);
        var dataOffsetField = new FieldDefinitionNode("DataOffset", dataOffsetType);

        var dataType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var atOffset = new IdentifierNode("DataOffset");
        var dataField = new FieldDefinitionNode("Data", dataType, null, atOffset);

        var schema = new BinarySchemaNode("Header", [dataOffsetField, dataField]);
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        var data = new byte[12];
        data[0] = 0x08;
        data[1] = 0x00;
        data[8] = 0x78;
        data[9] = 0x56;
        data[10] = 0x34;
        data[11] = 0x12;

        var result = InvokeInterpret(interpreter, data);
        var dataOffset = GetPropertyValue<short>(result, "DataOffset");
        var dataValue = GetPropertyValue<int>(result, "Data");

        Assert.AreEqual((short)8, dataOffset);
        Assert.AreEqual(0x12345678, dataValue);
    }

    [TestMethod]
    public void Interpret_AtPositioning_Expression_ShouldCalculatePosition()
    {
        var registry = new SchemaRegistry();

        var baseOffsetType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var baseOffsetField = new FieldDefinitionNode("BaseOffset", baseOffsetType);

        var valueType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);
        var baseOffsetRef = new IdentifierNode("BaseOffset");
        var offsetExpr = new AddNode(baseOffsetRef, new IntegerNode(4));
        var valueField = new FieldDefinitionNode("Value", valueType, null, offsetExpr);

        var schema = new BinarySchemaNode("Header", [baseOffsetField, valueField]);
        registry.Register("Header", schema);

        var interpreter = CompileInterpreter(registry, "Header");

        var data = new byte[16];
        data[0] = 0x0A;
        data[1] = 0x00;
        data[2] = 0x00;
        data[3] = 0x00;
        data[14] = 0x34;
        data[15] = 0x12;

        var result = InvokeInterpret(interpreter, data);
        var baseOffset = GetPropertyValue<int>(result, "BaseOffset");
        var value = GetPropertyValue<short>(result, "Value");

        Assert.AreEqual(10, baseOffset);
        Assert.AreEqual((short)0x1234, value);
    }

    [TestMethod]
    public void Interpret_AtPositioning_BackwardJump_ShouldRereadEarlierData()
    {
        var registry = new SchemaRegistry();

        var fieldAType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var fieldA = new FieldDefinitionNode("FieldA", fieldAType);

        var fieldBType = new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian);
        var fieldB = new FieldDefinitionNode("FieldB", fieldBType, null, new IntegerNode(0));

        var fieldCType = new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var fieldC = new FieldDefinitionNode("FieldC", fieldCType, null, new IntegerNode(2));

        var schema = new BinarySchemaNode("Record", [fieldA, fieldB, fieldC]);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");

        var data = new byte[] { 0x78, 0x56, 0x34, 0x12 };

        var result = InvokeInterpret(interpreter, data);
        var a = GetPropertyValue<int>(result, "FieldA");
        var b = GetPropertyValue<short>(result, "FieldB");
        var c = GetPropertyValue<byte>(result, "FieldC");

        Assert.AreEqual(0x12345678, a);
        Assert.AreEqual((short)0x5678, b);
        Assert.AreEqual((byte)0x34, c);
    }

    [TestMethod]
    public void Interpret_AtPositioning_WithConditionalField_ShouldCombineModifiers()
    {
        var registry = new SchemaRegistry();

        var hasDataField = CreatePrimitiveField("HasData", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var dataOffsetField = CreatePrimitiveField("DataOffset", PrimitiveTypeName.Short, Endianness.LittleEndian);

        var dataType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var atOffset = new IdentifierNode("DataOffset");
        var hasDataRef = new IdentifierNode("HasData");
        var whenCondition = new DiffNode(hasDataRef, new IntegerNode(0));
        var dataField = new FieldDefinitionNode("Data", dataType, null, atOffset, whenCondition);

        var schema = new BinarySchemaNode("Record", [hasDataField, dataOffsetField, dataField]);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");

        var data = new byte[12];
        data[0] = 0x01;
        data[1] = 0x08;
        data[2] = 0x00;
        data[8] = 0xEF;
        data[9] = 0xBE;
        data[10] = 0xAD;
        data[11] = 0xDE;

        var result = InvokeInterpret(interpreter, data);
        var hasData = GetPropertyValue<byte>(result, "HasData");
        var dataOffset = GetPropertyValue<short>(result, "DataOffset");
        var dataValue = GetPropertyValue<int?>(result, "Data");

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

        var schema = new BinarySchemaNode("Record", [hasDataField, dataOffsetField, dataField]);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");

        var data = new byte[3];
        data[0] = 0x00;
        data[1] = 0x08;
        data[2] = 0x00;

        var result = InvokeInterpret(interpreter, data);
        var hasData = GetPropertyValue<byte>(result, "HasData");
        var dataValue = GetPropertyValue<int?>(result, "Data");

        Assert.AreEqual((byte)0, hasData);
        Assert.IsNull(dataValue);
    }
}
