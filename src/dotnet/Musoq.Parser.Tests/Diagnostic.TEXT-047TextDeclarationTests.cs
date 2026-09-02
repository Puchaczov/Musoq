using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticText047TextDeclarationTests : SchemaParserTestsBase
{
    [TestMethod]
    public void TextSchema_DeclarationFields_ShouldPreserveValuesModifiersAndSpans()
    {
        const string schemaText =
            "text LogEntry { Timestamp: between '[' ']' nested, _: literal ' ', " +
            "Level: until ':', Code: pattern '(?<Value>[A-Z]+)' capture (Value) }";

        var schema = ParseTextSchema(schemaText);

        Assert.HasCount(4, schema.Fields);
        Assert.AreEqual(new TextSpan(schemaText.IndexOf("Timestamp", StringComparison.Ordinal), 9),
            schema.Fields[0].Span);
        Assert.AreEqual(TextFieldType.Between, schema.Fields[0].FieldType);
        Assert.AreEqual("[", schema.Fields[0].PrimaryValue);
        Assert.AreEqual("]", schema.Fields[0].SecondaryValue);
        Assert.IsTrue((schema.Fields[0].Modifiers & TextFieldModifier.Nested) != 0);
        Assert.AreEqual(new TextSpan(schemaText.IndexOf("Level", StringComparison.Ordinal), 5),
            schema.Fields[2].Span);
        Assert.AreEqual(TextFieldType.Pattern, schema.Fields[3].FieldType);
        Assert.AreEqual("(?<Value>[A-Z]+)", schema.Fields[3].PrimaryValue);
        Assert.HasCount(1, schema.Fields[3].CaptureGroups);
        Assert.AreEqual("Value", schema.Fields[3].CaptureGroups[0]);
    }

    [TestMethod]
    public void TextSchema_RepeatedDiscardFields_ShouldRemainValid()
    {
        var schema = ParseTextSchema("text LogEntry { _: literal '[', Value: until ']', _: literal ']' }");

        Assert.HasCount(3, schema.Fields);
        Assert.IsTrue(schema.Fields[0].IsDiscard);
        Assert.IsTrue(schema.Fields[2].IsDiscard);
    }

    [TestMethod]
    public void TextSchema_FieldNames_ShouldRemainCaseSensitive()
    {
        var schema = ParseTextSchema("text CaseSensitive { Value: rest, value: rest }");

        Assert.HasCount(2, schema.Fields);
        Assert.AreEqual("Value", schema.Fields[0].Name);
        Assert.AreEqual("value", schema.Fields[1].Name);
    }

    [TestMethod]
    public void TextSchema_DuplicateField_ShouldReportSecondNameSpan()
    {
        const string schemaText = "text LogLine { Data: until ' ', Data: rest }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4008_DuplicateSchemaField, exception.Code);
        Assert.AreEqual(new TextSpan(schemaText.LastIndexOf("Data", StringComparison.Ordinal), 4), exception.Span);
    }

    [TestMethod]
    public void TextSchema_InvalidPattern_ShouldReportPatternLiteralSpan()
    {
        const string schemaText = "text LogLine { Data: pattern '[unclosed' }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
        const string patternLiteral = "'[unclosed'";
        Assert.AreEqual(new TextSpan(schemaText.IndexOf(patternLiteral, StringComparison.Ordinal), patternLiteral.Length),
            exception.Span);
    }

    [TestMethod]
    public void TextSchema_UnsupportedPatternConstruct_ShouldReportPatternLiteralSpan()
    {
        const string schemaText = "text LogLine { Data: pattern '(?=abc)abc' }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
        const string patternLiteral = "'(?=abc)abc'";
        Assert.AreEqual(new TextSpan(schemaText.IndexOf(patternLiteral, StringComparison.Ordinal), patternLiteral.Length),
            exception.Span);
    }

    [TestMethod]
    public void TextSchema_UnknownCaptureGroup_ShouldReportPatternLiteralSpan()
    {
        const string schemaText = "text LogLine { Data: pattern '(?<Value>\\d+)' capture (Missing) }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
        const string patternLiteral = "'(?<Value>\\d+)'";
        Assert.AreEqual(new TextSpan(schemaText.IndexOf(patternLiteral, StringComparison.Ordinal), patternLiteral.Length),
            exception.Span);
    }

    [TestMethod]
    public void TextSchema_BetweenCustomEscape_ShouldParseOneCharacterEscape()
    {
        var schema = ParseTextSchema("text Config { Value: between '[' ']' escaped '~' }");

        Assert.AreEqual(TextFieldType.Between, schema.Fields[0].FieldType);
        Assert.IsTrue((schema.Fields[0].Modifiers & TextFieldModifier.Escaped) != 0);
        Assert.AreEqual("~", schema.Fields[0].EscapeCharacter);
    }

    [TestMethod]
    public void TextSchema_BetweenMultiCharacterEscape_ShouldBeRejected()
    {
        const string schemaText = "text Config { Value: between '[' ']' escaped 'ab' }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
        const string escapeLiteral = "'ab'";
        Assert.AreEqual(new TextSpan(schemaText.IndexOf(escapeLiteral, StringComparison.Ordinal), escapeLiteral.Length),
            exception.Span);
    }
}
