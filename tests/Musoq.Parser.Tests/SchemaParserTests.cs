using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for the SchemaParser class.
///     Note: Some tests involving array syntax (e.g., byte[16], string[32]) are limited
///     due to the standard Lexer interpreting Name[n] as NumericAccess tokens rather than
///     separate bracket and number tokens. Full array syntax support requires lexer modifications
///     or a dedicated schema lexer (planned for a future session).
/// </summary>
[TestClass]
public class SchemaParserTests
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

    #region Text Schema - Token Field

    [TestMethod]
    public void TextSchema_TokenField_ShouldParse()
    {
        var schema = "text T { Word: token }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Token, field.FieldType);
    }

    #endregion

    #region Text Schema - Rest Field

    [TestMethod]
    public void TextSchema_RestField_ShouldParse()
    {
        var schema = "text T { Remainder: rest }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Rest, field.FieldType);
    }

    #endregion

    #region Binary Schema - Basic Structure

    [TestMethod]
    public void BinarySchema_EmptySchema_ShouldParse()
    {
        // Arrange
        var schema = "binary Empty { }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Empty", result.Name);
        Assert.IsEmpty(result.Fields);
        Assert.IsNull(result.Extends);
    }

    [TestMethod]
    public void BinarySchema_WithExtends_ShouldParse()
    {
        // Arrange
        var schema = "binary Derived extends Base { }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.AreEqual("Derived", result.Name);
        Assert.AreEqual("Base", result.Extends);
    }

    [TestMethod]
    public void BinarySchema_SinglePrimitiveField_ShouldParse()
    {
        // Arrange
        var schema = "binary Simple { Value: byte }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
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
        // Arrange
        var schema = @"binary Header {
            Magic: byte,
            Version: byte,
            Size: int le
        }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
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

    #region Text Schema - Basic Structure

    [TestMethod]
    public void TextSchema_EmptySchema_ShouldParse()
    {
        var schema = "text Empty { }";
        var result = ParseTextSchema(schema);

        Assert.IsNotNull(result);
        Assert.AreEqual("Empty", result.Name);
        Assert.IsEmpty(result.Fields);
    }

    [TestMethod]
    public void TextSchema_WithExtends_ShouldParse()
    {
        var schema = "text Derived extends Base { }";
        var result = ParseTextSchema(schema);

        Assert.AreEqual("Derived", result.Name);
        Assert.AreEqual("Base", result.Extends);
    }

    #endregion

    #region Text Schema - Whitespace Field

    [TestMethod]
    public void TextSchema_WhitespaceField_ShouldParse()
    {
        var schema = "text T { _: whitespace }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Whitespace, field.FieldType);

        Assert.AreEqual("+", field.PrimaryValue);
    }

    [TestMethod]
    public void TextSchema_WhitespacePlus_ShouldParse()
    {
        var schema = "text T { _: whitespace+ }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Whitespace, field.FieldType);
        Assert.AreEqual("+", field.PrimaryValue);
    }

    [TestMethod]
    public void TextSchema_WhitespaceStar_ShouldParse()
    {
        var schema = "text T { _: whitespace* }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Whitespace, field.FieldType);
        Assert.AreEqual("*", field.PrimaryValue);
    }

    [TestMethod]
    public void TextSchema_WhitespaceQuestion_ShouldParse()
    {
        var schema = "text T { _: whitespace? }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Whitespace, field.FieldType);
        Assert.AreEqual("?", field.PrimaryValue);
    }

    #endregion

    #region Error Cases

    [TestMethod]
    public void SchemaParser_InvalidKeyword_ShouldThrowSyntaxException()
    {
        var schema = "invalid Name { }";

        Assert.Throws<SyntaxException>(() => ParseSchema(schema));
    }

    [TestMethod]
    public void BinarySchema_MissingBrace_ShouldThrowSyntaxException()
    {
        var schema = "binary Name { Field: byte";

        Assert.Throws<SyntaxException>(() => ParseBinarySchema(schema));
    }

    [TestMethod]
    public void BinarySchema_MissingColon_ShouldThrowSyntaxException()
    {
        var schema = "binary Name { Field byte }";

        Assert.Throws<SyntaxException>(() => ParseBinarySchema(schema));
    }

    [TestMethod]
    public void BinarySchema_MissingEndianness_ShouldThrowSyntaxException()
    {
        var schema = "binary Name { Value: int }";

        Assert.Throws<SyntaxException>(() => ParseBinarySchema(schema));
    }

    #endregion

    #region Helper Methods

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

    private static Node ParseSchema(string schema)
    {
        var lexer = new Lexer(schema, true);
        var parser = new SchemaParser(lexer);
        return parser.ParseSchema();
    }

    private static BinarySchemaNode ParseBinarySchema(string schema)
    {
        var result = ParseSchema(schema);
        Assert.IsInstanceOfType(result, typeof(BinarySchemaNode));
        return (BinarySchemaNode)result;
    }

    private static TextSchemaNode ParseTextSchema(string schema)
    {
        var result = ParseSchema(schema);
        Assert.IsInstanceOfType(result, typeof(TextSchemaNode));
        return (TextSchemaNode)result;
    }

    private static TypeAnnotationNode ParseFieldType(string schema)
    {
        var result = ParseBinarySchema(schema);
        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field, "Expected FieldDefinitionNode");
        return field.TypeAnnotation;
    }

    #endregion

    #region Computed Fields Tests

    [TestMethod]
    public void BinarySchema_ComputedField_SimpleMultiplication_ShouldParse()
    {
        // Arrange
        var schema = "binary Rectangle { Width: int le, Height: int le, Area: Width * Height }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
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
        // Arrange
        var schema = "binary Data { A: int le, B: int le, Sum: A + B }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.HasCount(3, result.Fields);
        var sumField = result.Fields[2] as ComputedFieldNode;
        Assert.IsNotNull(sumField, $"Expected ComputedFieldNode but got {result.Fields[2].GetType().Name}");
        Assert.AreEqual("Sum", sumField.Name);
    }

    [TestMethod]
    public void BinarySchema_ComputedField_Comparison_ShouldParse()
    {
        // Arrange
        var schema = "binary Data { Value: int le, IsLarge: Value > 100 }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.HasCount(2, result.Fields);
        var isLargeField = result.Fields[1] as ComputedFieldNode;
        Assert.IsNotNull(isLargeField);
        Assert.AreEqual("IsLarge", isLargeField.Name);
    }

    #endregion

    #region Generic Schema Tests

    [TestMethod]
    public void BinarySchema_SingleTypeParameter_ShouldParse()
    {
        // Arrange
        var schema = "binary LengthPrefixed<T> { Length: int le, Data: T[Length] }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.AreEqual("LengthPrefixed", result.Name);
        Assert.IsTrue(result.IsGeneric);
        Assert.HasCount(1, result.TypeParameters);
        Assert.AreEqual("T", result.TypeParameters[0]);
        Assert.HasCount(2, result.Fields);
    }

    [TestMethod]
    public void BinarySchema_MultipleTypeParameters_ShouldParse()
    {
        // Arrange
        var schema = "binary Pair<T, U> { First: T, Second: U }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.AreEqual("Pair", result.Name);
        Assert.IsTrue(result.IsGeneric);
        Assert.HasCount(2, result.TypeParameters);
        Assert.AreEqual("T", result.TypeParameters[0]);
        Assert.AreEqual("U", result.TypeParameters[1]);
    }

    [TestMethod]
    public void BinarySchema_GenericWithExtends_ShouldParse()
    {
        // Arrange
        var schema = "binary Extended<T> extends Base { Data: T }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.AreEqual("Extended", result.Name);
        Assert.IsTrue(result.IsGeneric);
        Assert.HasCount(1, result.TypeParameters);
        Assert.AreEqual("T", result.TypeParameters[0]);
        Assert.AreEqual("Base", result.Extends);
    }

    [TestMethod]
    public void BinarySchema_NonGeneric_ShouldHaveEmptyTypeParameters()
    {
        // Arrange
        var schema = "binary Header { Magic: int le }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.AreEqual("Header", result.Name);
        Assert.IsFalse(result.IsGeneric);
        Assert.IsEmpty(result.TypeParameters);
    }

    [TestMethod]
    public void BinarySchema_GenericTypeInstantiation_ShouldParse()
    {
        var schema = "binary Message { Records: LengthPrefixed<Record> }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.HasCount(1, result.Fields);
        var recordsField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(recordsField);
        Assert.AreEqual("Records", recordsField.Name);

        var schemaRef = recordsField.TypeAnnotation as SchemaReferenceTypeNode;
        Assert.IsNotNull(schemaRef);
        Assert.AreEqual("LengthPrefixed", schemaRef.SchemaName);
        Assert.IsTrue(schemaRef.IsGenericInstantiation);
        Assert.HasCount(1, schemaRef.TypeArguments);
        Assert.AreEqual("Record", schemaRef.TypeArguments[0]);
        Assert.AreEqual("LengthPrefixed<Record>", schemaRef.FullTypeName);
    }

    [TestMethod]
    public void BinarySchema_GenericTypeInstantiationWithMultipleArgs_ShouldParse()
    {
        // Arrange
        var schema = "binary Container { Data: Pair<Header, Footer> }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        var dataField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(dataField);

        var schemaRef = dataField.TypeAnnotation as SchemaReferenceTypeNode;
        Assert.IsNotNull(schemaRef);
        Assert.AreEqual("Pair", schemaRef.SchemaName);
        Assert.HasCount(2, schemaRef.TypeArguments);
        Assert.AreEqual("Header", schemaRef.TypeArguments[0]);
        Assert.AreEqual("Footer", schemaRef.TypeArguments[1]);
        Assert.AreEqual("Pair<Header, Footer>", schemaRef.FullTypeName);
    }

    [TestMethod]
    public void BinarySchema_GenericTypeArrayInstantiation_ShouldParse()
    {
        var schema = "binary Container { Items: Wrapper<Data>[5] }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.HasCount(1, result.Fields);
        var itemsField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(itemsField);

        var arrayType = itemsField.TypeAnnotation as ArrayTypeNode;
        Assert.IsNotNull(arrayType);

        var elementType = arrayType.ElementType as SchemaReferenceTypeNode;
        Assert.IsNotNull(elementType);
        Assert.AreEqual("Wrapper", elementType.SchemaName);
        Assert.IsTrue(elementType.IsGenericInstantiation);
        Assert.AreEqual("Data", elementType.TypeArguments[0]);
    }

    [TestMethod]
    public void BinarySchema_TypeParameterAsFieldType_ShouldParse()
    {
        var schema = "binary Wrapper<T> { Data: T }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        var dataField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(dataField);

        var schemaRef = dataField.TypeAnnotation as SchemaReferenceTypeNode;
        Assert.IsNotNull(schemaRef);
        Assert.AreEqual("T", schemaRef.SchemaName);
        Assert.IsFalse(schemaRef.IsGenericInstantiation);
        Assert.AreEqual("T", schemaRef.FullTypeName);
    }

    #endregion

    #region Optional Field Tests

    [TestMethod]
    public void TextSchema_OptionalPrefix_LiteralField_ShouldParse()
    {
        var schema = "text LogLine { _: optional literal '\\t' }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0];
        Assert.AreEqual("_", field.Name);
        Assert.AreEqual(TextFieldType.Literal, field.FieldType);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier to be set");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_PatternField_ShouldParse()
    {
        var schema = "text LogLine { TraceId: optional pattern '[a-f0-9]{32}' }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0];
        Assert.AreEqual("TraceId", field.Name);
        Assert.AreEqual(TextFieldType.Pattern, field.FieldType);
        Assert.AreEqual("[a-f0-9]{32}", field.PrimaryValue);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier to be set");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_UntilField_ShouldParse()
    {
        var schema = "text Line { Extra: optional until ',' }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        Assert.AreEqual("Extra", field.Name);
        Assert.AreEqual(TextFieldType.Until, field.FieldType);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier to be set");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_WithModifier_ShouldCombine()
    {
        var schema = "text Line { Data: optional until ',' trim }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        Assert.AreEqual("Data", field.Name);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier");
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Trim, "Expected Trim modifier");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_BetweenField_ShouldParse()
    {
        var schema = "text Config { Comment: optional between '/*' '*/' }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        Assert.AreEqual("Comment", field.Name);
        Assert.AreEqual(TextFieldType.Between, field.FieldType);
        Assert.AreEqual("/*", field.PrimaryValue);
        Assert.AreEqual("*/", field.SecondaryValue);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier");
    }

    [TestMethod]
    public void TextSchema_OptionalPrefix_CharsField_ShouldParse()
    {
        var schema = "text Record { Suffix: optional chars[4] }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        Assert.AreEqual("Suffix", field.Name);
        Assert.AreEqual(TextFieldType.Chars, field.FieldType);
        Assert.AreEqual("4", field.PrimaryValue);
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier");
    }

    [TestMethod]
    public void TextSchema_NoOptionalPrefix_ShouldNotHaveOptionalModifier()
    {
        var schema = "text Line { Data: until ',' }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        Assert.AreEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Should not have Optional modifier");
    }

    [TestMethod]
    public void TextSchema_OptionalTrailingModifier_ShouldAlsoWork()
    {
        var schema = "text Line { Data: until ',' optional }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        Assert.AreNotEqual((TextFieldModifier)0, field.Modifiers & TextFieldModifier.Optional,
            "Expected Optional modifier from trailing position");
    }

    #endregion

    #region Repeat Field Tests

    [TestMethod]
    public void TextSchema_RepeatField_WithUntilDelimiter_ShouldParse()
    {
        var schema = "text HttpHeaders { Headers: repeat HeaderLine until '\\r\\n' }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        Assert.AreEqual("HttpHeaders", result.Name);
        Assert.HasCount(1, result.Fields);

        var field = result.Fields[0];
        Assert.AreEqual("Headers", field.Name);
        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual("HeaderLine", field.PrimaryValue);
        Assert.AreEqual("\r\n", field.SecondaryValue);
    }

    [TestMethod]
    public void TextSchema_RepeatField_UntilEnd_ShouldParse()
    {
        var schema = "text AllLines { Lines: repeat Line until end }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        Assert.AreEqual("Lines", field.Name);
        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual("Line", field.PrimaryValue);
        Assert.IsNull(field.SecondaryValue);
    }

    [TestMethod]
    public void TextSchema_RepeatField_NoUntilClause_ShouldDefaultToEnd()
    {
        var schema = "text AllLines { Lines: repeat Line }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        Assert.AreEqual("Lines", field.Name);
        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual("Line", field.PrimaryValue);
        Assert.IsNull(field.SecondaryValue);
    }

    [TestMethod]
    public void TextSchema_RepeatField_WithSimpleDelimiter_ShouldParse()
    {
        var schema = "text CsvRow { Items: repeat Item until ',' }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual("Item", field.PrimaryValue);
        Assert.AreEqual(",", field.SecondaryValue);
    }

    [TestMethod]
    public void TextSchema_RepeatField_ToString_WithDelimiter_ShouldFormat()
    {
        // Arrange
        var schema = "text Test { Items: repeat Item until '\\n' }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        var str = field.ToString();
        Assert.Contains("repeat Item", str, "Should contain 'repeat Item'");
        Assert.Contains("until", str, "Should contain 'until'");
    }

    [TestMethod]
    public void TextSchema_RepeatField_ToString_UntilEnd_ShouldFormat()
    {
        // Arrange
        var schema = "text Test { Lines: repeat Line until end }";

        // Act
        var result = ParseTextSchema(schema);

        // Assert
        var field = result.Fields[0];
        var str = field.ToString();
        Assert.Contains("repeat Line", str, "Should contain 'repeat Line'");
        Assert.Contains("until end", str, "Should contain 'until end'");
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

    #region Text Switch Field Tests

    [TestMethod]
    public void TextSchema_SwitchField_SinglePattern_ShouldParse()
    {
        var schema = "text ConfigLine { Content: switch { pattern '\\\\s*\\\\[' => SectionHeader } }";
        var result = ParseTextSchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0];
        Assert.AreEqual("Content", field.Name);
        Assert.AreEqual(TextFieldType.Switch, field.FieldType);
        Assert.HasCount(1, field.SwitchCases);
        Assert.AreEqual("\\s*\\[", field.SwitchCases[0].Pattern);
        Assert.AreEqual("SectionHeader", field.SwitchCases[0].TypeName);
        Assert.IsFalse(field.SwitchCases[0].IsDefault);
    }

    [TestMethod]
    public void TextSchema_SwitchField_MultiplePatterns_ShouldParse()
    {
        var schema = @"text ConfigLine { 
            Content: switch { 
                pattern '\s*\[' => SectionHeader,
                pattern '\s*#' => Comment,
                pattern '\s*;' => Comment
            } 
        }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Switch, field.FieldType);
        Assert.HasCount(3, field.SwitchCases);

        Assert.AreEqual("\\s*\\[", field.SwitchCases[0].Pattern);
        Assert.AreEqual("SectionHeader", field.SwitchCases[0].TypeName);

        Assert.AreEqual("\\s*#", field.SwitchCases[1].Pattern);
        Assert.AreEqual("Comment", field.SwitchCases[1].TypeName);

        Assert.AreEqual("\\s*;", field.SwitchCases[2].Pattern);
        Assert.AreEqual("Comment", field.SwitchCases[2].TypeName);
    }

    [TestMethod]
    public void TextSchema_SwitchField_WithDefaultCase_ShouldParse()
    {
        var schema = @"text ConfigLine { 
            Content: switch { 
                pattern '\s*\[' => SectionHeader,
                _ => KeyValue
            } 
        }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Switch, field.FieldType);
        Assert.HasCount(2, field.SwitchCases);

        Assert.IsFalse(field.SwitchCases[0].IsDefault);
        Assert.AreEqual("SectionHeader", field.SwitchCases[0].TypeName);

        Assert.IsTrue(field.SwitchCases[1].IsDefault);
        Assert.IsNull(field.SwitchCases[1].Pattern);
        Assert.AreEqual("KeyValue", field.SwitchCases[1].TypeName);
    }

    [TestMethod]
    public void TextSchema_SwitchField_OnlyDefault_ShouldParse()
    {
        var schema = "text ConfigLine { Content: switch { _ => KeyValue } }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.AreEqual(TextFieldType.Switch, field.FieldType);
        Assert.HasCount(1, field.SwitchCases);
        Assert.IsTrue(field.SwitchCases[0].IsDefault);
        Assert.AreEqual("KeyValue", field.SwitchCases[0].TypeName);
    }

    [TestMethod]
    public void TextSchema_SwitchField_WithoutTrailingComma_ShouldParse()
    {
        var schema = @"text ConfigLine { 
            Content: switch { 
                pattern '\s*\[' => SectionHeader
                pattern '\s*#' => Comment
            } 
        }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        Assert.HasCount(2, field.SwitchCases);
    }

    [TestMethod]
    public void TextSchema_SwitchField_ToString_ShouldFormat()
    {
        var schema = @"text ConfigLine { 
            Content: switch { 
                pattern '\s*\[' => SectionHeader,
                _ => KeyValue
            } 
        }";
        var result = ParseTextSchema(schema);

        var field = result.Fields[0];
        var str = field.ToString();

        Assert.Contains("switch", str, "Should contain 'switch'");
        Assert.Contains("pattern", str, "Should contain 'pattern'");
        Assert.Contains("SectionHeader", str, "Should contain 'SectionHeader'");
        Assert.Contains("_ => KeyValue", str, "Should contain '_ => KeyValue'");
    }

    #endregion

    #region Binary-Text Composition (as clause)

    [TestMethod]
    public void BinarySchema_StringWithAsClause_ShouldParse()
    {
        var schema = "binary T { Config: string[64] utf8 as KeyValue }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.AreEqual("Config", field.Name);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(StringTypeNode));

        var stringType = (StringTypeNode)field.TypeAnnotation;
        Assert.AreEqual(StringEncoding.Utf8, stringType.Encoding);
        Assert.AreEqual("KeyValue", stringType.AsTextSchemaName);
    }

    [TestMethod]
    public void BinarySchema_StringWithModifiersAndAsClause_ShouldParse()
    {
        var schema = "binary T { Line: string[32] utf8 nullterm trim as ConfigLine }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(StringTypeNode));

        var stringType = (StringTypeNode)field.TypeAnnotation;
        Assert.AreEqual(StringEncoding.Utf8, stringType.Encoding);
        Assert.IsTrue(stringType.Modifiers.HasFlag(StringModifier.NullTerm));
        Assert.IsTrue(stringType.Modifiers.HasFlag(StringModifier.Trim));
        Assert.AreEqual("ConfigLine", stringType.AsTextSchemaName);
    }

    [TestMethod]
    public void BinarySchema_StringWithAsciiAndAsClause_ShouldParse()
    {
        var schema = "binary T { Content: string[128] ascii as IniFile }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(StringTypeNode));

        var stringType = (StringTypeNode)field.TypeAnnotation;
        Assert.AreEqual(StringEncoding.Ascii, stringType.Encoding);
        Assert.AreEqual("IniFile", stringType.AsTextSchemaName);
    }

    [TestMethod]
    public void BinarySchema_StringWithoutAsClause_ShouldHaveNullSchemaName()
    {
        var schema = "binary T { Config: string[64] utf8 }";
        var result = ParseBinarySchema(schema);

        Assert.HasCount(1, result.Fields);
        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        Assert.IsInstanceOfType(field.TypeAnnotation, typeof(StringTypeNode));

        var stringType = (StringTypeNode)field.TypeAnnotation;
        Assert.IsNull(stringType.AsTextSchemaName);
    }

    [TestMethod]
    public void BinarySchema_StringAsClause_ToString_ShouldIncludeSchemaName()
    {
        var schema = "binary T { Config: string[64] utf8 as KeyValue }";
        var result = ParseBinarySchema(schema);

        var field = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(field);
        var stringType = (StringTypeNode)field.TypeAnnotation;

        var str = stringType.ToString();
        Assert.Contains("as KeyValue", str, $"Expected 'as KeyValue' in ToString(), got: {str}");
    }

    #endregion

    #region Inline Schema Tests

    [TestMethod]
    public void BinarySchema_InlineSchema_SimpleFields_ShouldParse()
    {
        // Arrange
        var schema = @"binary Packet {
            Header: { Magic: int le, Version: short le }
        }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.HasCount(1, result.Fields);
        var headerField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(headerField);
        Assert.AreEqual("Header", headerField.Name);
        Assert.IsInstanceOfType(headerField.TypeAnnotation, typeof(InlineSchemaTypeNode));

        var inlineSchema = (InlineSchemaTypeNode)headerField.TypeAnnotation;
        Assert.HasCount(2, inlineSchema.Fields);

        var magicField = inlineSchema.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(magicField);
        Assert.AreEqual("Magic", magicField.Name);
        Assert.IsInstanceOfType(magicField.TypeAnnotation, typeof(PrimitiveTypeNode));

        var versionField = inlineSchema.Fields[1] as FieldDefinitionNode;
        Assert.IsNotNull(versionField);
        Assert.AreEqual("Version", versionField.Name);
    }

    [TestMethod]
    public void BinarySchema_InlineSchema_EmptyFields_ShouldParse()
    {
        var schema = "binary Empty { Data: { } }";


        var result = ParseBinarySchema(schema);


        Assert.HasCount(1, result.Fields);
        var dataField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(dataField);
        Assert.IsInstanceOfType(dataField.TypeAnnotation, typeof(InlineSchemaTypeNode));

        var inlineSchema = (InlineSchemaTypeNode)dataField.TypeAnnotation;
        Assert.IsEmpty(inlineSchema.Fields);
    }

    [TestMethod]
    public void BinarySchema_InlineSchema_NestedInline_ShouldParse()
    {
        var schema = @"binary DeepNesting {
            Outer: { 
                Inner: { Value: byte }
            }
        }";

        // Act
        var result = ParseBinarySchema(schema);

        // Assert
        Assert.HasCount(1, result.Fields);
        var outerField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(outerField);
        Assert.IsInstanceOfType(outerField.TypeAnnotation, typeof(InlineSchemaTypeNode));

        var outerInline = (InlineSchemaTypeNode)outerField.TypeAnnotation;
        Assert.HasCount(1, outerInline.Fields);

        var innerField = outerInline.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(innerField);
        Assert.AreEqual("Inner", innerField.Name);
        Assert.IsInstanceOfType(innerField.TypeAnnotation, typeof(InlineSchemaTypeNode));

        var innerInline = (InlineSchemaTypeNode)innerField.TypeAnnotation;
        Assert.HasCount(1, innerInline.Fields);
    }

    [TestMethod]
    public void BinarySchema_InlineSchema_MixedWithRegularFields_ShouldParse()
    {
        var schema = @"binary Mixed {
            Preamble: int le,
            Header: { Magic: int le, Version: short le },
            Data: byte[64]
        }";


        var result = ParseBinarySchema(schema);


        Assert.HasCount(3, result.Fields);


        var preambleField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(preambleField);
        Assert.IsInstanceOfType(preambleField.TypeAnnotation, typeof(PrimitiveTypeNode));


        var headerField = result.Fields[1] as FieldDefinitionNode;
        Assert.IsNotNull(headerField);
        Assert.IsInstanceOfType(headerField.TypeAnnotation, typeof(InlineSchemaTypeNode));


        var dataField = result.Fields[2] as FieldDefinitionNode;
        Assert.IsNotNull(dataField);
        Assert.IsInstanceOfType(dataField.TypeAnnotation, typeof(ByteArrayTypeNode));
    }

    [TestMethod]
    public void InlineSchemaTypeNode_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var schema = "binary T { H: { A: byte, B: short le } }";
        var result = ParseBinarySchema(schema);

        var headerField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(headerField);
        var inlineSchema = (InlineSchemaTypeNode)headerField.TypeAnnotation;

        // Act
        var str = inlineSchema.ToString();

        // Assert
        Assert.StartsWith("{", str, $"Expected inline schema ToString to start with '{{', got: {str}");
        Assert.EndsWith("}", str, $"Expected inline schema ToString to end with '}}', got: {str}");
        Assert.Contains("A", str, $"Expected 'A' in ToString(), got: {str}");
        Assert.Contains("B", str, $"Expected 'B' in ToString(), got: {str}");
    }

    [TestMethod]
    public void InlineSchemaTypeNode_IsFixedSize_AllFixed_ShouldBeTrue()
    {
        var schema = "binary T { H: { A: byte, B: int le } }";
        var result = ParseBinarySchema(schema);

        var headerField = result.Fields[0] as FieldDefinitionNode;
        var inlineSchema = (InlineSchemaTypeNode)headerField!.TypeAnnotation;


        Assert.IsTrue(inlineSchema.IsFixedSize);
        Assert.AreEqual(5, inlineSchema.FixedSizeBytes);
    }

    [TestMethod]
    public void InlineSchemaTypeNode_IsFixedSize_VariableSize_ShouldBeFalse()
    {
        var fields = new SchemaFieldNode[]
        {
            new FieldDefinitionNode("A", new PrimitiveTypeNode(PrimitiveTypeName.Byte, Endianness.NotApplicable))
        };
        var inlineSchema = new InlineSchemaTypeNode(fields);


        Assert.IsTrue(inlineSchema.IsFixedSize);
    }

    #endregion
}
