using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private ParsedSource ComposeDerivedTableFrom(SourceParseContext context)
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
        var aliasResult = ComposeAlias(AliasContext.DerivedSource);
        EnsureAliasSyntax(aliasResult, AliasContext.DerivedSource);
        var alias = aliasResult.Alias;
        var aliasSpan = aliasResult.Span;

        if (string.IsNullOrWhiteSpace(alias))
            throw MissingRequiredAliasAfterDelimiter(
                "derived table",
                "the closing parenthesis",
                _lexer.AlreadyResolvedQueryPart,
                closing.Span);

        RegisterFromAlias(alias);

        var derived = new DerivedTableFromNode(query, alias, context == SourceParseContext.ApplyRight);
        derived.WithSpan(opening.Span.Through(closing.Span));
        if (!aliasSpan.IsEmpty)
            derived.WithFullSpan(derived.Span.Through(aliasSpan));
        return ParsedSource.Create(derived, SourceKind.Derived, opening.Span.Start, closing.Span, aliasResult);
    }
}
