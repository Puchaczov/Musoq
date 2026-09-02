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
        "NOT", "NULL", "UNION", "ALL", "EXCEPT", "INTERSECT", "DISTINCT", "PARAM", "LET", "TABLE", "COUPLE", "LIKE"
    ];

    public static Diagnostic CreateDiagnostic(
        DiagnosticCode code,
        string message,
        TextSpan span,
        Token? currentToken,
        SourceText? sourceText)
    {
        var metadata = ErrorMetadataCatalog.Get(code);
        var allowKeywordEnhancement = code is DiagnosticCode.MQ2001_UnexpectedToken or
            DiagnosticCode.MQ2004_MissingFromClause or DiagnosticCode.MQ2030_UnsupportedSyntax;
        var allowDialectEnhancement = allowKeywordEnhancement || code is DiagnosticCode.MQ2009_InvalidOrderByExpression or
            DiagnosticCode.MQ2021_UnclosedFunctionCall;
        var keywordCandidate = allowKeywordEnhancement
            ? GetKeywordCandidate(currentToken, sourceText, span)
            : null;
        var keywordSuggestion = allowKeywordEnhancement
            ? GetKeywordSuggestion(currentToken, sourceText, span)
            : null;
        var dialectHelp = allowDialectEnhancement
            ? GetDialectKeywordHelp(currentToken, sourceText, span)
            : null;
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
