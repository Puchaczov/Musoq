using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeIsPredicate(Node node)
    {
        if (Current.TokenType == TokenType.Not)
        {
            Consume(TokenType.Not);
            if (Current.TokenType == TokenType.Distinct)
                return ComposeIsDistinctFrom(node, true);
            ConsumeIsNullToken("IS NOT");
            return new IsNullNode(node, true);
        }

        return Current.TokenType switch
        {
            TokenType.Distinct => ComposeIsDistinctFrom(node, false),
            TokenType.Present => ConsumeRowPresence(node, true),
            TokenType.Missing => ConsumeRowPresence(node, false),
            TokenType.Word when IsContextualKeyword("present") => ConsumeContextualRowPresence(node, true),
            TokenType.Word when IsContextualKeyword("missing") => ConsumeContextualRowPresence(node, false),
            TokenType.Identifier when IsContextualKeyword("present") => ConsumeContextualRowPresence(node, true),
            TokenType.Identifier when IsContextualKeyword("missing") => ConsumeContextualRowPresence(node, false),
            TokenType.Null => ComposeAndSkip(_ => new IsNullNode(node, false), TokenType.Null),
            _ => throw ParserDiagnosticFacts.MissingToken("IS must be followed by NULL, DISTINCT FROM, PRESENT, or MISSING.",
                _lexer.AlreadyResolvedQueryPart, new TextSpan(Current.Span.Start, 0))
        };
    }

    private IsDistinctFromNode ComposeIsDistinctFrom(Node left, bool isNegated)
    {
        Consume(TokenType.Distinct);
        Consume(TokenType.From);
        ThrowIfMissingRightOperand(isNegated ? "IS NOT DISTINCT FROM" : "IS DISTINCT FROM");
        return new IsDistinctFromNode(left, ComposeEqualityOperators(), isNegated);
    }

    private void ConsumeIsNullToken(string phrase)
    {
        if (Current.TokenType != TokenType.Null)
            throw ParserDiagnosticFacts.MissingToken($"{phrase} must be followed by NULL or DISTINCT FROM.",
                _lexer.AlreadyResolvedQueryPart, new TextSpan(Current.Span.Start, 0));
        Consume(TokenType.Null);
    }

    private RowPresenceNode ConsumeContextualRowPresence(Node node, bool isPresent)
    {
        var presenceToken = ConsumeAndGetToken();
        return (RowPresenceNode)new RowPresenceNode(node, isPresent)
            .WithSpan(node.Span.Through(presenceToken.Span));
    }

    private RowPresenceNode ConsumeRowPresence(Node node, bool isPresent)
    {
        var presenceToken = ConsumeAndGetToken();
        return (RowPresenceNode)new RowPresenceNode(node, isPresent)
            .WithSpan(node.Span.Through(presenceToken.Span));
    }
}
