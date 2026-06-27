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

            Consume(TokenType.Null);
            return new IsNullNode(node, true);
        }

        return Current.TokenType switch
        {
            TokenType.Distinct => ComposeIsDistinctFrom(node, false),
            TokenType.Present => ComposeAndSkip(_ => new RowPresenceNode(node, true), TokenType.Present),
            TokenType.Missing => ComposeAndSkip(_ => new RowPresenceNode(node, false), TokenType.Missing),
            TokenType.Word when IsContextualKeyword("present") => ConsumeContextualRowPresence(node, true),
            TokenType.Word when IsContextualKeyword("missing") => ConsumeContextualRowPresence(node, false),
            TokenType.Identifier when IsContextualKeyword("present") => ConsumeContextualRowPresence(node, true),
            TokenType.Identifier when IsContextualKeyword("missing") => ConsumeContextualRowPresence(node, false),
            _ => ComposeAndSkip(_ => new IsNullNode(node, false), TokenType.Null)
        };
    }

    private IsDistinctFromNode ComposeIsDistinctFrom(Node left, bool isNegated)
    {
        Consume(TokenType.Distinct);
        Consume(TokenType.From);
        return new IsDistinctFromNode(left, ComposeEqualityOperators(), isNegated);
    }

    private RowPresenceNode ConsumeContextualRowPresence(Node node, bool isPresent)
    {
        ConsumeAndGetToken();
        return new RowPresenceNode(node, isPresent);
    }
}
