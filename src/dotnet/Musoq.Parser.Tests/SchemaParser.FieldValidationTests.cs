using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public class SchemaParserFieldValidationTests : SchemaParserTestsBase
{
    private static FieldValueValidationNode ParseFieldValidation(string fieldDefinition)
    {
        var schema = $"binary T {{ F: {fieldDefinition} }}";
        var result = ParseBinarySchema(schema);
        var field = (FieldDefinitionNode)result.Fields[0];
        Assert.IsNotNull(field.ValueValidation, "Expected a value validation on the field.");
        return field.ValueValidation;
    }

    [TestMethod]
    public void ScalarConst_OnPrimitive_ShouldParse()
    {
        var validation = ParseFieldValidation("int le const 1234");

        Assert.AreEqual(FieldValueValidationKind.Const, validation.Kind);
        Assert.IsFalse(validation.IsByteList);
        Assert.HasCount(1, validation.Values);
    }

    [TestMethod]
    public void ScalarMagic_OnPrimitive_ShouldParse()
    {
        var validation = ParseFieldValidation("uint le magic 0x474E5089");

        Assert.AreEqual(FieldValueValidationKind.Magic, validation.Kind);
        Assert.IsFalse(validation.IsByteList);
    }

    [TestMethod]
    public void ByteListMagic_OnByteArray_ShouldParse()
    {
        var validation = ParseFieldValidation("byte[4] magic [0x89, 0x50, 0x4E, 0x47]");

        Assert.AreEqual(FieldValueValidationKind.Magic, validation.Kind);
        Assert.IsTrue(validation.IsByteList);
        Assert.HasCount(4, validation.Values);
    }

    [TestMethod]
    public void ByteListConst_WithTrailingComma_ShouldParse()
    {
        var validation = ParseFieldValidation("byte[2] const [1, 2,]");

        Assert.IsTrue(validation.IsByteList);
        Assert.HasCount(2, validation.Values);
    }

    [TestMethod]
    public void ByteListConst_Empty_ShouldParse()
    {
        var validation = ParseFieldValidation("byte[0] const []");

        Assert.AreEqual(FieldValueValidationKind.Const, validation.Kind);
        Assert.IsTrue(validation.IsByteList);
        Assert.IsEmpty(validation.Values);
    }

    [TestMethod]
    public void StringOneOf_ShouldParse()
    {
        var validation = ParseFieldValidation("string[4] ascii oneOf ['IHDR', 'IDAT', 'IEND']");

        Assert.AreEqual(FieldValueValidationKind.OneOf, validation.Kind);
        Assert.IsFalse(validation.IsByteList);
        Assert.HasCount(3, validation.Values);
    }

    [TestMethod]
    public void RawSubstreamMagic_ShouldParse()
    {
        var schema = @"binary Packet {
            Length: uint le,
            Payload: substream[Length] raw magic [0x01, 0x02]
        }";

        var result = ParseBinarySchema(schema);
        var payload = (FieldDefinitionNode)result.Fields[1];

        Assert.IsNotNull(payload.ValueValidation);
        Assert.IsTrue(payload.ValueValidation.IsByteList);
    }

    [TestMethod]
    public void Validation_CoexistsWithCheck_ShouldParseBoth()
    {
        var schema = "binary T { Version: byte oneOf [1, 2] check Version > 0 }";
        var result = ParseBinarySchema(schema);
        var field = (FieldDefinitionNode)result.Fields[0];

        Assert.IsNotNull(field.ValueValidation);
        Assert.IsNotNull(field.Constraint);
    }

    [TestMethod]
    public void Validation_CoexistsWithAtAndWhen_ShouldParseAll()
    {
        var schema = "binary T { Sig: byte[2] magic [1, 2] at 0 when true }";
        var result = ParseBinarySchema(schema);
        var field = (FieldDefinitionNode)result.Fields[0];

        Assert.IsNotNull(field.ValueValidation);
        Assert.IsNotNull(field.AtOffset);
        Assert.IsNotNull(field.WhenCondition);
    }

    [TestMethod]
    public void FieldNamedMagic_ShouldRemainBackwardCompatible()
    {
        var schema = "binary Header { Magic: int le }";
        var result = ParseBinarySchema(schema);
        var field = (FieldDefinitionNode)result.Fields[0];

        Assert.AreEqual("Magic", field.Name);
        Assert.IsNull(field.ValueValidation);
    }

    [TestMethod]
    public void DuplicateValidationModifier_ShouldReportInvalidFieldConstraint()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => ParseBinarySchema("binary T { F: byte const 1 const 2 }"));

        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.Code);
    }

    [TestMethod]
    public void ByteValueOutOfRange_ShouldReportInvalidFieldConstraint()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => ParseBinarySchema("binary T { F: byte[1] const [256] }"));

        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.Code);
    }

    [TestMethod]
    public void EmptyOneOf_ShouldReportInvalidFieldConstraint()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => ParseBinarySchema("binary T { F: byte oneOf [] }"));

        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.Code);
    }

    [TestMethod]
    public void ScalarConst_OnByteArray_ShouldReportInvalidFieldConstraint()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => ParseBinarySchema("binary T { F: byte[4] const 5 }"));

        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.Code);
    }

    [TestMethod]
    public void ByteListConst_OnPrimitive_ShouldReportInvalidFieldConstraint()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => ParseBinarySchema("binary T { F: int le const [1, 2] }"));

        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.Code);
    }

    [TestMethod]
    public void OneOf_OnByteArray_ShouldReportInvalidFieldConstraint()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => ParseBinarySchema("binary T { F: byte[4] oneOf [1, 2] }"));

        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.Code);
    }

    [TestMethod]
    public void Validation_OnSchemaReference_ShouldReportInvalidFieldConstraint()
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(
            () => ParseBinarySchema("binary T { F: OtherSchema const 1 }"));

        Assert.AreEqual(DiagnosticCode.MQ4006_InvalidFieldConstraint, exception.Code);
    }
}
