using System.Collections.Generic;
using System.Globalization;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

internal static class ParserDiagnosticFacts
{
    public static SyntaxException MissingToken(string message, string queryPart, TextSpan span) =>
        new(message, queryPart, DiagnosticCode.MQ2002_MissingToken, span);

    public static SyntaxException EmptyPredicateList(string message, string queryPart, TextSpan span) =>
        new(message, queryPart, DiagnosticCode.MQ2037_EmptyPredicateListNotAllowed, span);

    public static void ThrowIfMissingRightOperand(Token currentToken, string operatorText, string queryPart)
    {
        if (currentToken.TokenType is not (TokenType.EndOfFile or TokenType.Semicolon or TokenType.RightParenthesis or TokenType.Comma or
            TokenType.From or TokenType.Where or TokenType.GroupBy or TokenType.Having or TokenType.OrderBy or TokenType.Take or
            TokenType.Skip or TokenType.Window or TokenType.Qualify or TokenType.Union or TokenType.UnionAll or TokenType.Except or
            TokenType.Intersect or TokenType.And or TokenType.Or or TokenType.Then or TokenType.Else or TokenType.End or TokenType.When or
            TokenType.On or TokenType.As or TokenType.InnerJoin or TokenType.OuterJoin or TokenType.SemiJoin or TokenType.AntiJoin or
            TokenType.CrossJoin or TokenType.CrossApply or TokenType.OuterApply or TokenType.AsOfJoin))
            return;

        throw new SyntaxException($"Operator '{operatorText}' is missing its right operand.", queryPart,
            DiagnosticCode.MQ2020_MissingOperand, new TextSpan(currentToken.Span.Start, 0));
    }

    public static IReadOnlyDictionary<string, string> PositionalAfterNamed(int argumentIndex) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["argumentKind"] = "positional-after-named",
            ["argumentIndex"] = argumentIndex.ToString(CultureInfo.InvariantCulture)
        };

    public static IReadOnlyDictionary<string, string> NamedOutsideDatasource(string argument) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["argumentKind"] = "named-outside-datasource",
            ["argument"] = argument
        };

    public static IReadOnlyDictionary<string, string> InvalidArgumentName(string argument) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["argumentKind"] = "invalid-name",
            ["argument"] = argument
        };

    public static IReadOnlyDictionary<string, string> MissingArgumentValue(string argument) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["argumentKind"] = "missing-value",
            ["argument"] = argument
        };

    public static IReadOnlyDictionary<string, string> AliasSyntax(
        AliasParseResult result,
        AliasContext context,
        string token)
    {
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["aliasKind"] = result.State == AliasParseState.MissingAfterAs
                ? "missing-after-as"
                : "invalid-after-as",
            ["context"] = AliasTokenFacts.GetContextName(context)
        };

        if (!string.IsNullOrWhiteSpace(token))
            arguments["token"] = token;

        return arguments;
    }

    public static IReadOnlyDictionary<string, string> RequiredAlias(
        SourceKind sourceKind,
        string operatorText,
        string boundary,
        bool isFirstSource) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["aliasKind"] = "required-source-alias",
            ["sourceKind"] = sourceKind.ToString().ToLowerInvariant(),
            ["operator"] = operatorText,
            ["boundary"] = boundary,
            ["isFirstSource"] = isFirstSource ? "true" : "false"
        };

    public static IReadOnlyDictionary<string, string> RequiredAliasAfterDelimiter(
        string sourceKind,
        string delimiter) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["aliasKind"] = "required-source-alias",
            ["sourceKind"] = sourceKind,
            ["boundary"] = delimiter
        };

    public static Diagnostic ApplyExceptionPayload(Diagnostic diagnostic, SyntaxException exception)
    {
        foreach (var argument in exception.Arguments)
            diagnostic = diagnostic.WithArgument(argument.Key, argument.Value);

        foreach (var suggestedFix in exception.SuggestedFixes)
            diagnostic = diagnostic.WithSuggestedFix(suggestedFix);

        return diagnostic;
    }
}
