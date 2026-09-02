using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private FieldOrderedNode[] ComposeOrderedFields()
    {
        var fields = new List<FieldOrderedNode>();
        var i = 0;

        if (Current.TokenType == TokenType.Comma)
            throw InvalidOrderByList(
                DiagnosticCode.MQ2015_LeadingComma,
                "ORDER BY list has a leading comma. Add an expression before the comma or remove it.",
                Current.Span);

        fields.Add(ConsumeFieldOrdered(i++));
        while (Current.TokenType == TokenType.Comma)
        {
            var commaSpan = Current.Span;
            Consume(TokenType.Comma);
            if (Current.TokenType == TokenType.Comma)
                throw InvalidOrderByList(
                    DiagnosticCode.MQ2015_LeadingComma,
                    "ORDER BY list has a leading comma. Add an expression before the comma or remove it.",
                    Current.Span);
            if (IsSetOperator(Current.TokenType) ||
                Current.TokenType is TokenType.EndOfFile or TokenType.RightParenthesis or TokenType.Skip or TokenType.Take ||
                IsContextualKeyword("rows") || IsContextualKeyword("range"))
                throw InvalidOrderByList(
                    DiagnosticCode.MQ2014_TrailingComma,
                    "ORDER BY list has a trailing comma. Add another expression or remove the comma.",
                    commaSpan);
            fields.Add(ConsumeFieldOrdered(i++));
        }

        return fields.ToArray();
    }

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
            if (Current.TokenType == TokenType.EndOfFile)
                throw new SyntaxException(
                    "The WHERE clause ends unexpectedly at end of input.",
                    _lexer.AlreadyResolvedQueryPart,
                    DiagnosticCode.MQ2017_UnexpectedEndOfFile,
                    new TextSpan(Current.Span.Start, 0));
            return new WhereNode(ComposeOperations());
        }

        if (withoutWhereToken)
            return new WhereNode(ComposeOperations());

        return null;
    }
}
