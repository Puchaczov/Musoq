using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private static bool IsLikelyMisspelledClauseKeyword(string identifier)
    {
        var maxDistance = identifier.Length >= MinLengthForLargerDistance ? LongWordMaxDistance : ShortWordMaxDistance;

        return ErrorCatalog.GetDidYouMeanSuggestion(identifier, ClauseKeywords, maxDistance: maxDistance) != null;
    }

    private WhereNode? ComposeWhere(bool withoutWhereToken)
    {
        if (Current.TokenType == TokenType.Where)
        {
            Consume(TokenType.Where);
            return new WhereNode(ComposeOperations());
        }

        if (withoutWhereToken)
            return new WhereNode(ComposeOperations());

        return null;
    }
}
