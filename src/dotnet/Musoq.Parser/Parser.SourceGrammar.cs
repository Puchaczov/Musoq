using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal enum SourceParseContext
{
    Primary,
    JoinRight,
    ApplyRight
}

internal enum SourceKind
{
    Referential,
    InMemory,
    Schema,
    Function,
    AccessMethod,
    Property,
    Derived,
    Values,
    Composite
}

internal readonly record struct ParsedSource(
    FromNode Node,
    SourceKind Kind,
    TextSpan Extent,
    TextSpan SourceEndSpan,
    AliasParseResult Alias,
    string? NaturalAlias)
{
    public bool HasExplicitAlias => Alias.HasAlias;

    public bool HasNaturalAlias => !string.IsNullOrWhiteSpace(NaturalAlias);

    public bool HasStableName => Kind == SourceKind.Composite || HasExplicitAlias || HasNaturalAlias;

    public static ParsedSource Create(
        FromNode node,
        SourceKind kind,
        int sourceStart,
        TextSpan sourceEndSpan,
        AliasParseResult alias,
        string? naturalAlias = null)
    {
        var extentEnd = alias.HasAlias ? alias.Span.End : sourceEndSpan.End;
        return new ParsedSource(
            node,
            kind,
            TextSpan.FromBounds(sourceStart, extentEnd),
            sourceEndSpan,
            alias,
            naturalAlias);
    }

    public static ParsedSource Composite(FromNode node)
    {
        return Create(node, SourceKind.Composite, node.Span.Start, node.Span, default);
    }
}

public partial class Parser
{
    private void EnsureStableAlias(ParsedSource source, string operatorText, bool isFirstSource)
    {
        if (source.HasStableName)
            return;

        var message = isFirstSource
            ? $"The first source in a multi-source query requires an alias before {operatorText}."
            : $"The {operatorText} source requires an alias before {GetSourceBoundaryText()}.";

        throw new SyntaxException(
            message,
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2035_MissingRequiredAlias,
            new TextSpan(source.SourceEndSpan.End, 0));
    }

    private static string GetJoinOperatorText(Token token)
    {
        return token switch
        {
            OuterJoinToken outer => outer.Type switch
            {
                OuterJoinType.Left => "LEFT OUTER JOIN",
                OuterJoinType.Right => "RIGHT OUTER JOIN",
                OuterJoinType.Full => "FULL OUTER JOIN",
                _ => "OUTER JOIN"
            },
            AsOfJoinToken asOf => asOf.IsLeft ? "ASOF LEFT JOIN" : "ASOF JOIN",
            _ => token.TokenType switch
            {
                TokenType.InnerJoin => "INNER JOIN",
                TokenType.SemiJoin => "SEMI JOIN",
                TokenType.AntiJoin => "ANTI JOIN",
                TokenType.CrossJoin => "CROSS JOIN",
                TokenType.CrossApply => "CROSS APPLY",
                TokenType.OuterApply => "OUTER APPLY",
                _ => token.Value.ToUpperInvariant()
            }
        };
    }

    private string GetSourceBoundaryText()
    {
        return Current.TokenType switch
        {
            TokenType.On => "ON",
            TokenType.With => "WITH ORDINALITY",
            TokenType.Where => "WHERE",
            TokenType.GroupBy => "GROUP BY",
            TokenType.Having => "HAVING",
            TokenType.Window => "WINDOW",
            TokenType.Qualify => "QUALIFY",
            TokenType.OrderBy => "ORDER BY",
            TokenType.Skip => "SKIP",
            TokenType.Take => "TAKE",
            TokenType.InnerJoin or TokenType.OuterJoin or TokenType.SemiJoin or TokenType.AntiJoin
                or TokenType.CrossJoin or TokenType.CrossApply or TokenType.OuterApply or TokenType.AsOfJoin
                => GetJoinOperatorText(Current),
            TokenType.Union => "UNION",
            TokenType.UnionAll => "UNION ALL",
            TokenType.Except => "EXCEPT",
            TokenType.Intersect => "INTERSECT",
            TokenType.RightParenthesis => ")",
            TokenType.Semicolon => ";",
            TokenType.EndOfFile => "the end of the statement",
            _ when !string.IsNullOrWhiteSpace(Current.Value) => Current.Value.ToUpperInvariant(),
            _ => "the next clause"
        };
    }

    private static bool IsJoinConditionBoundary(TokenType tokenType)
    {
        return tokenType is TokenType.EndOfFile or TokenType.Semicolon or TokenType.RightParenthesis
            or TokenType.Where or TokenType.GroupBy or TokenType.Having or TokenType.Window or TokenType.Qualify
            or TokenType.OrderBy or TokenType.Skip or TokenType.Take or TokenType.With or TokenType.Union
            or TokenType.UnionAll or TokenType.Except or TokenType.Intersect or TokenType.InnerJoin
            or TokenType.OuterJoin or TokenType.SemiJoin or TokenType.AntiJoin or TokenType.CrossJoin
            or TokenType.CrossApply or TokenType.OuterApply or TokenType.AsOfJoin;
    }

    private void EnsureJoinCondition(string operatorText)
    {
        if (Current.TokenType != TokenType.On)
            throw CreateInvalidJoinCondition(operatorText, Current.Span);

        Consume(TokenType.On);
        if (IsJoinConditionBoundary(Current.TokenType))
            throw CreateInvalidJoinCondition(operatorText, Current.Span);
    }

    private SyntaxException CreateInvalidJoinCondition(string operatorText, TextSpan span)
    {
        return new SyntaxException(
            $"The {operatorText} requires an ON condition.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            span);
    }

    private static SyntaxException MissingRequiredAliasAfterDelimiter(
        string sourceKind,
        string delimiter,
        string queryPart,
        TextSpan position)
    {
        return new SyntaxException(
            $"The {sourceKind} source requires an alias after {delimiter}.",
            queryPart,
            DiagnosticCode.MQ2035_MissingRequiredAlias,
            new TextSpan(position.End, 0));
    }
}
