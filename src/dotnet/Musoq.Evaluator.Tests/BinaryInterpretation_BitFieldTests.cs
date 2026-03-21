#nullable enable annotations

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretation_BitFieldTests : BinaryInterpretationTestBase
{
    #region Bit Field Tests (bits[N] and align[N])

    [TestMethod]
    public void Interpret_SingleBit_ShouldParseLeastSignificantBit()
    {
        var registry = new SchemaRegistry();

        var flag0Field = new FieldDefinitionNode("Flag0", new BitsTypeNode(1));

        var schema = new BinarySchemaNode("Flags", [flag0Field]);
        registry.Register("Flags", schema);

        var interpreter = CompileInterpreter(registry, "Flags");

        var data = new byte[] { 0x01 };

        var result = InvokeInterpret(interpreter, data);
        var flag0 = GetPropertyValue<byte>(result, "Flag0");

        Assert.AreEqual((byte)1, flag0);
    }

    [TestMethod]
    public void Interpret_SingleBit_ShouldParseMiddleBit()
    {
        var registry = new SchemaRegistry();

        var skipField = new FieldDefinitionNode("Skip", new BitsTypeNode(3));
        var targetField = new FieldDefinitionNode("Target", new BitsTypeNode(1));

        var schema = new BinarySchemaNode("Flags", [skipField, targetField]);
        registry.Register("Flags", schema);

        var interpreter = CompileInterpreter(registry, "Flags");

        var data = new byte[] { 0x08 };

        var result = InvokeInterpret(interpreter, data);
        var skip = GetPropertyValue<byte>(result, "Skip");
        var target = GetPropertyValue<byte>(result, "Target");

        Assert.AreEqual((byte)0, skip);
        Assert.AreEqual((byte)1, target);
    }

    [TestMethod]
    public void Interpret_MultipleBitsInOneByte_ShouldParseCorrectly()
    {
        var registry = new SchemaRegistry();

        var reservedField = new FieldDefinitionNode("Reserved", new BitsTypeNode(4));
        var dataOffField = new FieldDefinitionNode("DataOff", new BitsTypeNode(4));

        var schema = new BinarySchemaNode("TcpHeader", [reservedField, dataOffField]);
        registry.Register("TcpHeader", schema);

        var interpreter = CompileInterpreter(registry, "TcpHeader");

        var data = new byte[] { 0x53 };

        var result = InvokeInterpret(interpreter, data);
        var reserved = GetPropertyValue<byte>(result, "Reserved");
        var dataOff = GetPropertyValue<byte>(result, "DataOff");

        Assert.AreEqual((byte)3, reserved);
        Assert.AreEqual((byte)5, dataOff);
    }

    [TestMethod]
    public void Interpret_MultipleSingleBits_ShouldParseSequentially()
    {
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

        var data = new byte[] { 0xAA };

        var result = InvokeInterpret(interpreter, data);

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
        var registry = new SchemaRegistry();

        var valueField = new FieldDefinitionNode("Value", new BitsTypeNode(12));

        var schema = new BinarySchemaNode("CrossByte", [valueField]);
        registry.Register("CrossByte", schema);

        var interpreter = CompileInterpreter(registry, "CrossByte");

        var data = new byte[] { 0xAB, 0xCD };

        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<ushort>(result, "Value");

        Assert.AreEqual((ushort)0xDAB, value);
    }

    [TestMethod]
    public void Interpret_LargeBitField_ShouldParse32Bits()
    {
        var registry = new SchemaRegistry();

        var valueField = new FieldDefinitionNode("Value", new BitsTypeNode(32));

        var schema = new BinarySchemaNode("LargeBits", [valueField]);
        registry.Register("LargeBits", schema);

        var interpreter = CompileInterpreter(registry, "LargeBits");

        var data = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE };

        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<uint>(result, "Value");

        Assert.AreEqual(0xDEADBEEFU, value);
    }

    [TestMethod]
    public void Interpret_AlignToByteAfterBits_ShouldSkipRemainingBits()
    {
        var registry = new SchemaRegistry();

        var flagsField = new FieldDefinitionNode("Flags", new BitsTypeNode(3));
        var alignField = new FieldDefinitionNode("_align", new AlignmentNode(8));
        var nextByteField = new FieldDefinitionNode("NextByte",
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable));

        var schema = new BinarySchemaNode("Record", [flagsField, alignField, nextByteField]);
        registry.Register("Record", schema);

        var interpreter = CompileInterpreter(registry, "Record");

        var data = new byte[] { 0x05, 0x42 };

        var result = InvokeInterpret(interpreter, data);
        var flags = GetPropertyValue<byte>(result, "Flags");
        var nextByte = GetPropertyValue<byte>(result, "NextByte");

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

        var schema = new BinarySchemaNode("Mixed", [flagField, alignField, valueField]);
        registry.Register("Mixed", schema);

        var interpreter = CompileInterpreter(registry, "Mixed");

        var data = new byte[] { 0x0A, 0x34, 0x12 };

        var result = InvokeInterpret(interpreter, data);
        var flag = GetPropertyValue<byte>(result, "Flag");
        var value = GetPropertyValue<short>(result, "Value");

        Assert.AreEqual((byte)0x0A, flag);
        Assert.AreEqual((short)0x1234, value);
    }

    [TestMethod]
    public void Interpret_TcpFlagsLikeBitPattern_ShouldParseCorrectly()
    {
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

        var data = new byte[] { 0x50, 0x12 };

        var result = InvokeInterpret(interpreter, data);

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
        var registry = new SchemaRegistry();

        var fieldA = CreatePrimitiveField("A", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var fieldB = CreatePrimitiveField("B", PrimitiveTypeName.Int, Endianness.LittleEndian);

        var aRef = new IdentifierNode("A");
        var bRef = new IdentifierNode("B");
        var addExpr = new AddNode(aRef, bRef);
        var sumField = new ComputedFieldNode("Sum", addExpr);

        var schema = new BinarySchemaNode("Math", [fieldA, fieldB, sumField]);
        registry.Register("Math", schema);

        var interpreter = CompileInterpreter(registry, "Math");

        var data = new byte[8];
        BitConverter.GetBytes(10).CopyTo(data, 0);
        BitConverter.GetBytes(20).CopyTo(data, 4);

        var result = InvokeInterpret(interpreter, data);
        var a = GetPropertyValue<int>(result, "A");
        var b = GetPropertyValue<int>(result, "B");
        var sum = GetPropertyValue<object>(result, "Sum");

        Assert.AreEqual(10, a);
        Assert.AreEqual(20, b);
        Assert.AreEqual(30, sum);
    }

    [TestMethod]
    public void Interpret_ComputedField_EqualityComparison_ShouldReturnBool()
    {
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);

        var valueRef = new IdentifierNode("Value");
        var zeroNode = new IntegerNode(0);
        var eqExpr = new EqualityNode(valueRef, zeroNode);
        var isZeroField = new ComputedFieldNode("IsZero", eqExpr);

        var schema = new BinarySchemaNode("Check", [valueField, isZeroField]);
        registry.Register("Check", schema);

        var interpreter = CompileInterpreter(registry, "Check");

        var data = BitConverter.GetBytes(0);

        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result, "Value");
        var isZero = GetPropertyValue<bool>(result, "IsZero");

        Assert.AreEqual(0, value);
        Assert.IsTrue(isZero);
    }

    [TestMethod]
    public void Interpret_ComputedField_InequalityComparison_ShouldReturnBool()
    {
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);

        var valueRef = new IdentifierNode("Value");
        var zeroNode = new IntegerNode(0);
        var diffExpr = new DiffNode(valueRef, zeroNode);
        var hasValueField = new ComputedFieldNode("HasValue", diffExpr);

        var schema = new BinarySchemaNode("Check", [valueField, hasValueField]);
        registry.Register("Check", schema);

        var interpreter = CompileInterpreter(registry, "Check");

        var data = BitConverter.GetBytes(42);

        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result, "Value");
        var hasValue = GetPropertyValue<bool>(result, "HasValue");

        Assert.AreEqual(42, value);
        Assert.IsTrue(hasValue);
    }

    [TestMethod]
    public void Interpret_ComputedField_Multiplication_ShouldCalculateCorrectly()
    {
        var registry = new SchemaRegistry();

        var widthField = CreatePrimitiveField("Width", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var heightField = CreatePrimitiveField("Height", PrimitiveTypeName.Int, Endianness.LittleEndian);

        var widthRef = new IdentifierNode("Width");
        var heightRef = new IdentifierNode("Height");
        var mulExpr = new StarNode(widthRef, heightRef);
        var areaField = new ComputedFieldNode("Area", mulExpr);

        var schema = new BinarySchemaNode("Rectangle", [widthField, heightField, areaField]);
        registry.Register("Rectangle", schema);

        var interpreter = CompileInterpreter(registry, "Rectangle");

        var data = new byte[8];
        BitConverter.GetBytes(5).CopyTo(data, 0);
        BitConverter.GetBytes(7).CopyTo(data, 4);

        var result = InvokeInterpret(interpreter, data);
        var width = GetPropertyValue<int>(result, "Width");
        var height = GetPropertyValue<int>(result, "Height");
        var area = GetPropertyValue<object>(result, "Area");

        Assert.AreEqual(5, width);
        Assert.AreEqual(7, height);
        Assert.AreEqual(35, area);
    }

    [TestMethod]
    public void Interpret_ComputedField_GreaterThanComparison_ShouldReturnBool()
    {
        var registry = new SchemaRegistry();

        var sizeField = CreatePrimitiveField("Size", PrimitiveTypeName.Int, Endianness.LittleEndian);

        var sizeRef = new IdentifierNode("Size");
        var thresholdNode = new IntegerNode(100);
        var gtExpr = new GreaterNode(sizeRef, thresholdNode);
        var isLargeField = new ComputedFieldNode("IsLarge", gtExpr);

        var schema = new BinarySchemaNode("Data", [sizeField, isLargeField]);
        registry.Register("Data", schema);

        var interpreter = CompileInterpreter(registry, "Data");

        var data = BitConverter.GetBytes(150);

        var result = InvokeInterpret(interpreter, data);
        var size = GetPropertyValue<int>(result, "Size");
        var isLarge = GetPropertyValue<bool>(result, "IsLarge");

        Assert.AreEqual(150, size);
        Assert.IsTrue(isLarge);
    }

    [TestMethod]
    public void Interpret_ComputedField_NoByteConsumption_ShouldNotAdvanceCursor()
    {
        var registry = new SchemaRegistry();

        var magicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var versionField = CreatePrimitiveField("Version", PrimitiveTypeName.Byte, Endianness.NotApplicable);

        var magicRef = new IdentifierNode("Magic");
        var versionRef = new IdentifierNode("Version");
        var addExpr = new AddNode(magicRef, versionRef);
        var checksumField = new ComputedFieldNode("Checksum", addExpr);

        var schema = new BinarySchemaNode("Packet", [magicField, versionField, checksumField]);
        registry.Register("Packet", schema);

        var interpreter = CompileInterpreter(registry, "Packet");

        var data = new byte[] { 0x78, 0x56, 0x34, 0x12, 0x01 };

        var result = InvokeInterpret(interpreter, data);
        var magic = GetPropertyValue<int>(result, "Magic");
        var version = GetPropertyValue<byte>(result, "Version");
        var checksum = GetPropertyValue<object>(result, "Checksum");

        Assert.AreEqual(0x12345678, magic);
        Assert.AreEqual((byte)1, version);
        Assert.AreEqual(0x12345678 + 1, checksum);
    }

    #endregion
}
