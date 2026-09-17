using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

[TestClass]
public sealed class DescParserArgumentsTests : DescParserTestBase
{
    [TestMethod]
    public void DescArgumentsSchemaMethodWithStructuralCall_ShouldParse()
    {
        const string query = "desc arguments #inputs.match('TODO', patterns: array { (Id: 'todo', Pattern: 'TODO') })";
        var parser = new Parser(new Lexer(query, true));

        var desc = GetDescNode(parser.ComposeAll());
        var source = (SchemaFromNode)desc.From;

        Assert.AreEqual(DescForType.Arguments, desc.Type);
        Assert.AreEqual("#inputs", source.Schema);
        Assert.AreEqual("match", source.Method);
        Assert.AreEqual(2, source.Parameters.Args.Length);
    }

    [TestMethod]
    public void DescArgumentsSchemaMethodWithoutCall_ShouldParseInventory()
    {
        var parser = new Parser(new Lexer("desc arguments #inputs.match", true));

        var desc = GetDescNode(parser.ComposeAll());
        var source = (SchemaFromNode)desc.From;

        Assert.AreEqual(DescForType.Arguments, desc.Type);
        Assert.AreEqual("#inputs", source.Schema);
        Assert.AreEqual("match", source.Method);
        Assert.AreEqual(0, source.Parameters.Args.Length);
    }

    [TestMethod]
    public void DescArgumentsCoupledAlias_ShouldParse()
    {
        var parser = new Parser(new Lexer("desc arguments Matches", true));

        var desc = GetDescNode(parser.ComposeAll());
        var source = (AliasedFromNode)desc.From;

        Assert.AreEqual(DescForType.Arguments, desc.Type);
        Assert.AreEqual("Matches", source.Identifier);
    }

    [TestMethod]
    public void DescArgumentsCommentsAroundArrayDelimiters_ShouldParse()
    {
        const string query = "desc arguments #inputs.match('TODO', patterns: array /* open */ { (Id: 'todo') /* close */ })";
        var parser = new Parser(new Lexer(query, true));

        var desc = GetDescNode(parser.ComposeAll());

        Assert.AreEqual(DescForType.Arguments, desc.Type);
    }
}