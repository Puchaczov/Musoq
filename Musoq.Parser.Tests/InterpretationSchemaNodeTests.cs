using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public class InterpretationSchemaNodeTests
{
    #region SchemaReferenceTypeNode Tests

    [TestMethod]
    public void SchemaReferenceTypeNode_ShouldHaveCorrectProperties()
    {
        var node = new SchemaReferenceTypeNode("Header");

        Assert.AreEqual("Header", node.SchemaName);
        Assert.AreEqual(typeof(object), node.ClrType);
        Assert.IsFalse(node.IsFixedSize);
        Assert.AreEqual("Header", node.ToString());
    }

    #endregion

    #region AlignmentNode Tests

    [TestMethod]
    public void AlignmentNode_ByteAlignment_ShouldHaveCorrectProperties()
    {
        var node = new AlignmentNode(8);

        Assert.AreEqual(8, node.AlignmentBits);
        Assert.AreEqual(typeof(void), node.ClrType);
        Assert.AreEqual("align[8]", node.ToString());
    }

    #endregion

    #region PrimitiveTypeNode Tests

    [TestMethod]
    public void PrimitiveTypeNode_ByteType_ShouldHaveCorrectProperties()
    {
        var node = new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable);

        Assert.AreEqual(PrimitiveTypeName.Byte, node.TypeName);
        Assert.AreEqual(Endianness.NotApplicable, node.Endianness);
        Assert.AreEqual(typeof(byte), node.ClrType);
        Assert.AreEqual(1, node.FixedSizeBytes);
        Assert.IsTrue(node.IsFixedSize);
        Assert.AreEqual("byte", node.ToString());
    }

    [TestMethod]
    public void PrimitiveTypeNode_IntLittleEndian_ShouldHaveCorrectProperties()
    {
        var node = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);

        Assert.AreEqual(PrimitiveTypeName.Int, node.TypeName);
        Assert.AreEqual(Endianness.LittleEndian, node.Endianness);
        Assert.AreEqual(typeof(int), node.ClrType);
        Assert.AreEqual(4, node.FixedSizeBytes);
        Assert.AreEqual("int le", node.ToString());
    }

    [TestMethod]
    public void PrimitiveTypeNode_DoubleBigEndian_ShouldHaveCorrectProperties()
    {
        var node = new PrimitiveTypeNode(PrimitiveTypeName.Double, Endianness.BigEndian);

        Assert.AreEqual(typeof(double), node.ClrType);
        Assert.AreEqual(8, node.FixedSizeBytes);
        Assert.AreEqual("double be", node.ToString());
    }

    #endregion

    #region ByteArrayTypeNode Tests

    [TestMethod]
    public void ByteArrayTypeNode_FixedSize_ShouldHaveCorrectProperties()
    {
        var sizeNode = new IntegerNode("16", "s");
        var node = new ByteArrayTypeNode(sizeNode);

        Assert.AreEqual(typeof(byte[]), node.ClrType);
        Assert.IsTrue(node.IsFixedSize);
        Assert.AreEqual(16, node.FixedSizeBytes);
        Assert.AreEqual("byte[16]", node.ToString());
    }

    [TestMethod]
    public void ByteArrayTypeNode_DynamicSize_ShouldNotBeFixedSize()
    {
        var sizeNode = new IdentifierNode("Length");
        var node = new ByteArrayTypeNode(sizeNode);

        Assert.IsFalse(node.IsFixedSize);
        Assert.IsNull(node.FixedSizeBytes);
        Assert.AreEqual("byte[Length]", node.ToString());
    }

    #endregion

    #region StringTypeNode Tests

    [TestMethod]
    public void StringTypeNode_Utf8_ShouldHaveCorrectProperties()
    {
        var sizeNode = new IntegerNode("32", "s");
        var node = new StringTypeNode(sizeNode, StringEncoding.Utf8);

        Assert.AreEqual(typeof(string), node.ClrType);
        Assert.AreEqual(StringEncoding.Utf8, node.Encoding);
        Assert.AreEqual(StringModifier.None, node.Modifiers);
        Assert.AreEqual("string[32] utf8", node.ToString());
    }

    [TestMethod]
    public void StringTypeNode_AsciiWithTrim_ShouldHaveCorrectProperties()
    {
        var sizeNode = new IntegerNode("8", "s");
        var node = new StringTypeNode(sizeNode, StringEncoding.Ascii, StringModifier.Trim);

        Assert.AreEqual(StringEncoding.Ascii, node.Encoding);
        Assert.AreEqual(StringModifier.Trim, node.Modifiers);
        Assert.AreEqual("string[8] ascii trim", node.ToString());
    }

    [TestMethod]
    public void StringTypeNode_WithMultipleModifiers_ShouldFormatCorrectly()
    {
        var sizeNode = new IntegerNode("64", "s");
        var node = new StringTypeNode(sizeNode, StringEncoding.Utf8, StringModifier.NullTerm | StringModifier.RTrim);

        Assert.Contains("nullterm", node.ToString());
        Assert.Contains("rtrim", node.ToString());
    }

    #endregion

    #region BitsTypeNode Tests

    [TestMethod]
    public void BitsTypeNode_SingleBit_ShouldReturnByte()
    {
        var node = new BitsTypeNode(1);

        Assert.AreEqual(1, node.BitCount);
        Assert.AreEqual(typeof(byte), node.ClrType);
        Assert.AreEqual("bits[1]", node.ToString());
    }

    [TestMethod]
    public void BitsTypeNode_SixteenBits_ShouldReturnUShort()
    {
        var node = new BitsTypeNode(16);

        Assert.AreEqual(typeof(ushort), node.ClrType);
    }

    [TestMethod]
    public void BitsTypeNode_ThirtyTwoBits_ShouldReturnUInt()
    {
        var node = new BitsTypeNode(32);

        Assert.AreEqual(typeof(uint), node.ClrType);
    }

    [TestMethod]
    public void BitsTypeNode_SixtyFourBits_ShouldReturnULong()
    {
        var node = new BitsTypeNode(64);

        Assert.AreEqual(typeof(ulong), node.ClrType);
    }

    #endregion

    #region FieldDefinitionNode Tests

    [TestMethod]
    public void FieldDefinitionNode_Simple_ShouldHaveCorrectProperties()
    {
        var typeNode = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var node = new FieldDefinitionNode("Magic", typeNode);

        Assert.AreEqual("Magic", node.Name);
        Assert.AreSame(typeNode, node.TypeAnnotation);
        Assert.IsNull(node.Constraint);
        Assert.IsNull(node.AtOffset);
        Assert.AreEqual("Magic: int le", node.ToString());
    }

    [TestMethod]
    public void FieldDefinitionNode_WithAtOffset_ShouldIncludeInString()
    {
        var typeNode = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var offsetNode = new IntegerNode("60", "s");
        var node = new FieldDefinitionNode("PeOffset", typeNode, atOffset: offsetNode);

        Assert.AreSame(offsetNode, node.AtOffset);
        Assert.Contains("at 60", node.ToString());
    }

    [TestMethod]
    public void FieldDefinitionNode_WithConstraint_ShouldIncludeInString()
    {
        var typeNode = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var constraintExpr = new IntegerNode("1234", "s");
        var constraint = new FieldConstraintNode(constraintExpr);
        var node = new FieldDefinitionNode("Magic", typeNode, constraint);

        Assert.AreSame(constraint, node.Constraint);
        Assert.Contains("check", node.ToString());
    }

    #endregion

    #region BinarySchemaNode Tests

    [TestMethod]
    public void BinarySchemaNode_Empty_ShouldHaveCorrectProperties()
    {
        var node = new BinarySchemaNode("Empty", []);

        Assert.AreEqual("Empty", node.Name);
        Assert.IsEmpty(node.Fields);
        Assert.IsNull(node.Extends);
        Assert.Contains("binary Empty", node.ToString());
    }

    [TestMethod]
    public void BinarySchemaNode_WithFields_ShouldHaveCorrectProperties()
    {
        var field1 = new FieldDefinitionNode("Magic",
            new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian));
        var field2 = new FieldDefinitionNode("Version",
            new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian));

        var node = new BinarySchemaNode("Header", [field1, field2]);

        Assert.AreEqual("Header", node.Name);
        Assert.HasCount(2, node.Fields);
        Assert.AreEqual("Magic", node.Fields[0].Name);
        Assert.AreEqual("Version", node.Fields[1].Name);
    }

    [TestMethod]
    public void BinarySchemaNode_WithExtends_ShouldIncludeInheritance()
    {
        var field = new FieldDefinitionNode("Extra",
            new PrimitiveTypeNode(PrimitiveTypeName.Long, Endianness.LittleEndian));

        var node = new BinarySchemaNode("ExtendedHeader", [field], "BaseHeader");

        Assert.AreEqual("BaseHeader", node.Extends);
        Assert.Contains("extends BaseHeader", node.ToString());
    }

    [TestMethod]
    public void BinarySchemaNode_ToString_ShouldFormatCorrectly()
    {
        var field1 = new FieldDefinitionNode("Magic",
            new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian));
        var field2 = new FieldDefinitionNode("Version",
            new PrimitiveTypeNode(PrimitiveTypeName.Short, Endianness.LittleEndian));

        var node = new BinarySchemaNode("Header", [field1, field2]);
        var result = node.ToString();

        Assert.Contains("binary Header {", result);
        Assert.Contains("Magic: int le", result);
        Assert.Contains("Version: short le", result);
        Assert.Contains("}", result);
    }

    #endregion

    #region TextFieldDefinitionNode Tests

    [TestMethod]
    public void TextFieldDefinitionNode_Pattern_ShouldHaveCorrectProperties()
    {
        var node = new TextFieldDefinitionNode("Ip", TextFieldType.Pattern, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");

        Assert.AreEqual("Ip", node.Name);
        Assert.AreEqual(TextFieldType.Pattern, node.FieldType);
        Assert.AreEqual(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}", node.PrimaryValue);
        Assert.IsFalse(node.IsDiscard);
    }

    [TestMethod]
    public void TextFieldDefinitionNode_Literal_ShouldHaveCorrectProperties()
    {
        var node = new TextFieldDefinitionNode("_", TextFieldType.Literal, " ");

        Assert.IsTrue(node.IsDiscard);
        Assert.AreEqual(TextFieldType.Literal, node.FieldType);
    }

    [TestMethod]
    public void TextFieldDefinitionNode_Until_ShouldHaveCorrectProperties()
    {
        var node = new TextFieldDefinitionNode("Key", TextFieldType.Until, ":");

        Assert.AreEqual(TextFieldType.Until, node.FieldType);
        Assert.AreEqual(":", node.PrimaryValue);
        Assert.Contains("until ':'", node.ToString());
    }

    [TestMethod]
    public void TextFieldDefinitionNode_Between_ShouldHaveCorrectProperties()
    {
        var node = new TextFieldDefinitionNode("Quoted", TextFieldType.Between, "[", "]");

        Assert.AreEqual(TextFieldType.Between, node.FieldType);
        Assert.AreEqual("[", node.PrimaryValue);
        Assert.AreEqual("]", node.SecondaryValue);
        Assert.Contains("between '[' ']'", node.ToString());
    }

    [TestMethod]
    public void TextFieldDefinitionNode_CharsWithTrim_ShouldHaveCorrectProperties()
    {
        var node = new TextFieldDefinitionNode("Name", TextFieldType.Chars, "30", modifiers: TextFieldModifier.Trim);

        Assert.AreEqual(TextFieldType.Chars, node.FieldType);
        Assert.AreEqual("30", node.PrimaryValue);
        Assert.AreEqual(TextFieldModifier.Trim, node.Modifiers);
        Assert.Contains("chars[30]", node.ToString());
        Assert.Contains("trim", node.ToString());
    }

    [TestMethod]
    public void TextFieldDefinitionNode_Token_ShouldHaveCorrectProperties()
    {
        var node = new TextFieldDefinitionNode("Word", TextFieldType.Token);

        Assert.AreEqual(TextFieldType.Token, node.FieldType);
        Assert.AreEqual("Word: token", node.ToString());
    }

    [TestMethod]
    public void TextFieldDefinitionNode_Rest_ShouldHaveCorrectProperties()
    {
        var node = new TextFieldDefinitionNode("Message", TextFieldType.Rest);

        Assert.AreEqual(TextFieldType.Rest, node.FieldType);
        Assert.AreEqual("Message: rest", node.ToString());
    }

    [TestMethod]
    public void TextFieldDefinitionNode_WithCaptureGroups_ShouldIncludeInString()
    {
        var node = new TextFieldDefinitionNode(
            "Coords",
            TextFieldType.Pattern,
            @"(?<Lat>-?\d+\.\d+),(?<Lon>-?\d+\.\d+)",
            captureGroups: ["Lat", "Lon"]);

        Assert.HasCount(2, node.CaptureGroups);
        Assert.Contains("capture (Lat, Lon)", node.ToString());
    }

    #endregion

    #region TextSchemaNode Tests

    [TestMethod]
    public void TextSchemaNode_Simple_ShouldHaveCorrectProperties()
    {
        var field1 = new TextFieldDefinitionNode("Key", TextFieldType.Until, ":");
        var field2 = new TextFieldDefinitionNode("_", TextFieldType.Literal, ": ");
        var field3 = new TextFieldDefinitionNode("Value", TextFieldType.Rest);

        var node = new TextSchemaNode("KeyValue", [field1, field2, field3]);

        Assert.AreEqual("KeyValue", node.Name);
        Assert.HasCount(3, node.Fields);
        Assert.IsNull(node.Extends);
    }

    [TestMethod]
    public void TextSchemaNode_ToString_ShouldFormatCorrectly()
    {
        var field1 = new TextFieldDefinitionNode("Timestamp", TextFieldType.Between, "[", "]");
        var field2 = new TextFieldDefinitionNode("Level", TextFieldType.Token);
        var field3 = new TextFieldDefinitionNode("Message", TextFieldType.Rest);

        var node = new TextSchemaNode("LogLine", [field1, field2, field3]);
        var result = node.ToString();

        Assert.Contains("text LogLine {", result);
        Assert.Contains("Timestamp:", result);
        Assert.Contains("Level:", result);
        Assert.Contains("Message:", result);
    }

    #endregion

    #region ArrayTypeNode Tests

    [TestMethod]
    public void ArrayTypeNode_PrimitiveArray_ShouldHaveCorrectProperties()
    {
        var elementType = new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian);
        var sizeNode = new IntegerNode("10", "s");
        var node = new ArrayTypeNode(elementType, sizeNode);

        Assert.AreEqual(typeof(int[]), node.ClrType);
        Assert.IsTrue(node.IsFixedSize);
        Assert.AreEqual(40, node.FixedSizeBytes);
        Assert.AreEqual("int le[10]", node.ToString());
    }

    [TestMethod]
    public void ArrayTypeNode_SchemaArray_ShouldHaveCorrectProperties()
    {
        var elementType = new SchemaReferenceTypeNode("Record");
        var sizeNode = new IdentifierNode("Count");
        var node = new ArrayTypeNode(elementType, sizeNode);

        Assert.IsFalse(node.IsFixedSize);
        Assert.IsNull(node.FixedSizeBytes);
        Assert.AreEqual("Record[Count]", node.ToString());
    }

    #endregion

    #region Visitor Tests

    [TestMethod]
    public void AllNodes_ShouldAcceptVisitor()
    {
        var visitor = new TestVisitor();


        new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian).Accept(visitor);
        new ByteArrayTypeNode(new IntegerNode("16", "s")).Accept(visitor);
        new StringTypeNode(new IntegerNode("32", "s"), StringEncoding.Utf8).Accept(visitor);
        new BitsTypeNode(4).Accept(visitor);
        new AlignmentNode(8).Accept(visitor);
        new SchemaReferenceTypeNode("Test").Accept(visitor);
        new ArrayTypeNode(new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable),
            new IntegerNode("10", "s")).Accept(visitor);
        new FieldConstraintNode(new IntegerNode("1", "s")).Accept(visitor);
        new FieldDefinitionNode("Test", new PrimitiveTypeNode(PrimitiveTypeName.Int, Endianness.LittleEndian))
            .Accept(visitor);
        new TextFieldDefinitionNode("Test", TextFieldType.Rest).Accept(visitor);
        new BinarySchemaNode("Test", []).Accept(visitor);
        new TextSchemaNode("Test", []).Accept(visitor);

        Assert.AreEqual(12, visitor.VisitCount);
    }

    private class TestVisitor : NoOpExpressionVisitor
    {
        public int VisitCount { get; private set; }

        public override void Visit(PrimitiveTypeNode node)
        {
            VisitCount++;
        }

        public override void Visit(ByteArrayTypeNode node)
        {
            VisitCount++;
        }

        public override void Visit(StringTypeNode node)
        {
            VisitCount++;
        }

        public override void Visit(BitsTypeNode node)
        {
            VisitCount++;
        }

        public override void Visit(AlignmentNode node)
        {
            VisitCount++;
        }

        public override void Visit(SchemaReferenceTypeNode node)
        {
            VisitCount++;
        }

        public override void Visit(ArrayTypeNode node)
        {
            VisitCount++;
        }

        public override void Visit(FieldConstraintNode node)
        {
            VisitCount++;
        }

        public override void Visit(FieldDefinitionNode node)
        {
            VisitCount++;
        }

        public override void Visit(TextFieldDefinitionNode node)
        {
            VisitCount++;
        }

        public override void Visit(BinarySchemaNode node)
        {
            VisitCount++;
        }

        public override void Visit(TextSchemaNode node)
        {
            VisitCount++;
        }
    }

    #endregion
}
