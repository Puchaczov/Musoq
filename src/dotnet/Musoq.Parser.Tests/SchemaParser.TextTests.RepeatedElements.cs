using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser.Tests;

public partial class SchemaParserTextTests
{
    [TestMethod]
    [DataRow("chars[1]", TextFieldType.Chars, "1")]
    [DataRow("literal 'x'", TextFieldType.Literal, "x")]
    public void TextRepeat_InlineElement_ShouldPreserveTypeAndDefinition(
        string definition, TextFieldType elementType, string value)
    {
        var text = $"text Letters {{ Items: repeat {definition} until end }}";
        var schema = ParseTextSchema(text);
        var field = schema.Fields[0];
        var element = field.RepeatElement ?? throw new AssertFailedException("Expected inline repeat element.");

        Assert.AreEqual(TextFieldType.Repeat, field.FieldType);
        Assert.AreEqual(elementType, element.FieldType);
        Assert.AreEqual(value, element.PrimaryValue);
        Assert.AreEqual(typeof(string[]), field.ReturnType);
        Assert.AreEqual($"Items: repeat {definition} until end", field.ToString());
    }

    [TestMethod]
    public void TextRepeat_OptionalInlineElement_ShouldPreserveElementAndDelimiter()
    {
        var schema = ParseTextSchema("text Letters { Items: optional repeat chars[1] until '|' }");
        var field = schema.Fields[0];
        var element = field.RepeatElement ?? throw new AssertFailedException("Expected inline repeat element.");

        Assert.AreEqual(TextFieldModifier.Optional, field.Modifiers);
        Assert.AreEqual(TextFieldType.Chars, element.FieldType);
        Assert.AreEqual("1", element.PrimaryValue);
        Assert.AreEqual("|", field.SecondaryValue);
    }
}
