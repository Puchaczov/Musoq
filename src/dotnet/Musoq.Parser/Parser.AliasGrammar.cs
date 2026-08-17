using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal enum AliasParseState
{
    Absent,
    Implicit,
    Explicit,
    MissingAfterAs,
    InvalidAfterAs
}

internal enum AliasContext
{
    Projection,
    Source,
    DerivedSource,
    ValuesSource,
    Pivot,
    Unpivot,
    Description
}

internal readonly record struct AliasParseResult(
    string Alias,
    TextSpan Span,
    AliasParseState State,
    TextSpan IntroducerSpan)
{
    public bool HasAlias => State is AliasParseState.Implicit or AliasParseState.Explicit;
}

public partial class Parser
{
    private AliasParseResult ComposeAlias(AliasContext context)
    {
        if (Current.TokenType == TokenType.As)
        {
            var asToken = ConsumeAndGetToken(TokenType.As);
            var isActiveBoundary = !IsBracketedIdentifier(Current) && AliasTokenFacts.IsBoundary(Current, context);
            if (AliasTokenFacts.IsExplicitAliasToken(Current.TokenType) && !isActiveBoundary)
            {
                var aliasToken = ConsumeAndGetToken(Current.TokenType);
                return new AliasParseResult(aliasToken.Value, aliasToken.Span, AliasParseState.Explicit,
                    asToken.Span);
            }

            var state = isActiveBoundary
                ? AliasParseState.MissingAfterAs
                : AliasParseState.InvalidAfterAs;
            var span = state == AliasParseState.MissingAfterAs
                ? new TextSpan(Current.Span.Start, 0)
                : Current.Span;
            return new AliasParseResult(string.Empty, span, state, asToken.Span);
        }

        if (!AliasTokenFacts.IsAliasToken(Current.TokenType))
            return new AliasParseResult(string.Empty, default, AliasParseState.Absent, default);

        if (Current.TokenType == TokenType.Identifier && !IsBracketedIdentifier(Current) &&
            IsLikelyMisspelledClauseKeyword(Current.Value))
            return new AliasParseResult(string.Empty, default, AliasParseState.Absent, default);

        var token = ConsumeAndGetToken(Current.TokenType);
        return new AliasParseResult(token.Value, token.Span, AliasParseState.Implicit, default);
    }

    private bool IsBracketedIdentifier(Token token)
    {
        return token.Span.Start < _lexer.Input.Length && _lexer.Input[token.Span.Start] == '[';
    }

    private void EnsureAliasSyntax(AliasParseResult result, AliasContext context)
    {
        if (result.State is not (AliasParseState.MissingAfterAs or AliasParseState.InvalidAfterAs))
            return;

        var message = result.State == AliasParseState.MissingAfterAs
            ? $"The {AliasTokenFacts.GetRequiredAliasContextName(context)} requires an alias identifier after AS."
            : $"The token '{Current.Value}' cannot be used as an alias in this position.";

        var isRequiredSourceAlias = result.State == AliasParseState.MissingAfterAs &&
                                     context is AliasContext.Source or AliasContext.DerivedSource or AliasContext.ValuesSource;

        throw new SyntaxException(message, _lexer.AlreadyResolvedQueryPart,
            isRequiredSourceAlias ? DiagnosticCode.MQ2035_MissingRequiredAlias : DiagnosticCode.MQ2022_InvalidAlias,
            isRequiredSourceAlias ? new TextSpan(result.IntroducerSpan.End, 0) : result.Span);
    }
}

internal static class AliasTokenFacts
{
    public static bool IsAliasToken(TokenType tokenType)
    {
        return tokenType is TokenType.Word or TokenType.Identifier;
    }

    public static bool IsExplicitAliasToken(TokenType tokenType)
    {
        return IsAliasToken(tokenType) || tokenType == TokenType.StringLiteral ||
               tokenType is TokenType.Exists or TokenType.Any or TokenType.Some or TokenType.All;
    }

    public static bool IsBoundary(Token token, AliasContext context)
    {
        if (token.TokenType is TokenType.EndOfFile or TokenType.Semicolon or TokenType.RightParenthesis)
            return true;

        if (token.TokenType is TokenType.From or TokenType.Where or TokenType.GroupBy or TokenType.Having
            or TokenType.OrderBy or TokenType.Skip or TokenType.Take or TokenType.Window or TokenType.Qualify
            or TokenType.On or TokenType.With or TokenType.InnerJoin or TokenType.OuterJoin
            or TokenType.SemiJoin or TokenType.AntiJoin or TokenType.CrossJoin or TokenType.CrossApply
            or TokenType.OuterApply or TokenType.AsOfJoin or TokenType.Union or TokenType.UnionAll
            or TokenType.Except or TokenType.Intersect or TokenType.Comma)
            return true;

        if (token.TokenType is TokenType.Identifier or TokenType.Word)
            return context is AliasContext.Source or AliasContext.DerivedSource or AliasContext.ValuesSource
                ? IsContextualBoundary(token.Value)
                : false;

        return false;
    }

    public static string GetContextName(AliasContext context)
    {
        return context switch
        {
            AliasContext.Projection => "projection",
            AliasContext.Source => "source",
            AliasContext.DerivedSource => "derived table",
            AliasContext.ValuesSource => "VALUES source",
            AliasContext.Pivot => "PIVOT value",
            AliasContext.Unpivot => "UNPIVOT entry",
            AliasContext.Description => "description source",
            _ => "source"
        };
    }

    public static string GetRequiredAliasContextName(AliasContext context)
    {
        return context switch
        {
            AliasContext.Source => "source",
            AliasContext.DerivedSource => "derived table source",
            AliasContext.ValuesSource => "VALUES source",
            _ => GetContextName(context)
        };
    }

    private static bool IsContextualBoundary(string value)
    {
        return value.Equals("rows", StringComparison.OrdinalIgnoreCase)
               || value.Equals("range", StringComparison.OrdinalIgnoreCase)
               || value.Equals("qualify", StringComparison.OrdinalIgnoreCase)
               || value.Equals("using", StringComparison.OrdinalIgnoreCase)
               || value.Equals("keep", StringComparison.OrdinalIgnoreCase)
               || value.Equals("on", StringComparison.OrdinalIgnoreCase)
               || value.Equals("tie", StringComparison.OrdinalIgnoreCase)
               || value.Equals("union", StringComparison.OrdinalIgnoreCase)
               || value.Equals("except", StringComparison.OrdinalIgnoreCase)
               || value.Equals("intersect", StringComparison.OrdinalIgnoreCase);
    }
}
