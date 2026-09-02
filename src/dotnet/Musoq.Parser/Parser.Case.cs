using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private ((Node When, Node Then)[] WhenThenNodes, Node ElseNode) ComposeCase()
    {
        var caseToken = ConsumeAndGetToken(TokenType.Case);

        if (Current.TokenType == TokenType.Comma)
            throw new SyntaxException(
                "CASE is reserved and starts a CASE expression here, but no WHEN branch or subject expression follows. Use [case] when the word is intended as an identifier.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2027_MissingWhenClause,
                caseToken.Span);

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
