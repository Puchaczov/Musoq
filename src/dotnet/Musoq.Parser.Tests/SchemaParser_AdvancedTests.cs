using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

/// <summary>
///     Tests for the SchemaParser class - Error cases, generics, composition, and inline schemas.
/// </summary>
[TestClass]
public class SchemaParser_AdvancedTests : SchemaParserTestsBase
{
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

    #region Generic Schema Tests

    [TestMethod]
    public void BinarySchema_SingleTypeParameter_ShouldParse()
    {
        var schema = "binary LengthPrefixed<T> { Length: int le, Data: T[Length] }";

        var result = ParseBinarySchema(schema);

        Assert.AreEqual("LengthPrefixed", result.Name);
        Assert.IsTrue(result.IsGeneric);
        Assert.HasCount(1, result.TypeParameters);
        Assert.AreEqual("T", result.TypeParameters[0]);
        Assert.HasCount(2, result.Fields);
    }

    [TestMethod]
    public void BinarySchema_MultipleTypeParameters_ShouldParse()
    {
        var schema = "binary Pair<T, U> { First: T, Second: U }";

        var result = ParseBinarySchema(schema);

        Assert.AreEqual("Pair", result.Name);
        Assert.IsTrue(result.IsGeneric);
        Assert.HasCount(2, result.TypeParameters);
        Assert.AreEqual("T", result.TypeParameters[0]);
        Assert.AreEqual("U", result.TypeParameters[1]);
    }

    [TestMethod]
    public void BinarySchema_GenericWithExtends_ShouldParse()
    {
        var schema = "binary Extended<T> extends Base { Data: T }";

        var result = ParseBinarySchema(schema);

        Assert.AreEqual("Extended", result.Name);
        Assert.IsTrue(result.IsGeneric);
        Assert.HasCount(1, result.TypeParameters);
        Assert.AreEqual("T", result.TypeParameters[0]);
        Assert.AreEqual("Base", result.Extends);
    }

    [TestMethod]
    public void BinarySchema_NonGeneric_ShouldHaveEmptyTypeParameters()
    {
        var schema = "binary Header { Magic: int le }";

        var result = ParseBinarySchema(schema);

        Assert.AreEqual("Header", result.Name);
        Assert.IsFalse(result.IsGeneric);
        Assert.IsEmpty(result.TypeParameters);
    }

    [TestMethod]
    public void BinarySchema_GenericTypeInstantiation_ShouldParse()
    {
        var schema = "binary Message { Records: LengthPrefixed<Record> }";

        var result = ParseBinarySchema(schema);

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
        var schema = "binary Container { Data: Pair<Header, Footer> }";

        var result = ParseBinarySchema(schema);

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

        var result = ParseBinarySchema(schema);

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

        var result = ParseBinarySchema(schema);

        var dataField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(dataField);

        var schemaRef = dataField.TypeAnnotation as SchemaReferenceTypeNode;
        Assert.IsNotNull(schemaRef);
        Assert.AreEqual("T", schemaRef.SchemaName);
        Assert.IsFalse(schemaRef.IsGenericInstantiation);
        Assert.AreEqual("T", schemaRef.FullTypeName);
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
        var schema = @"binary Packet {
            Header: { Magic: int le, Version: short le }
        }";

        var result = ParseBinarySchema(schema);

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

        var result = ParseBinarySchema(schema);

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
        var schema = "binary T { H: { A: byte, B: short le } }";
        var result = ParseBinarySchema(schema);

        var headerField = result.Fields[0] as FieldDefinitionNode;
        Assert.IsNotNull(headerField);
        var inlineSchema = (InlineSchemaTypeNode)headerField.TypeAnnotation;

        var str = inlineSchema.ToString();

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
