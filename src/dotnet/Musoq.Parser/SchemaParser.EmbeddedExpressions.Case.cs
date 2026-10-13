using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private bool IsSchemaKeywordReference() => PeekNextTokenType() is
        TokenType.Equality or TokenType.Diff or TokenType.Greater or TokenType.GreaterEqual or TokenType.Less or TokenType.LessEqual
        or TokenType.Plus or TokenType.Hyphen or TokenType.Star or TokenType.FSlash or TokenType.Mod or TokenType.Dot
        or TokenType.And or TokenType.Or or TokenType.Comma or TokenType.RBracket or TokenType.RightParenthesis
        or TokenType.RightSquareBracket or TokenType.Check or TokenType.Then or TokenType.Else or TokenType.End;

    private Node ComposeSchemaCase()
    {
        var start = ConsumeAndGetToken(TokenType.Case);
        var subject = Current.TokenType == TokenType.When ? null : ComposeExpression();
        var pairs = new List<(Node When, Node Then)>();
        do
        {
            Consume(TokenType.When);
            var condition = ComposeExpression();
            if (subject is not null)
                condition = new EqualityNode(subject, condition);
            Consume(TokenType.Then);
            pairs.Add((new WhenNode(condition), new ThenNode(ComposeExpression())));
        } while (Current.TokenType == TokenType.When);
        Consume(TokenType.Else);
        var otherwise = new ElseNode(ComposeExpression());
        var end = ConsumeAndGetToken(TokenType.End);
        return new CaseNode(pairs.ToArray(), otherwise).WithSpan(start.Span.Through(end.Span));
    }
}
