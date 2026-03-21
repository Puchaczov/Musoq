using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for the SchemaParser class - Binary schema parsing.
///     Note: Some tests involving array syntax (e.g., byte[16], string[32]) are limited
///     due to the standard Lexer interpreting Name[n] as NumericAccess tokens rather than
///     separate bracket and number tokens. Full array syntax support requires lexer modifications
///     or a dedicated schema lexer (planned for a future session).
/// </summary>
[TestClass]
public class SchemaParser_BinaryTests : SchemaParserTestsBase
{
    #region Binary Schema - Schema References

    [TestMethod]
    public void BinarySchema_NestedSchemaReference_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: OtherSchema }");
        Assert.IsInstanceOfType(result, typeof(SchemaReferenceTypeNode));
        var schemaRef = (SchemaReferenceTypeNode)result;
        Assert.AreEqual("OtherSchema", schemaRef.SchemaName);
    }

    #endregion

    #region Binary Schema - Basic Structure

    [TestMethod]
    public void BinarySchema_EmptySchema_ShouldParse()
    {
        var schema = "binary Empty { }";

        var result = ParseBinarySchema(schema);

        Assert.IsNotNull(result);
        Assert.AreEqual("Empty", result.Name);
        Assert.IsEmpty(result.Fields);
        Assert.IsNull(result.Extends);
    }

    [TestMethod]
    public void BinarySchema_WithExtends_ShouldParse()
    {
        var schema = "binary Derived extends Base { }";

        var result = ParseBinarySchema(schema);

        Assert.AreEqual("Derived", result.Name);
        Assert.AreEqual("Base", result.Extends);
    }

    [TestMethod]
    public void BinarySchema_SinglePrimitiveField_ShouldParse()
    {
        var schema = "binary Simple { Value: byte }";

        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field, "Expected FieldDefinitionNode");
        Assert.AreEqual("Value", field.Name);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(PrimitiveTypeNode));
        var primitiveType = (PrimitiveTypeNode)field.TypeAnnotation;
        Assert.AreEqual(PrimitiveTypeName.Byte, primitiveType.TypeName);
    }

    [TestMethod]
    public void BinarySchema_MultiplePrimitiveFields_ShouldParse()
    {
        var schema = @"binary Header {
            Magic: byte,
            Version: byte,
            Size: int le
        }";

        var result = ParseBinarySchema(schema);

        Assert.HasCount(3, result.Fields);
        Assert.AreEqual("Magic", result.Fields[0].Name);
        Assert.AreEqual("Version", result.Fields[1].Name);
        Assert.AreEqual("Size", result.Fields[2].Name);
    }

    #endregion

    #region Binary Schema - Primitive Types

    [TestMethod]
    public void BinarySchema_ByteType_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: byte }");
        Assert.IsInstanceOfType(result, typeof(PrimitiveTypeNode));
        var primitive = (PrimitiveTypeNode)result;
        Assert.AreEqual(PrimitiveTypeName.Byte, primitive.TypeName);
        Assert.AreEqual(Endianness.NotApplicable, primitive.Endianness);
    }

    [TestMethod]
    public void BinarySchema_SByteType_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: sbyte }");
        Assert.IsInstanceOfType(result, typeof(PrimitiveTypeNode));
        var primitive = (PrimitiveTypeNode)result;
        Assert.AreEqual(PrimitiveTypeName.SByte, primitive.TypeName);
    }

    [TestMethod]
    public void BinarySchema_ShortLittleEndian_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: short le }");
        Assert.IsInstanceOfType(result, typeof(PrimitiveTypeNode));
        var primitive = (PrimitiveTypeNode)result;
        Assert.AreEqual(PrimitiveTypeName.Short, primitive.TypeName);
        Assert.AreEqual(Endianness.LittleEndian, primitive.Endianness);
    }

    [TestMethod]
    public void BinarySchema_ShortBigEndian_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: short be }");
        Assert.IsInstanceOfType(result, typeof(PrimitiveTypeNode));
        var primitive = (PrimitiveTypeNode)result;
        Assert.AreEqual(PrimitiveTypeName.Short, primitive.TypeName);
        Assert.AreEqual(Endianness.BigEndian, primitive.Endianness);
    }

    [TestMethod]
    public void BinarySchema_IntLittleEndian_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: int le }");
        var primitive = (PrimitiveTypeNode)result;
        Assert.AreEqual(PrimitiveTypeName.Int, primitive.TypeName);
        Assert.AreEqual(Endianness.LittleEndian, primitive.Endianness);
    }

    [TestMethod]
    public void BinarySchema_UIntBigEndian_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: uint be }");
        var primitive = (PrimitiveTypeNode)result;
        Assert.AreEqual(PrimitiveTypeName.UInt, primitive.TypeName);
        Assert.AreEqual(Endianness.BigEndian, primitive.Endianness);
    }

    [TestMethod]
    public void BinarySchema_LongLittleEndian_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: long le }");
        var primitive = (PrimitiveTypeNode)result;
        Assert.AreEqual(PrimitiveTypeName.Long, primitive.TypeName);
    }

    [TestMethod]
    public void BinarySchema_FloatLittleEndian_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: float le }");
        var primitive = (PrimitiveTypeNode)result;
        Assert.AreEqual(PrimitiveTypeName.Float, primitive.TypeName);
    }

    [TestMethod]
    public void BinarySchema_DoubleBigEndian_ShouldParse()
    {
        var result = ParseFieldType("binary T { F: double be }");
        var primitive = (PrimitiveTypeNode)result;
        Assert.AreEqual(PrimitiveTypeName.Double, primitive.TypeName);
        Assert.AreEqual(Endianness.BigEndian, primitive.Endianness);
    }

    #endregion

    #region Binary Schema - Array Syntax (Context-Aware Tokenization)

    [TestMethod]
    public void BinarySchema_ByteArrayFixed_ShouldParse()
    {
        var schema = "binary T { Data: byte[16] }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.AreEqual("Data", field.Name);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(ByteArrayTypeNode));
    }

    [TestMethod]
    public void BinarySchema_StringArrayWithEncoding_ShouldParse()
    {
        var schema = "binary T { Name: string[32] utf8 }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.AreEqual("Name", field.Name);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(StringTypeNode));

        var stringType = (StringTypeNode)field.TypeAnnotation;
        Assert.AreEqual(StringEncoding.Utf8, stringType.Encoding);
    }

    [TestMethod]
    public void BinarySchema_BitsType_ShouldParse()
    {
        var schema = "binary T { Flags: bits[4] }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.AreEqual("Flags", field.Name);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(BitsTypeNode));
    }

    [TestMethod]
    public void BinarySchema_AlignType_ShouldParse()
    {
        var schema = "binary T { _: align[32] }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.AreEqual("_", field.Name);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(AlignmentNode));
    }

    [TestMethod]
    public void BinarySchema_MultipleArrayFields_ShouldParse()
    {
        var schema = @"binary Packet {
            Header: byte[4],
            Name: string[32] utf8,
            Flags: bits[8],
            _: align[8],
            Payload: byte[128]
        }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(5, result.Fields);
        Assert.AreEqual("Header", result.Fields[0].Name);
        Assert.AreEqual("Name", result.Fields[1].Name);
        Assert.AreEqual("Flags", result.Fields[2].Name);
        Assert.AreEqual("_", result.Fields[3].Name);
        Assert.AreEqual("Payload", result.Fields[4].Name);
    }

    [TestMethod]
    public void BinarySchema_PrimitiveArrayWithEndianness_ShouldParse()
    {
        var schema = "binary T { Values: int[5] le }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(ArrayTypeNode));

        var arrayType = (ArrayTypeNode)field.TypeAnnotation;
        Assert.IsInstanceOfType(arrayType.ElementType, typeof(PrimitiveTypeNode));
        var elementType = (PrimitiveTypeNode)arrayType.ElementType;
        Assert.AreEqual(PrimitiveTypeName.Int, elementType.TypeName);
        Assert.AreEqual(Endianness.LittleEndian, elementType.Endianness);
    }

    [TestMethod]
    public void BinarySchema_StringWithModifiers_ShouldParse()
    {
        var schema = "binary T { Name: string[64] utf8 nullterm trim }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(StringTypeNode));

        var stringType = (StringTypeNode)field.TypeAnnotation;
        Assert.AreEqual(StringEncoding.Utf8, stringType.Encoding);
        Assert.IsTrue(stringType.Modifiers.HasFlag(StringModifier.NullTerm));
        Assert.IsTrue(stringType.Modifiers.HasFlag(StringModifier.Trim));
    }

    [TestMethod]
    public void BinarySchema_SchemaReferenceArray_ShouldParse()
    {
        var schema = "binary T { Items: CustomType[10] }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(ArrayTypeNode));

        var arrayType = (ArrayTypeNode)field.TypeAnnotation;
        Assert.IsInstanceOfType(arrayType.ElementType, typeof(SchemaReferenceTypeNode));
        var elementType = (SchemaReferenceTypeNode)arrayType.ElementType;
        Assert.AreEqual("CustomType", elementType.SchemaName);
    }

    [TestMethod]
    public void BinarySchema_ByteArrayWithArithmeticSize_ShouldParse()
    {
        var schema = "binary T { Total: byte, HeaderSize: byte, Data: byte[Total - HeaderSize] }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(3, result.Fields);
        var dataField = result.Fields[2] as FieldDefinitionNode;
        Assert.IsNotNull(dataField);
        Assert.IsInstanceOfType(dataField.TypeAnnotation, typeof(ByteArrayTypeNode));

        var byteArrayType = (ByteArrayTypeNode)dataField.TypeAnnotation;

        Assert.IsInstanceOfType(byteArrayType.SizeExpression, typeof(HyphenNode));
        var hyphenNode = (HyphenNode)byteArrayType.SizeExpression;

        Assert.IsInstanceOfType(hyphenNode.Left, typeof(IdentifierNode));
        Assert.IsInstanceOfType(hyphenNode.Right, typeof(IdentifierNode));

        Assert.AreEqual("Total", ((IdentifierNode)hyphenNode.Left).Name);
        Assert.AreEqual("HeaderSize", ((IdentifierNode)hyphenNode.Right).Name);
    }

    #endregion

    #region Computed Fields Tests

    [TestMethod]
    public void BinarySchema_ComputedField_SimpleMultiplication_ShouldParse()
    {
        var schema = "binary Rectangle { Width: int le, Height: int le, Area: Width * Height }";

        var result = ParseBinarySchema(schema);

        Assert.HasCount(3, result.Fields);

        var widthField = result.Fields[0] as FieldDefinitionNode;
        var heightField = result.Fields[1] as FieldDefinitionNode;
        Assert.IsNotNull(widthField);
        Assert.IsNotNull(heightField);
        Assert.AreEqual("Width", widthField.Name);
        Assert.AreEqual("Height", heightField.Name);

        var areaField = result.Fields[2] as ComputedFieldNode;
        Assert.IsNotNull(areaField, $"Expected ComputedFieldNode but got {result.Fields[2].GetType().Name}");
        Assert.AreEqual("Area", areaField.Name);
        Assert.IsTrue(areaField.IsComputed);
    }

    [TestMethod]
    public void BinarySchema_ComputedField_SimpleAddition_ShouldParse()
    {
        var schema = "binary Data { A: int le, B: int le, Sum: A + B }";

        var result = ParseBinarySchema(schema);

        Assert.HasCount(3, result.Fields);
        var sumField = result.Fields[2] as ComputedFieldNode;
        Assert.IsNotNull(sumField, $"Expected ComputedFieldNode but got {result.Fields[2].GetType().Name}");
        Assert.AreEqual("Sum", sumField.Name);
    }

    [TestMethod]
    public void BinarySchema_ComputedField_Comparison_ShouldParse()
    {
        var schema = "binary Data { Value: int le, IsLarge: Value > 100 }";

        var result = ParseBinarySchema(schema);

        Assert.HasCount(2, result.Fields);
        var isLargeField = result.Fields[1] as ComputedFieldNode;
        Assert.IsNotNull(isLargeField);
        Assert.AreEqual("IsLarge", isLargeField.Name);
    }

    #endregion

    #region Binary Repeat Until Tests

    [TestMethod]
    public void BinarySchema_RepeatUntil_SimpleCondition_ShouldParse()
    {
        var schema = "binary Stream { Records: Record repeat until Count = 10 }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.AreEqual("Records", field.Name);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(RepeatUntilTypeNode));

        var repeatType = (RepeatUntilTypeNode)field.TypeAnnotation;
        Assert.IsInstanceOfType(repeatType.ElementType, typeof(SchemaReferenceTypeNode));
        Assert.AreEqual("Record", ((SchemaReferenceTypeNode)repeatType.ElementType).SchemaName);
        Assert.AreEqual("Records", repeatType.FieldName);
        Assert.IsNotNull(repeatType.Condition);
    }

    [TestMethod]
    public void BinarySchema_RepeatUntil_WithPrimitiveType_ShouldParse()
    {
        var schema = "binary Stream { Bytes: byte repeat until Done = 1 }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(RepeatUntilTypeNode));

        var repeatType = (RepeatUntilTypeNode)field.TypeAnnotation;
        Assert.IsInstanceOfType(repeatType.ElementType, typeof(PrimitiveTypeNode));
    }

    [TestMethod]
    public void BinarySchema_RepeatUntil_ClrType_ShouldBeArray()
    {
        var schema = "binary Stream { Records: Record repeat until Count = 0 }";
        var result = ParseBinarySchema(schema);

        var field = result.Fields[0] as FieldDefinitionNode;
        var repeatType = (RepeatUntilTypeNode)field!.TypeAnnotation;

        Assert.IsTrue(repeatType.ClrType.IsArray);
    }

    [TestMethod]
    public void BinarySchema_RepeatUntil_IsFixedSize_ShouldBeFalse()
    {
        var schema = "binary Stream { Records: Record repeat until Count = 0 }";
        var result = ParseBinarySchema(schema);

        var field = result.Fields[0] as FieldDefinitionNode;
        var repeatType = (RepeatUntilTypeNode)field!.TypeAnnotation;

        Assert.IsFalse(repeatType.IsFixedSize);
        Assert.IsNull(repeatType.FixedSizeBytes);
    }

    [TestMethod]
    public void BinarySchema_RepeatUntil_ToString_ShouldFormat()
    {
        var schema = "binary Stream { Items: Item repeat until Done = 1 }";
        var result = ParseBinarySchema(schema);

        var field = result.Fields[0] as FieldDefinitionNode;
        var repeatType = (RepeatUntilTypeNode)field!.TypeAnnotation;

        var str = repeatType.ToString();
        Assert.Contains("repeat until", str, "Should contain 'repeat until'");
    }

    #endregion

    #region Lexer Context Tests

    [TestMethod]
    public void ByteArrayType_SchemaContext_TokenizesAsSeparateTokens()
    {
        var lexer = new Lexer("byte[16]", true);
        lexer.IsSchemaContext = true;

        var tokens = new List<(TokenType Type, string Value)>();
        for (var i = 0; i < 6; i++)
        {
            var token = lexer.Next();
            tokens.Add((token.TokenType, token.Value));
            if (token.TokenType == TokenType.EndOfFile) break;
        }

        var tokenList = string.Join(", ", tokens.Select(t => $"{t.Type}({t.Value})"));

        Assert.AreEqual(TokenType.ByteType, tokens[0].Type, $"Token 0: {tokenList}");
        Assert.AreEqual(TokenType.LeftSquareBracket, tokens[1].Type, $"Token 1: {tokenList}");
        Assert.AreEqual(TokenType.Integer, tokens[2].Type, $"Token 2: {tokenList}");
        Assert.AreEqual(TokenType.RightSquareBracket, tokens[3].Type, $"Token 3: {tokenList}");
    }

    #endregion
}
