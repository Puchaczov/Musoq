using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticText049TextAlternativesTests : SchemaParserTestsBase
{
    [TestMethod]
    public void TextSchema_OptionalSwitch_ShouldPreserveCasesAndOptionalModifier()
    {
        const string schemaText =
            "text Envelope { Content: optional switch { pattern 'X' => Payload, _ => Fallback } }";

        var schema = ParseTextSchema(schemaText);
        var field = schema.Fields[0];

        Assert.AreEqual(TextFieldType.Switch, field.FieldType);
        Assert.AreEqual(TextFieldModifier.Optional, field.Modifiers);
        Assert.HasCount(2, field.SwitchCases);
        Assert.AreEqual("X", field.SwitchCases[0].Pattern);
        Assert.AreEqual("Payload", field.SwitchCases[0].TypeName);
        Assert.IsTrue(field.SwitchCases[1].IsDefault);
        Assert.AreEqual("Fallback", field.SwitchCases[1].TypeName);
    }

    [TestMethod]
    public void TextSchema_EmptySwitch_ShouldReportInvalidTextFieldAtClosingBrace()
    {
        const string schemaText = "text Invalid { Content: switch { } }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
        var closingBrace = schemaText.IndexOf('}', schemaText.IndexOf("switch", StringComparison.Ordinal));
        Assert.AreEqual(new TextSpan(closingBrace, 1), exception.Span);
    }

    [TestMethod]
    public void TextSchema_SwitchDefaultBeforePattern_ShouldReportInvalidTextFieldAtPattern()
    {
        const string schemaText =
            "text Invalid { Content: switch { _ => Fallback, pattern 'X' => Payload } }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
        var patternStart = schemaText.IndexOf("pattern", StringComparison.Ordinal);
        Assert.AreEqual(new TextSpan(patternStart, "pattern".Length), exception.Span);
    }

    [TestMethod]
    public void TextSchema_SwitchDuplicateDefault_ShouldReportInvalidTextFieldAtSecondDefault()
    {
        const string schemaText =
            "text Invalid { Content: switch { _ => First, _ => Second } }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
        var secondDefault = schemaText.LastIndexOf('_');
        Assert.AreEqual(new TextSpan(secondDefault, 1), exception.Span);
    }

    [TestMethod]
    public void TextSchema_SwitchInvalidPattern_ShouldReportInvalidTextFieldAtPatternLiteral()
    {
        const string schemaText = "text Invalid { Content: switch { pattern '[unclosed' => Payload } }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
        var literalStart = schemaText.IndexOf("'[unclosed'", StringComparison.Ordinal);
        Assert.AreEqual(new TextSpan(literalStart, "'[unclosed'".Length), exception.Span);
    }

    [TestMethod]
    public void TextSchema_OptionalRepeat_ShouldPreserveReferencedSchemaAndModifier()
    {
        var schema = ParseTextSchema(
            "text Envelope { Items: optional repeat Item until ',' } ");
        var field = schema.Fields[0];

        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual("Item", field.PrimaryValue);
        Assert.AreEqual(",", field.SecondaryValue);
        Assert.AreEqual(TextFieldModifier.Optional, field.Modifiers);
    }
}
