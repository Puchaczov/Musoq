using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticBinary044SwitchTests : SchemaParserTestsBase
{
    [TestMethod]
    public void BinarySwitch_ScalarLabels_ShouldParseAndPreserveSourceSpans()
    {
        const string schema = "binary Packet { Type: byte, Payload: switch Type { 0b1 => Binary: byte, 0o2 => Octal: byte, 0x3 => Hex: byte } }";

        var result = ParseBinarySchema(schema);
        var switchType = ((FieldDefinitionNode)result.Fields[1]).TypeAnnotation as BinarySwitchTypeNode;

        Assert.IsNotNull(switchType);
        Assert.AreEqual(new TextSpan(schema.IndexOf("switch Type", StringComparison.Ordinal) + 7, 4), switchType.SelectorSpan);
        Assert.IsInstanceOfType<BinaryIntegerNode>(switchType.Cases[0].CaseValue);
        Assert.IsInstanceOfType<OctalIntegerNode>(switchType.Cases[1].CaseValue);
        Assert.IsInstanceOfType<HexIntegerNode>(switchType.Cases[2].CaseValue);
        Assert.AreEqual(new TextSpan(schema.IndexOf("0b1", StringComparison.Ordinal), 3), switchType.Cases[0].CaseLabelSpan);
        Assert.AreEqual(new TextSpan(schema.IndexOf("Binary:", StringComparison.Ordinal), 6), switchType.Cases[0].BranchAliasSpan);
        var binaryTypeStart = schema.IndexOf("byte", schema.IndexOf("Binary:", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.AreEqual(new TextSpan(binaryTypeStart, 4), switchType.Cases[0].BranchTypeSpan);
    }

    [TestMethod]
    public void BinarySwitch_StringSelector_ShouldAcceptStringLiteralLabel()
    {
        const string schema = "binary Packet { Kind: string[1] ascii, Payload: switch Kind { 'A' => Letter: byte, _ => Raw: byte[1] } }";

        var result = ParseBinarySchema(schema);
        var switchType = ((FieldDefinitionNode)result.Fields[1]).TypeAnnotation as BinarySwitchTypeNode;

        Assert.IsNotNull(switchType);
        Assert.IsInstanceOfType<WordNode>(switchType.Cases[0].CaseValue);
        Assert.IsTrue(switchType.Cases[1].IsDefault);
    }

    [TestMethod]
    public void BinarySwitch_ComputedBooleanSelector_ShouldAcceptBooleanLabel()
    {
        const string schema = "binary Packet { Type: byte, IsLogin: Type = 1, Payload: switch IsLogin { true => Login: byte, _ => Raw: byte[1] } }";

        var result = ParseBinarySchema(schema);
        var switchType = ((FieldDefinitionNode)result.Fields[2]).TypeAnnotation as BinarySwitchTypeNode;

        Assert.IsNotNull(switchType);
        Assert.IsInstanceOfType<BooleanNode>(switchType.Cases[0].CaseValue);
    }

    [TestMethod]
    public void BinarySwitch_IncompatibleLabel_ShouldReportMq4013AtLabel()
    {
        const string schema = "binary Packet { Type: byte, Payload: switch Type { 'login' => Login: byte } }";
        var labelStart = schema.IndexOf("'login'", StringComparison.Ordinal);

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));

        Assert.AreEqual(DiagnosticCode.MQ4013_InvalidSwitchCaseLabel, exception.Code);
        Assert.AreEqual(new TextSpan(labelStart, "'login'".Length), exception.Span!.Value);
    }

    [TestMethod]
    public void BinarySwitch_OutOfRangeLabel_ShouldReportMq4013AtLabel()
    {
        const string schema = "binary Packet { Type: byte, Payload: switch Type { 256 => TooLarge: byte } }";
        var labelStart = schema.IndexOf("256", StringComparison.Ordinal);

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));

        Assert.AreEqual(DiagnosticCode.MQ4013_InvalidSwitchCaseLabel, exception.Code);
        Assert.AreEqual(new TextSpan(labelStart, 3), exception.Span!.Value);
    }

    [TestMethod]
    public void BinarySwitch_UnsupportedBranchType_ShouldReportMq4013AtType()
    {
        const string schema = "binary Packet { Type: byte, Payload: switch Type { 1 => Flags: bits[4] } }";
        var typeStart = schema.IndexOf("bits", StringComparison.Ordinal);

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));

        Assert.AreEqual(DiagnosticCode.MQ4013_InvalidSwitchCaseLabel, exception.Code);
        Assert.AreEqual(new TextSpan(typeStart, 4), exception.Span!.Value);
    }

    [TestMethod]
    public void BinarySwitch_DuplicateAlias_ShouldIgnoreCaseAndReportSecondAlias()
    {
        const string schema = "binary Packet { Type: byte, Payload: switch Type { 1 => Same: byte, 2 => same: byte } }";
        var aliasStart = schema.LastIndexOf("same", StringComparison.Ordinal);

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));

        Assert.AreEqual(DiagnosticCode.MQ4012_DuplicateSwitchBranchAlias, exception.Code);
        Assert.AreEqual(new TextSpan(aliasStart, 4), exception.Span!.Value);
    }

    [TestMethod]
    public void BinarySwitch_SelectorNotPrevious_ShouldReportMq4011AtSelector()
    {
        const string schema = "binary Packet { Payload: switch Missing { 1 => Login: byte } }";
        var selectorStart = schema.IndexOf("Missing", StringComparison.Ordinal);

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));

        Assert.AreEqual(DiagnosticCode.MQ4011_SwitchSelectorNotPreviousField, exception.Code);
        Assert.AreEqual(new TextSpan(selectorStart, "Missing".Length), exception.Span!.Value);
    }

    [TestMethod]
    public void BinarySwitch_DefaultNotLast_ShouldReportMq4013AtDefaultLabel()
    {
        const string schema = "binary Packet { Type: byte, Payload: switch Type { _ => Raw: byte[1], 1 => Login: byte } }";
        var defaultStart = schema.IndexOf('_');

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseBinarySchema(schema));

        Assert.AreEqual(DiagnosticCode.MQ4013_InvalidSwitchCaseLabel, exception.Code);
        Assert.AreEqual(new TextSpan(defaultStart, 1), exception.Span!.Value);
    }
}
