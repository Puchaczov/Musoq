using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DiagnosticText051TextSchemaCompositionTests : SchemaParserTestsBase
{
    [TestMethod]
    public void TextSchema_DirectSchemaReference_ShouldParseAsTextType()
    {
        var schema = ParseTextSchema("text Container { Header: Item, Tail: rest, }");

        var reference = schema.Fields[0];
        Assert.AreEqual(TextFieldType.SchemaReference, reference.FieldType);
        Assert.AreEqual("Item", reference.PrimaryValue);
        Assert.AreEqual(typeof(object), reference.ReturnType);
        Assert.AreEqual("Header: Item", reference.ToString());
    }

    [TestMethod]
    public void TextSchema_OptionalDirectSchemaReference_ShouldPreserveModifier()
    {
        var schema = ParseTextSchema("text Container { Header: optional Item }");

        var reference = schema.Fields.Single();
        Assert.AreEqual(TextFieldType.SchemaReference, reference.FieldType);
        Assert.AreEqual("Item", reference.PrimaryValue);
        Assert.AreEqual(TextFieldModifier.Optional, reference.Modifiers);
    }

    [TestMethod]
    public void TextSchema_Comments_ShouldRetainOnlyCommentsInsideSchemaSpan()
    {
        const string schemaText = "-- outside before\n" +
                                  "text Record {\n" +
                                  "    -- field comment\n" +
                                  "    Value: rest, /* inline block */\n" +
                                  "    _: literal 'x' -- trailing field comment\n" +
                                  "}\n" +
                                  "-- outside after";

        var schema = ParseTextSchema(schemaText);

        Assert.HasCount(3, schema.Comments);
        Assert.AreEqual("-- field comment", schema.Comments[0].Text);
        Assert.AreEqual("/* inline block */", schema.Comments[1].Text);
        Assert.AreEqual("-- trailing field comment", schema.Comments[2].Text);
        Assert.AreEqual(
            new Musoq.Parser.TextSpan(schemaText.IndexOf("-- field comment", System.StringComparison.Ordinal), "-- field comment".Length),
            schema.Comments[0].Span);
        Assert.AreEqual(
            new Musoq.Parser.TextSpan(schemaText.IndexOf("/* inline block */", System.StringComparison.Ordinal), "/* inline block */".Length),
            schema.Comments[1].Span);
        Assert.IsFalse(schema.Comments.Any(comment => comment.Text.Contains("outside", System.StringComparison.Ordinal)));
    }

    [TestMethod]
    public void TextSchema_EmptyAndTrailingCommaForms_ShouldParse()
    {
        var empty = ParseTextSchema("text Empty { }");
        var trailingComma = ParseTextSchema("text Record { Value: rest, }");

        Assert.IsEmpty(empty.Fields);
        Assert.HasCount(1, trailingComma.Fields);
        Assert.AreEqual("Value", trailingComma.Fields[0].Name);
    }
}
