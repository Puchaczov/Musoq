using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private ((Node When, Node Then)[] WhenThenNodes, Node ElseNode, TextSpan Span) ComposeCase()
    {
        using var sourceRegion = _sourceCapture?.Begin(Syntax.SyntaxKind.CaseExpression, Current);
        var caseToken = ConsumeAndGetToken(TokenType.Case);

        if (Current.TokenType == TokenType.Comma)
            CaseExpressionSyntax.ThrowMissingWhen(_lexer.AlreadyResolvedQueryPart, caseToken.Span);

        Node? subjectExpression = null;
        if (Current.TokenType != TokenType.When)
            subjectExpression = ComposeArithmeticExpression(0);

        var whenThenNodes = new List<(Node When, Node Then)>();

        do
        {
            Consume(TokenType.When);
            Node whenNode;
            if (subjectExpression != null)
                whenNode = new EqualityNode(subjectExpression, ComposeArithmeticExpression(0));
            else
                whenNode = ComposeOperations();
            Consume(TokenType.Then);
            var thenNode = ComposeEqualityOperators();

            whenThenNodes.Add(CaseExpressionSyntax.CreateBranch(whenNode, thenNode));
        } while (Current.TokenType == TokenType.When);

        Node elseNode;
        if (Current.TokenType == TokenType.End)
            elseNode = CaseExpressionSyntax.CreateImplicitElse(Current.Span);
        else
        {
            Consume(TokenType.Else);
            elseNode = new ElseNode(ComposeEqualityOperators());
        }
        var endToken = ConsumeAndGetToken(TokenType.End);

        return (whenThenNodes.ToArray(), elseNode, caseToken.Span.Through(endToken.Span));
    }

}
