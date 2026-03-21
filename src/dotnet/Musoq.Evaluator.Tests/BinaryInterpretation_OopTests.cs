#nullable enable annotations

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class BinaryInterpretation_OopTests : BinaryInterpretationTestBase
{
    #region Schema Inheritance Tests

    [TestMethod]
    public void Interpret_Inheritance_BasicExtends_ShouldIncludeParentFields()
    {
        var registry = new SchemaRegistry();

        var parentVersionField = CreatePrimitiveField("Version", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var parentSchema = new BinarySchemaNode("Parent", [parentVersionField]);
        registry.Register("Parent", parentSchema);

        var childFlagsField = CreatePrimitiveField("Flags", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var childSchema = new BinarySchemaNode("Child", [childFlagsField], "Parent");
        registry.Register("Child", childSchema);

        var interpreter = CompileInterpreter(registry, "Child");

        var data = new byte[] { 0x01, 0xFF };

        var result = InvokeInterpret(interpreter, data);
        var version = GetPropertyValue<byte>(result, "Version");
        var flags = GetPropertyValue<byte>(result, "Flags");

        Assert.AreEqual((byte)0x01, version);
        Assert.AreEqual((byte)0xFF, flags);
    }

    [TestMethod]
    public void Interpret_Inheritance_ChildComputedFieldReferencesParent_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var parentValueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var parentSchema = new BinarySchemaNode("Parent", [parentValueField]);
        registry.Register("Parent", parentSchema);

        var valueRef = new IdentifierNode("Value");
        var twoNode = new IntegerNode(2);
        var mulExpr = new StarNode(valueRef, twoNode);
        var doubledField = new ComputedFieldNode("DoubledValue", mulExpr);

        var childSchema = new BinarySchemaNode("Child", [doubledField], "Parent");
        registry.Register("Child", childSchema);

        var interpreter = CompileInterpreter(registry, "Child");

        var data = BitConverter.GetBytes(25);

        var result = InvokeInterpret(interpreter, data);
        var value = GetPropertyValue<int>(result, "Value");
        var doubled = GetPropertyValue<object>(result, "DoubledValue");

        Assert.AreEqual(25, value);
        Assert.AreEqual(50, doubled);
    }

    [TestMethod]
    public void Interpret_Inheritance_ParentFieldsParsedFirst_ShouldMaintainOrder()
    {
        var registry = new SchemaRegistry();

        var parentMagicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var parentVersionField = CreatePrimitiveField("Version", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var parentSchema =
            new BinarySchemaNode("Header", [parentMagicField, parentVersionField]);
        registry.Register("Header", parentSchema);

        var childLengthField = CreatePrimitiveField("DataLength", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var childDataField = new FieldDefinitionNode("Data", new ArrayTypeNode(
            new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable),
            new IdentifierNode("DataLength")));

        var childSchema = new BinarySchemaNode("Packet", [childLengthField, childDataField],
            "Header");
        registry.Register("Packet", childSchema);

        var interpreter = CompileInterpreter(registry, "Packet");

        var data = new byte[]
        {
            0xEF, 0xBE, 0xAD, 0xDE,
            0x01, 0x00,
            0x03, 0x00, 0x00, 0x00,
            0xAA, 0xBB, 0xCC
        };

        var result = InvokeInterpret(interpreter, data);
        var magic = GetPropertyValue<int>(result, "Magic");
        var version = GetPropertyValue<short>(result, "Version");
        var dataLength = GetPropertyValue<int>(result, "DataLength");
        var dataValue = GetPropertyValue<byte[]>(result, "Data");

        Assert.AreEqual(unchecked((int)0xDEADBEEF), magic);
        Assert.AreEqual((short)1, version);
        Assert.AreEqual(3, dataLength);
        CollectionAssert.AreEqual(new byte[] { 0xAA, 0xBB, 0xCC }, dataValue);
    }

    [TestMethod]
    public void Interpret_Inheritance_MultiLevel_ShouldIncludeAllAncestorFields()
    {
        var registry = new SchemaRegistry();

        var baseMagicField = CreatePrimitiveField("Magic", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var baseSchema = new BinarySchemaNode("Base", [baseMagicField]);
        registry.Register("Base", baseSchema);

        var childVersionField = CreatePrimitiveField("Version", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var childSchema = new BinarySchemaNode("Child", [childVersionField], "Base");
        registry.Register("Child", childSchema);

        var grandchildFlagsField = CreatePrimitiveField("Flags", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var grandchildSchema =
            new BinarySchemaNode("Grandchild", [grandchildFlagsField], "Child");
        registry.Register("Grandchild", grandchildSchema);

        var interpreter = CompileInterpreter(registry, "Grandchild");

        var data = new byte[] { 0x01, 0x02, 0x03 };

        var result = InvokeInterpret(interpreter, data);
        var magic = GetPropertyValue<byte>(result, "Magic");
        var version = GetPropertyValue<byte>(result, "Version");
        var flags = GetPropertyValue<byte>(result, "Flags");

        Assert.AreEqual((byte)0x01, magic);
        Assert.AreEqual((byte)0x02, version);
        Assert.AreEqual((byte)0x03, flags);
    }

    #endregion

    #region Generic Schema Tests

    [TestMethod]
    public void Interpret_GenericSchema_SingleTypeParameter_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var recordSchema = new BinarySchemaNode("Record", [valueField]);
        registry.Register("Record", recordSchema);

        var lengthField = CreatePrimitiveField("Length", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("T"));
        var genericSchema = new BinarySchemaNode("LengthPrefixed", [lengthField, dataField],
            null, ["T"]);
        registry.Register("LengthPrefixed", genericSchema);

        var prefixedField =
            new FieldDefinitionNode("Prefixed", new SchemaReferenceTypeNode("LengthPrefixed", ["Record"]));
        var wrapperSchema = new BinarySchemaNode("Wrapper", [prefixedField]);
        registry.Register("Wrapper", wrapperSchema);

        var interpreter = CompileInterpreter(registry, "Wrapper");

        var data = new byte[]
        {
            0x02, 0x00, 0x00, 0x00,
            0x34, 0x12
        };

        var result = InvokeInterpret(interpreter, data);
        var prefixed = GetPropertyValue<object>(result, "Prefixed");
        var length = GetPropertyValue<int>(prefixed, "Length");
        var dataObj = GetPropertyValue<object>(prefixed, "Data");
        var value = GetPropertyValue<short>(dataObj, "Value");

        Assert.AreEqual(2, length);
        Assert.AreEqual((short)0x1234, value);
    }

    [TestMethod]
    public void Interpret_GenericSchema_ArrayOfTypeParameter_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var idField = CreatePrimitiveField("Id", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemSchema = new BinarySchemaNode("Item", [idField]);
        registry.Register("Item", itemSchema);

        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemsField = new FieldDefinitionNode("Items", new ArrayTypeNode(
            new SchemaReferenceTypeNode("T"),
            new IdentifierNode("Count")));
        var genericSchema = new BinarySchemaNode("Container", [countField, itemsField], null,
            ["T"]);
        registry.Register("Container", genericSchema);

        var boxField = new FieldDefinitionNode("Box", new SchemaReferenceTypeNode("Container", ["Item"]));
        var holderSchema = new BinarySchemaNode("Holder", [boxField]);
        registry.Register("Holder", holderSchema);

        var interpreter = CompileInterpreter(registry, "Holder");

        var data = new byte[] { 0x03, 0x0A, 0x0B, 0x0C };

        var result = InvokeInterpret(interpreter, data);
        var box = GetPropertyValue<object>(result, "Box");
        var count = GetPropertyValue<byte>(box, "Count");
        var items = GetPropertyValue<object[]>(box, "Items");

        Assert.AreEqual((byte)3, count);
        Assert.HasCount(3, items);
        Assert.AreEqual((byte)0x0A, GetPropertyValue<byte>(items[0], "Id"));
        Assert.AreEqual((byte)0x0B, GetPropertyValue<byte>(items[1], "Id"));
        Assert.AreEqual((byte)0x0C, GetPropertyValue<byte>(items[2], "Id"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_MultipleTypeParameters_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var byteField = CreatePrimitiveField("Val", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var byteSchema = new BinarySchemaNode("ByteVal", [byteField]);
        registry.Register("ByteVal", byteSchema);

        var shortField = CreatePrimitiveField("Val", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var shortSchema = new BinarySchemaNode("ShortVal", [shortField]);
        registry.Register("ShortVal", shortSchema);

        var firstField = new FieldDefinitionNode("First", new SchemaReferenceTypeNode("T"));
        var secondField = new FieldDefinitionNode("Second", new SchemaReferenceTypeNode("U"));
        var genericSchema = new BinarySchemaNode("Pair", [firstField, secondField], null,
            ["T", "U"]);
        registry.Register("Pair", genericSchema);

        var dataField =
            new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("Pair", ["ByteVal", "ShortVal"]));
        var containerSchema = new BinarySchemaNode("Container", [dataField]);
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        var data = new byte[] { 0xAA, 0x34, 0x12 };

        var result = InvokeInterpret(interpreter, data);
        var pairData = GetPropertyValue<object>(result, "Data");
        var first = GetPropertyValue<object>(pairData, "First");
        var second = GetPropertyValue<object>(pairData, "Second");

        var firstVal = GetPropertyValue<byte>(first, "Val");
        var secondVal = GetPropertyValue<short>(second, "Val");

        Assert.AreEqual((byte)0xAA, firstVal);
        Assert.AreEqual((short)0x1234, secondVal);
    }

    [TestMethod]
    public void Interpret_GenericSchema_SameGenericWithDifferentTypes_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var byteField = CreatePrimitiveField("Value", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var byteRecordSchema = new BinarySchemaNode("ByteRecord", [byteField]);
        registry.Register("ByteRecord", byteRecordSchema);

        var intField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var intRecordSchema = new BinarySchemaNode("IntRecord", [intField]);
        registry.Register("IntRecord", intRecordSchema);

        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("T"));
        var wrapperSchema = new BinarySchemaNode("Wrapper", [dataField], null, ["T"]);
        registry.Register("Wrapper", wrapperSchema);

        var byteWrappedField =
            new FieldDefinitionNode("ByteWrapped", new SchemaReferenceTypeNode("Wrapper", ["ByteRecord"]));
        var intWrappedField =
            new FieldDefinitionNode("IntWrapped", new SchemaReferenceTypeNode("Wrapper", ["IntRecord"]));
        var containerSchema =
            new BinarySchemaNode("Container", [byteWrappedField, intWrappedField]);
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        var data = new byte[] { 0xAB, 0x78, 0x56, 0x34, 0x12 };

        var result = InvokeInterpret(interpreter, data);
        var byteWrapped = GetPropertyValue<object>(result, "ByteWrapped");
        var intWrapped = GetPropertyValue<object>(result, "IntWrapped");

        var byteData = GetPropertyValue<object>(byteWrapped, "Data");
        var intData = GetPropertyValue<object>(intWrapped, "Data");

        Assert.AreEqual((byte)0xAB, GetPropertyValue<byte>(byteData, "Value"));
        Assert.AreEqual(0x12345678, GetPropertyValue<int>(intData, "Value"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_NestedGenericInstantiation_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var byteField = CreatePrimitiveField("Val", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var byteValSchema = new BinarySchemaNode("ByteVal", [byteField]);
        registry.Register("ByteVal", byteValSchema);

        var shortField = CreatePrimitiveField("Val", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var shortValSchema = new BinarySchemaNode("ShortVal", [shortField]);
        registry.Register("ShortVal", shortValSchema);

        var hasValueField = CreatePrimitiveField("HasValue", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var hasValueRef = new IdentifierNode("HasValue");
        var zeroNode = new IntegerNode(0);
        var condition = new DiffNode(hasValueRef, zeroNode);
        var valueField = new FieldDefinitionNode("Value", new SchemaReferenceTypeNode("T"), null, null, condition);
        var optionalSchema = new BinarySchemaNode("Optional", [hasValueField, valueField], null,
            ["T"]);
        registry.Register("Optional", optionalSchema);

        var firstField = new FieldDefinitionNode("First", new SchemaReferenceTypeNode("T"));
        var secondField = new FieldDefinitionNode("Second", new SchemaReferenceTypeNode("U"));
        var pairSchema = new BinarySchemaNode("Pair", [firstField, secondField], null,
            ["T", "U"]);
        registry.Register("Pair", pairSchema);

        var pairField = new FieldDefinitionNode("Data",
            new SchemaReferenceTypeNode("Pair", ["Optional<ByteVal>", "Optional<ShortVal>"]));
        var containerSchema = new BinarySchemaNode("Container", [pairField]);
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        var data = new byte[]
        {
            0x01, 0xAA,
            0x01, 0x34, 0x12
        };

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
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var recordSchema = new BinarySchemaNode("Record", [valueField]);
        registry.Register("Record", recordSchema);

        var tagField = CreatePrimitiveField("Tag", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("T"));

        var tagRef = new IdentifierNode("Tag");
        var expectedTag = new IntegerNode(0xAA);
        var isValidExpr = new EqualityNode(tagRef, expectedTag);
        var isValidField = new ComputedFieldNode("IsValid", isValidExpr);

        var taggedSchema = new BinarySchemaNode("Tagged", [tagField, dataField, isValidField],
            null, ["T"]);
        registry.Register("Tagged", taggedSchema);

        var containerField = new FieldDefinitionNode("Item", new SchemaReferenceTypeNode("Tagged", ["Record"]));
        var containerSchema = new BinarySchemaNode("Container", [containerField]);
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        var data = new byte[] { 0xAA, 0x34, 0x12 };

        var result = InvokeInterpret(interpreter, data);
        var item = GetPropertyValue<object>(result, "Item");

        Assert.AreEqual((byte)0xAA, GetPropertyValue<byte>(item, "Tag"));
        var recordData = GetPropertyValue<object>(item, "Data");
        Assert.AreEqual((short)0x1234, GetPropertyValue<short>(recordData, "Value"));
        Assert.IsTrue(GetPropertyValue<bool>(item, "IsValid"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_WithFixedSizeArray_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Val", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemSchema = new BinarySchemaNode("Item", [valueField]);
        registry.Register("Item", itemSchema);

        var itemsField = new FieldDefinitionNode("Items", new ArrayTypeNode(
            new SchemaReferenceTypeNode("T"),
            new IntegerNode(3)));
        var bufferSchema =
            new BinarySchemaNode("FixedBuffer", [itemsField], null, ["T"]);
        registry.Register("FixedBuffer", bufferSchema);

        var bufferField =
            new FieldDefinitionNode("Buffer", new SchemaReferenceTypeNode("FixedBuffer", ["Item"]));
        var containerSchema = new BinarySchemaNode("Container", [bufferField]);
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        var data = new byte[] { 0x0A, 0x0B, 0x0C };

        var result = InvokeInterpret(interpreter, data);
        var buffer = GetPropertyValue<object>(result, "Buffer");
        var items = GetPropertyValue<object[]>(buffer, "Items");

        Assert.HasCount(3, items);
        Assert.AreEqual((byte)0x0A, GetPropertyValue<byte>(items[0], "Val"));
        Assert.AreEqual((byte)0x0B, GetPropertyValue<byte>(items[1], "Val"));
        Assert.AreEqual((byte)0x0C, GetPropertyValue<byte>(items[2], "Val"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_DirectInstantiation_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var recordSchema = new BinarySchemaNode("Record", [valueField]);
        registry.Register("Record", recordSchema);

        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("T"));
        var wrapperSchema = new BinarySchemaNode("Wrapper", [dataField], null, ["T"]);
        registry.Register("Wrapper", wrapperSchema);

        var interpreter = CompileInterpreterForGenericInstantiation(registry, "Wrapper", ["Record"]);

        var data = new byte[] { 0xEF, 0xBE, 0xAD, 0xDE };

        var result = InvokeInterpret(interpreter, data);
        var recordData = GetPropertyValue<object>(result, "Data");

        Assert.AreEqual(unchecked((int)0xDEADBEEF), GetPropertyValue<int>(recordData, "Value"));
    }

    [TestMethod]
    public void Interpret_GenericSchema_EmptyArray_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Int, Endianness.LittleEndian);
        var itemSchema = new BinarySchemaNode("Item", [valueField]);
        registry.Register("Item", itemSchema);

        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemsField = new FieldDefinitionNode("Items", new ArrayTypeNode(
            new SchemaReferenceTypeNode("T"),
            new IdentifierNode("Count")));
        var containerSchema = new BinarySchemaNode("Container", [countField, itemsField], null,
            ["T"]);
        registry.Register("Container", containerSchema);

        var wrapperField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("Container", ["Item"]));
        var wrapperSchema = new BinarySchemaNode("Wrapper", [wrapperField]);
        registry.Register("Wrapper", wrapperSchema);

        var interpreter = CompileInterpreter(registry, "Wrapper");

        var data = new byte[] { 0x00 };

        var result = InvokeInterpret(interpreter, data);
        var container = GetPropertyValue<object>(result, "Data");
        var count = GetPropertyValue<byte>(container, "Count");
        var items = GetPropertyValue<object[]>(container, "Items");

        Assert.AreEqual((byte)0, count);
        Assert.IsEmpty(items);
    }

    [TestMethod]
    public void Interpret_GenericSchema_LargeArray_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var idField = CreatePrimitiveField("Id", PrimitiveTypeName.Byte, Endianness.NotApplicable);
        var itemSchema = new BinarySchemaNode("Item", [idField]);
        registry.Register("Item", itemSchema);

        var countField = CreatePrimitiveField("Count", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var itemsField = new FieldDefinitionNode("Items", new ArrayTypeNode(
            new SchemaReferenceTypeNode("T"),
            new IdentifierNode("Count")));
        var bufferSchema = new BinarySchemaNode("Buffer", [countField, itemsField], null,
            ["T"]);
        registry.Register("Buffer", bufferSchema);

        var wrapperField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("Buffer", ["Item"]));
        var wrapperSchema = new BinarySchemaNode("Wrapper", [wrapperField]);
        registry.Register("Wrapper", wrapperSchema);

        var interpreter = CompileInterpreter(registry, "Wrapper");

        const int itemCount = 100;
        var data = new byte[2 + itemCount];
        data[0] = itemCount & 0xFF;
        data[1] = (itemCount >> 8) & 0xFF;
        for (var i = 0; i < itemCount; i++) data[2 + i] = (byte)i;

        var result = InvokeInterpret(interpreter, data);
        var buffer = GetPropertyValue<object>(result, "Data");
        var count = GetPropertyValue<short>(buffer, "Count");
        var items = GetPropertyValue<object[]>(buffer, "Items");

        Assert.AreEqual((short)itemCount, count);
        Assert.HasCount(itemCount, items);
        for (var i = 0; i < itemCount; i++)
            Assert.AreEqual((byte)i, GetPropertyValue<byte>(items[i], "Id"), $"Item {i} has wrong Id");
    }

    [TestMethod]
    public void Interpret_GenericSchema_TypeParameterInMultipleFields_ShouldWork()
    {
        var registry = new SchemaRegistry();

        var valueField = CreatePrimitiveField("Value", PrimitiveTypeName.Short, Endianness.LittleEndian);
        var markerSchema = new BinarySchemaNode("Marker", [valueField]);
        registry.Register("Marker", markerSchema);

        var headerField = new FieldDefinitionNode("Header", new SchemaReferenceTypeNode("T"));
        var footerField = new FieldDefinitionNode("Footer", new SchemaReferenceTypeNode("T"));
        var bracketedSchema = new BinarySchemaNode("Bracketed", [headerField, footerField],
            null, ["T"]);
        registry.Register("Bracketed", bracketedSchema);

        var dataField = new FieldDefinitionNode("Data", new SchemaReferenceTypeNode("Bracketed", ["Marker"]));
        var containerSchema = new BinarySchemaNode("Container", [dataField]);
        registry.Register("Container", containerSchema);

        var interpreter = CompileInterpreter(registry, "Container");

        var data = new byte[] { 0x11, 0x11, 0x22, 0x22 };

        var result = InvokeInterpret(interpreter, data);
        var bracketed = GetPropertyValue<object>(result, "Data");
        var header = GetPropertyValue<object>(bracketed, "Header");
        var footer = GetPropertyValue<object>(bracketed, "Footer");

        Assert.AreEqual((short)0x1111, GetPropertyValue<short>(header, "Value"));
        Assert.AreEqual((short)0x2222, GetPropertyValue<short>(footer, "Value"));
    }

    #endregion
}
