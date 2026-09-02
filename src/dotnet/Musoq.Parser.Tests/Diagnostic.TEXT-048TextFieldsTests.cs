using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticText048TextFieldsTests : SchemaParserTestsBase
{
    [TestMethod]
    public void TextSchema_FixedTokenRestWhitespace_ShouldPreserveDefinitions()
    {
        const string schemaText =
            "text Fields { Fixed: chars[3] trim upper, Word: token lower, " +
            "Tail: rest ltrim, Gap: whitespace* }";

        var schema = ParseTextSchema(schemaText);

        Assert.AreEqual(TextFieldType.Chars, schema.Fields[0].FieldType);
        Assert.AreEqual("3", schema.Fields[0].PrimaryValue);
        Assert.AreEqual(TextFieldModifier.Trim | TextFieldModifier.Upper, schema.Fields[0].Modifiers);
        Assert.AreEqual(TextFieldType.Token, schema.Fields[1].FieldType);
        Assert.AreEqual(TextFieldModifier.Lower, schema.Fields[1].Modifiers);
        Assert.AreEqual(TextFieldType.Rest, schema.Fields[2].FieldType);
        Assert.AreEqual(TextFieldModifier.LTrim, schema.Fields[2].Modifiers);
        Assert.AreEqual(TextFieldType.Whitespace, schema.Fields[3].FieldType);
        Assert.AreEqual("*", schema.Fields[3].PrimaryValue);
        Assert.Contains("whitespace*", schema.Fields[3].ToString());
    }

    [TestMethod]
    [DataRow("Value: chars[1] nested")]
    [DataRow("Value: token escaped")]
    [DataRow("Value: rest greedy")]
    [DataRow("Value: chars[1] lazy")]
    [DataRow("Value: between '[' ']' nested escaped")]
    [DataRow("Value: until ',' greedy lazy")]
    public void TextSchema_InvalidModifierApplicability_ShouldFailClosed(string field)
    {
        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema($"text Invalid {{ {field} }}"));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
    }

    [TestMethod]
    public void TextSchema_CharsCountOverflow_ShouldReportFieldValue()
    {
        const string schemaText = "text Invalid { Value: chars[2147483648] }";

        var exception = Assert.ThrowsExactly<SyntaxException>(() => ParseTextSchema(schemaText));

        Assert.AreEqual(DiagnosticCode.MQ4002_InvalidTextSchemaField, exception.Code);
        Assert.AreEqual(new TextSpan(schemaText.IndexOf("2147483648", StringComparison.Ordinal), 10), exception.Span);
    }
}
