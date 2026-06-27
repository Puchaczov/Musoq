using Musoq.Parser.Nodes;

namespace Musoq.Parser.Tests;

public abstract class DescParserTestBase
{
    protected static DescNode GetDescNode(Node result)
    {
        var rootNode = (RootNode)result;
        var statementsNode = (StatementsArrayNode)rootNode.Expression;
        var statementNode = statementsNode.Statements[0];
        return (DescNode)statementNode.Node;
    }
}
