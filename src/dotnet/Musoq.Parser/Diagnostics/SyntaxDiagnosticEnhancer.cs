using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Enriches syntax and lexer diagnostics with user-facing guidance for mistyped keywords,
///     unsupported dialect constructs, and malformed input.
/// </summary>
internal static partial class SyntaxDiagnosticEnhancer
{
    private static readonly string[] StatementStarts = ["PARAM", "LET", "SELECT", "FROM", "WITH", "DESC", "TABLE", "COUPLE"];

    private static readonly string[] KnownKeywords =
    [
        "SELECT", "FROM", "WHERE", "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC", "TAKE", "SKIP", "WITH",
        "AS", "JOIN", "INNER", "OUTER", "CROSS", "APPLY", "ON", "CASE", "WHEN", "THEN", "ELSE", "END", "IN",
        "NOT", "NULL", "UNION", "EXCEPT", "INTERSECT", "DISTINCT", "PARAM", "LET", "TABLE", "COUPLE", "LIKE"
    ];

    private static readonly FrozenDictionary<string, DialectKeywordHelp> DialectKeywordHelpMap =
        new Dictionary<string, DialectKeywordHelp>(StringComparer.OrdinalIgnoreCase)
        {
            ["LIMIT"] = new(
                "Musoq uses TAKE instead of LIMIT.",
                [
                    "Replace LIMIT n with TAKE n.",
                    "Example: SELECT Name FROM #schema.method() alias TAKE 5"
                ],
                "Core Spec §TAKE / SKIP"),
            ["OFFSET"] = new(
                "Musoq uses SKIP instead of OFFSET.",
                [
                    "Replace OFFSET n with SKIP n.",
                    "If you need paging, use ORDER BY ... TAKE ... SKIP ..."
                ],
                "Core Spec §TAKE / SKIP"),
            ["TOP"] = new(
                "Musoq does not use TOP in the SELECT list. Use TAKE after the FROM clause instead.",
                [
                    "Rewrite SELECT TOP 5 ... as SELECT ... FROM ... TAKE 5.",
                    "Keep TAKE near the end of the query after FROM / ORDER BY."
                ],
                "Core Spec §TAKE / SKIP"),
            ["FIRST"] = new(
                "Musoq does not use FIRST in the SELECT list. Use TAKE after the FROM clause instead.",
                [
                    "Rewrite SELECT FIRST 5 ... as SELECT ... FROM ... TAKE 5.",
                    "Keep TAKE near the end of the query after FROM / ORDER BY."
                ],
                "Core Spec §TAKE / SKIP"),
            ["FETCH"] = new(
                "Musoq does not support SQL Server OFFSET/FETCH paging syntax. Use TAKE and SKIP instead.",
                [
                    "Replace OFFSET ... FETCH ... with TAKE ... SKIP ...",
                    "Example: SELECT ... ORDER BY Name TAKE 5 SKIP 3"
                ],
                "Core Spec §TAKE / SKIP"),
            ["ROWS"] = new(
                "Musoq does not support SQL Server OFFSET/FETCH ROWS syntax. Use TAKE and SKIP instead.",
                [
                    "Remove ROWS/ONLY keywords and rewrite with TAKE / SKIP.",
                    "Example: SELECT ... ORDER BY Name TAKE 5 SKIP 3"
                ],
                "Core Spec §TAKE / SKIP"),
            ["NEXT"] = new(
                "Musoq does not support SQL Server FETCH NEXT syntax. Use TAKE and SKIP instead.",
                [
                    "Rewrite FETCH NEXT n ROWS ONLY as TAKE n.",
                    "Combine with SKIP if you need offset paging."
                ],
                "Core Spec §TAKE / SKIP"),
            ["ONLY"] = new(
                "Musoq does not support SQL Server FETCH ... ONLY syntax. Use TAKE and SKIP instead.",
                [
                    "Remove ONLY and rewrite the paging clause with TAKE / SKIP.",
                    "Example: SELECT ... ORDER BY Name TAKE 5 SKIP 3"
                ],
                "Core Spec §TAKE / SKIP"),
            ["ILIKE"] = new(
                "Musoq uses LIKE for pattern matching. ILIKE (case-insensitive LIKE) is a PostgreSQL extension not supported in Musoq.",
                [
                    "Replace ILIKE with LIKE.",
                    "For case-insensitive matching, use: WHERE ToLower(Name) LIKE '%value%'"
                ],
                "Core Spec §LIKE Operator")
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static Diagnostic CreateDiagnostic(
        DiagnosticCode code,
        string message,
        TextSpan span,
        Token? currentToken,
        SourceText? sourceText)
    {
        var metadata = ErrorMetadataCatalog.Get(code);
        var keywordCandidate = GetKeywordCandidate(currentToken, sourceText, span);
        var keywordSuggestion = GetKeywordSuggestion(currentToken, sourceText, span);
        var dialectHelp = GetDialectKeywordHelp(currentToken, sourceText);
        var enhancedMessage = code == DiagnosticCode.MQ2035_MissingRequiredAlias
            ? message
            : BuildMessage(message, span, keywordCandidate, keywordSuggestion, dialectHelp);
        var explanation = BuildExplanation(metadata, keywordCandidate, keywordSuggestion, dialectHelp);
        var docsReference = dialectHelp?.DocsReference ?? metadata?.DocsReference;
        var suggestedFixes = BuildSuggestedFixes(metadata, keywordCandidate, span, keywordSuggestion, dialectHelp);
        var (location, endLocation) = GetLocations(sourceText, span);
        var contextSnippet = sourceText?.GetContextSnippet(span);

        return new Diagnostic(
            code,
            ErrorCatalog.GetDefaultSeverity(code),
            enhancedMessage,
            location,
            endLocation,
            contextSnippet,
            suggestedFixes: suggestedFixes,
            explanation: explanation,
            docsReference: docsReference);
    }

    public static Diagnostic EnhanceLexerDiagnostic(
        DiagnosticCode code,
        string message,
        TextSpan span,
        SourceText? sourceText,
        IEnumerable<string>? relatedInfo = null)
    {
        var metadata = ErrorMetadataCatalog.Get(code);
        var (location, endLocation) = GetLocations(sourceText, span);
        var contextSnippet = sourceText?.GetContextSnippet(span);
        var suggestedFixes = metadata?.SuggestedFixes.Select(DiagnosticAction.Suggestion) ?? [];

        return new Diagnostic(
            code,
            ErrorCatalog.GetDefaultSeverity(code),
            message,
            location,
            endLocation,
            contextSnippet,
            relatedInfo: relatedInfo,
            suggestedFixes: suggestedFixes,
            explanation: metadata?.Explanation,
            docsReference: metadata?.DocsReference);
    }

    private sealed record DialectKeywordHelp(string Message, string[] SuggestedFixes, string DocsReference);
}
