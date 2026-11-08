using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Tests;

public partial class TextInterpretationTests
{
    [TestMethod]
    public void Parse_DirectTextSchemaReference_ShouldParseNestedValueAndContinueCursor()
    {
        var registry = new SchemaRegistry();
        registry.Register(
            "Item",
            new TextSchemaNode(
                "Item",
                [
                    new TextFieldDefinitionNode("Key", TextFieldType.Until, ":"),
                    new TextFieldDefinitionNode("Value", TextFieldType.Until, "|")
                ]));
        registry.Register(
            "Container",
            new TextSchemaNode(
                "Container",
                [
                    new TextFieldDefinitionNode("Header", TextFieldType.SchemaReference, "Item"),
                    new TextFieldDefinitionNode("Tail", TextFieldType.Rest)
                ]));

        var interpreter = CompileFromRegistry(registry, "Container");
        var result = InvokeParse(interpreter, "name:value|tail");
        var header = GetPropertyValue<object>(result, "Header");

        Assert.AreEqual("name", GetPropertyValue<string>(header, "Key"));
        Assert.AreEqual("value", GetPropertyValue<string>(header, "Value"));
        Assert.AreEqual("tail", GetPropertyValue<string>(result, "Tail"));
    }

    [TestMethod]
    public void Parse_TextSchemaInheritance_ShouldIncludeParentFieldsBeforeChildFields()
    {
        var registry = new SchemaRegistry();
        registry.Register(
            "Base",
            new TextSchemaNode(
                "Base",
                [new TextFieldDefinitionNode("Prefix", TextFieldType.Literal, "X")]));
        registry.Register(
            "Derived",
            new TextSchemaNode(
                "Derived",
                [new TextFieldDefinitionNode("Suffix", TextFieldType.Rest)],
                "Base"));

        var interpreter = CompileFromRegistry(registry, "Derived");
        var result = InvokeParse(interpreter, "Xpayload");

        Assert.AreEqual("X", GetPropertyValue<string>(result, "Prefix"));
        Assert.AreEqual("payload", GetPropertyValue<string>(result, "Suffix"));
    }
}
