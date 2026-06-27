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
