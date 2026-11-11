using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public class SchemaParserBinarySwitchTests : SchemaParserTestsBase
{
    [TestMethod]
    public void BinarySwitch_WithCasesAndDefault_ShouldParse()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Length: short le,\n"
            + "            Payload: switch Type {\n"
            + "                1 => Login: LoginPayload,\n"
            + "                2 => Data: DataPayload,\n"
            + "                _ => Raw: byte[Length]\n"
            + "            }\n"
            + "        }"
        );

        var result = ParseBinarySchema(schema);

        var payload = (FieldDefinitionNode)result.Fields[2];
        var switchType = payload.TypeAnnotation as BinarySwitchTypeNode;
        Assert.IsNotNull(switchType, "Expected BinarySwitchTypeNode");
        Assert.AreEqual("Type", switchType.Selector);
        Assert.HasCount(3, switchType.Cases);
    }

    [TestMethod]
    public void BinarySwitch_BranchAliases_ShouldBePreserved()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                1 => Login: LoginPayload,\n"
            + "                _ => Raw: byte[4]\n"
            + "            }\n"
            + "        }"
        );

        var switchType = ParseSwitchType(schema);

        Assert.AreEqual("Login", switchType.Cases[0].BranchAlias);
        Assert.AreEqual("Raw", switchType.Cases[1].BranchAlias);
    }

    [TestMethod]
    public void BinarySwitch_CaseLabel_ShouldBeIntegerLiteral()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                1 => Login: LoginPayload,\n"
            + "                _ => Raw: byte[4]\n"
            + "            }\n"
            + "        }"
        );

        var switchType = ParseSwitchType(schema);

        Assert.IsInstanceOfType<IntegerNode>(switchType.Cases[0].CaseValue);
    }

    [TestMethod]
    public void BinarySwitch_DefaultCase_ShouldBeMarkedDefault()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                1 => Login: LoginPayload,\n"
            + "                _ => Raw: byte[4]\n"
            + "            }\n"
            + "        }"
        );

        var switchType = ParseSwitchType(schema);

        Assert.IsTrue(switchType.Cases[1].IsDefault);
        Assert.IsNull(switchType.Cases[1].CaseValue);
        Assert.AreSame(switchType.Cases[1], switchType.DefaultCase);
    }

    [TestMethod]
    public void BinarySwitch_WithoutDefault_ShouldParse()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                1 => Login: LoginPayload,\n"
            + "                2 => Data: DataPayload\n"
            + "            }\n"
            + "        }"
        );

        var switchType = ParseSwitchType(schema);

        Assert.HasCount(2, switchType.Cases);
        Assert.IsNull(switchType.DefaultCase);
    }

    [TestMethod]
    public void BinarySwitch_NestedSchemaBranchType_ShouldParse()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                1 => Login: LoginPayload,\n"
            + "                _ => Raw: byte[4]\n"
            + "            }\n"
            + "        }"
        );

        var switchType = ParseSwitchType(schema);

        var branch = switchType.Cases[0].BranchType as SchemaReferenceTypeNode;
        Assert.IsNotNull(branch, "Expected SchemaReferenceTypeNode branch");
        Assert.AreEqual("LoginPayload", branch.SchemaName);
    }

    [TestMethod]
    public void BinarySwitch_ByteArrayBranchType_ShouldParse()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                1 => Login: LoginPayload,\n"
            + "                _ => Raw: byte[4]\n"
            + "            }\n"
            + "        }"
        );

        var switchType = ParseSwitchType(schema);

        Assert.IsInstanceOfType<ByteArrayTypeNode>(switchType.Cases[1].BranchType);
    }

    [TestMethod]
    public void BinarySwitch_DuplicateAliases_ShouldReportDiagnostic()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                1 => Same: LoginPayload,\n"
            + "                2 => Same: DataPayload\n"
            + "            }\n"
            + "        }"
        );

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));
        Assert.AreEqual(DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias, exception.Code);
    }

    [TestMethod]
    public void BinarySwitch_SelectorNotPreviousField_ShouldReportDiagnostic()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Missing {\n"
            + "                1 => Login: LoginPayload,\n"
            + "                _ => Raw: byte[4]\n"
            + "            }\n"
            + "        }"
        );

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));
        Assert.AreEqual(DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField, exception.Code);
    }

    [TestMethod]
    public void BinarySwitch_SelectorAfterSwitchField_ShouldReportDiagnostic()
    {
        var schema = (
            "binary Packet {\n"
            + "            Payload: switch Type {\n"
            + "                1 => Login: LoginPayload,\n"
            + "                _ => Raw: byte[4]\n"
            + "            },\n"
            + "            Type: byte\n"
            + "        }"
        );

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));
        Assert.AreEqual(DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField, exception.Code);
    }

    [TestMethod]
    public void BinarySwitch_NonLiteralCaseLabel_ShouldReportDiagnostic()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                Type => Login: LoginPayload,\n"
            + "                _ => Raw: byte[4]\n"
            + "            }\n"
            + "        }"
        );

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));
        Assert.AreEqual(DiagnosticCode.MQ4013_InvalidSwitchCaseLabel, exception.Code);
    }

    [TestMethod]
    public void BinarySwitch_DefaultNotLast_ShouldReportDiagnostic()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                _ => Raw: byte[4],\n"
            + "                1 => Login: LoginPayload\n"
            + "            }\n"
            + "        }"
        );

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));
        Assert.AreEqual(DiagnosticCode.MQ4013_InvalidSwitchCaseLabel, exception.Code);
    }

    [TestMethod]
    public void BinarySwitch_MissingBranchAliasColon_ShouldThrow()
    {
        var schema = (
            "binary Packet {\n"
            + "            Type: byte,\n"
            + "            Payload: switch Type {\n"
            + "                1 => LoginPayload\n"
            + "            }\n"
            + "        }"
        );

        Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));
    }

    private static BinarySwitchTypeNode ParseSwitchType(string schema)
    {
        var result = ParseBinarySchema(schema);
        var payload = (FieldDefinitionNode)result.Fields[^1];
        var switchType = payload.TypeAnnotation as BinarySwitchTypeNode;
        Assert.IsNotNull(switchType, "Expected BinarySwitchTypeNode");
        return switchType;
    }
}
