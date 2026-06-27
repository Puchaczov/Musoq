using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private DerivedTableFromNode ComposeDerivedTableFrom(bool isApplyContext)
    {
        var opening = ConsumeAndGetToken(TokenType.LeftParenthesis);

        if (Current.TokenType != TokenType.Select && Current.TokenType != TokenType.From && Current.TokenType != TokenType.Pivot && Current.TokenType != TokenType.Unpivot && Current.TokenType != TokenType.With)
            throw new SyntaxException(
                "Derived table requires a SELECT, FROM, PIVOT, UNPIVOT, or WITH query inside parentheses.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2024_InvalidSubquery,
                opening.Span);

        var query = Current.TokenType == TokenType.With
            ? ComposeCteExpression()
            : ComposeSetOperators(1);
        var closing = ConsumeAndGetToken(TokenType.RightParenthesis);
        var (alias, aliasSpan) = ComposeAlias();

        if (string.IsNullOrWhiteSpace(alias))
            throw new SyntaxException(
                "Derived table requires an alias after the closing parenthesis.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2022_InvalidAlias,
                closing.Span);

        RegisterFromAlias(alias);

        var derived = new DerivedTableFromNode(query, alias, isApplyContext);
        derived.WithSpan(opening.Span.Through(closing.Span));
        if (!aliasSpan.IsEmpty)
            derived.WithFullSpan(derived.Span.Through(aliasSpan));
        return derived;
    }
}
