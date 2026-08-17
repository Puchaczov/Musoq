using System.Collections.Generic;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Creates enriched syntax and lexer diagnostics for consumers outside the parser implementation.
/// </summary>
public static class SyntaxDiagnosticFactory
{
    /// <summary>
    ///     Creates a syntax diagnostic with contextual suggestions and metadata.
    /// </summary>
    public static Diagnostic CreateDiagnostic(
        DiagnosticCode code,
        string message,
        TextSpan span,
        Token? currentToken,
        SourceText? sourceText)
    {
        return SyntaxDiagnosticEnhancer.CreateDiagnostic(code, message, span, currentToken, sourceText);
    }

    /// <summary>
    ///     Creates an enriched lexer diagnostic with related information and metadata.
    /// </summary>
    public static Diagnostic EnhanceLexerDiagnostic(
        DiagnosticCode code,
        string message,
        TextSpan span,
        SourceText? sourceText,
        IEnumerable<string>? relatedInfo = null)
    {
        return SyntaxDiagnosticEnhancer.EnhanceLexerDiagnostic(code, message, span, sourceText, relatedInfo);
    }
}
