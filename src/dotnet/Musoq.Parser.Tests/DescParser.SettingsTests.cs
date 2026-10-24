using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

[TestClass]
public class DescParserSettingsTests : DescParserTestBase
{
    [TestMethod]
    [DataRow("desc schema.method(path: 'file', count: 2)")]
    [DataRow("desc settings schema.method(path: 'file', count: 2)")]
    [DataRow("desc #schema.method(path: 'file', count: 2)")]
    [DataRow("desc settings #schema.method(path: 'file', count: 2)")]
    public void ConcreteDescription_ShouldPreserveNamedSourceArguments(string query)
    {
        var source = GetSchemaFromNode(ParseDescNode(query));

        Assert.IsTrue(source.Parameters.HasNamedArguments);
        Assert.AreEqual(2, source.Parameters.Args.Length);
    }

    [TestMethod]
    [DataRow("desc Source(path: 'file', count: 2)")]
    [DataRow("desc arguments Source(path: 'file', count: 2)")]
    [DataRow("desc settings Source(path: 'file', count: 2)")]
    public void CoupledDescription_ShouldPreserveNamedSourceArguments(string query)
    {
        var source = (AliasedFromNode)ParseDescNode(query).From;

        Assert.AreEqual("Source", source.Identifier);
        Assert.IsTrue(source.Args.HasNamedArguments);
        Assert.AreEqual(2, source.Args.Args.Length);
    }

    [TestMethod]
    public void DescSettingsSchemaMethod_ShouldParse()
    {
        var query = "desc settings #schema.method";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        var descNode = GetDescNode(result);
        var fromNode = (SchemaFromNode)descNode.From;

        Assert.AreEqual(DescForType.Settings, descNode.Type);
        Assert.AreEqual("#schema", fromNode.Schema);
        Assert.AreEqual("method", fromNode.Method);
    }

    [TestMethod]
    public void DescSettingsSchemaMethodWithArguments_ShouldParse()
    {
        var query = "desc settings #schema.method('arg', 1)";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        var descNode = GetDescNode(result);
        var fromNode = (SchemaFromNode)descNode.From;

        Assert.AreEqual(DescForType.Settings, descNode.Type);
        Assert.AreEqual("#schema", fromNode.Schema);
        Assert.AreEqual("method", fromNode.Method);
        Assert.AreEqual(2, fromNode.Parameters.Args.Length);
    }

    [TestMethod]
    public void DescSettingsCoupledAlias_ShouldParse()
    {
        var query = "desc settings Source";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        var result = parser.ComposeAll();
        var descNode = GetDescNode(result);
        var fromNode = (AliasedFromNode)descNode.From;

        Assert.AreEqual(DescForType.Settings, descNode.Type);
        Assert.AreEqual("Source", fromNode.Identifier);
    }

    [TestMethod]
    public void DescSettingsSchemaWithoutMethod_ShouldFail()
    {
        var query = "desc settings #schema";

        var lexer = new Lexer(query, true);
        var parser = new Parser(lexer);

        Assert.Throws<SyntaxException>(parser.ComposeAll);
    }
}
