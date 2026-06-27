using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private ((Node When, Node Then)[] WhenThenNodes, Node ElseNode) ComposeCase()
    {
        Consume(TokenType.Case);

        Node? subjectExpression = null;
        if (Current.TokenType != TokenType.When)
            subjectExpression = ComposeArithmeticExpression(0);

        var whenThenNodes = new List<(Node When, Node Then)>();

        while (Current.TokenType == TokenType.When)
        {
            Consume(TokenType.When);
            Node whenNode;
            if (subjectExpression != null)
                whenNode = new EqualityNode(subjectExpression, ComposeArithmeticExpression(0));
            else
                whenNode = ComposeOperations();
            Consume(TokenType.Then);
            var thenNode = ComposeEqualityOperators();

            whenThenNodes.Add((
                new WhenNode(whenNode),
                new ThenNode(thenNode)));
        }

        Consume(TokenType.Else);
        var elseNode = ComposeEqualityOperators();
        Consume(TokenType.End);

        return (whenThenNodes.ToArray(), new ElseNode(elseNode));
    }

}
