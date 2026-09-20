using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Lexing;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Tests;

public abstract class DescParserTestBase
{
    protected static DescNode ParseDescNode(string query)
    {
        var parser = new Parser(new Lexer(query, true));
        return GetDescNode(parser.ComposeAll());
    }

    protected static SchemaFromNode GetSchemaFromNode(DescNode desc)
    {
        return Assert.IsInstanceOfType<SchemaFromNode>(desc.From);
    }

    protected static DescNode GetDescNode(Node result)
    {
        var rootNode = (RootNode)result;
        var statementsNode = (StatementsArrayNode)rootNode.Expression;
        var statementNode = statementsNode.Statements[0];
        return (DescNode)statementNode.Node;
    }
}
