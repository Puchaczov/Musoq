using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Lexing;

namespace Musoq.Parser.Exceptions;

/// <summary>
///     Exception thrown when a syntax error is encountered during parsing.
///     Provides detailed location and context information for error reporting.
/// </summary>
public class SyntaxException : Exception, IDiagnosticException
{
    public SyntaxException(string message, Exception innerException)
        : base(message, innerException)
    {
        QueryPart = string.Empty;
        Code = DiagnosticCode.MQ2001_UnexpectedToken;
    }

    public SyntaxException(string message)
        : base(message)
    {
        QueryPart = string.Empty;
        Code = DiagnosticCode.MQ2001_UnexpectedToken;
    }

    public SyntaxException()
    {
        QueryPart = string.Empty;
        Code = DiagnosticCode.MQ2001_UnexpectedToken;
    }

    /// <summary>
    ///     Initializes a new instance of SyntaxException with the specified message and query part.
    /// </summary>
    public SyntaxException(string message, string queryPart)
        : base(message)
    {
        QueryPart = queryPart;
        Code = DiagnosticCode.MQ2001_UnexpectedToken;
    }

    /// <summary>
    ///     Initializes a new instance of SyntaxException with the specified message, query part, and inner exception.
    ///     If the inner exception is a <see cref="LexerException" />, its diagnostic code and span are preserved
    ///     so that lexer-level diagnostics (e.g. unterminated strings, invalid literals) survive parser wrapping.
    /// </summary>
    public SyntaxException(string message, string queryPart, Exception innerException)
        : base(message, innerException)
    {
        QueryPart = queryPart;
        Code = innerException is LexerException lexerException
            ? lexerException.Code
            : DiagnosticCode.MQ2001_UnexpectedToken;

        if (innerException is LexerException { Span: not null } lexerWithSpan)
            Span = lexerWithSpan.Span;
    }

    /// <summary>
    ///     Initializes a new instance of SyntaxException with full diagnostic information.
    /// </summary>
    public SyntaxException(string message, string queryPart, DiagnosticCode code, TextSpan span)
        : base(message)
    {
        QueryPart = queryPart;
        Code = code;
        Span = span;
    }

    /// <summary>
    ///     Initializes a new instance of SyntaxException with full diagnostic information and inner exception.
    /// </summary>
    public SyntaxException(string message, string queryPart, DiagnosticCode code, TextSpan span,
        Exception innerException)
        : base(message, innerException)
    {
        QueryPart = queryPart;
        Code = code;
        Span = span;
    }

    /// <summary>
    ///     Initializes a new instance of SyntaxException with full diagnostic information,
    ///     structured facts, and optional explicit actions.
    /// </summary>
    public SyntaxException(
        string message,
        string queryPart,
        DiagnosticCode code,
        TextSpan span,
        IEnumerable<KeyValuePair<string, string>>? arguments,
        IEnumerable<DiagnosticAction>? suggestedFixes = null)
        : base(message)
    {
        QueryPart = queryPart;
        Code = code;
        Span = span;
        Arguments = arguments != null
            ? new Dictionary<string, string>(arguments, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        SuggestedFixes = suggestedFixes != null
            ? [..suggestedFixes]
            : [];
    }

    /// <summary>
    ///     Gets the portion of the query that caused the syntax error.
    /// </summary>
    public string QueryPart { get; }

    /// <summary>
    ///     Gets the diagnostic code for this syntax error.
    /// </summary>
    public DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the source location span where this error occurred.
    /// </summary>
    public TextSpan? Span { get; }

    /// <summary>
    ///     Gets stable string-valued facts associated with this syntax error.
    /// </summary>
    public IReadOnlyDictionary<string, string> Arguments { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    ///     Gets explicit actions associated with this syntax error.
    /// </summary>
    public IReadOnlyList<DiagnosticAction> SuggestedFixes { get; } = [];

    /// <summary>
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        var effectiveSourceText = sourceText;
        if (effectiveSourceText == null && !string.IsNullOrWhiteSpace(QueryPart))
            effectiveSourceText = new SourceText(QueryPart);

        var numericCode = (int)Code;
        var diagnostic = numericCode is >= 1001 and <= 1009 || Code == DiagnosticCode.MQ2011_MissingClosingBracket
            ? SyntaxDiagnosticEnhancer.EnhanceLexerDiagnostic(Code, Message, span, effectiveSourceText)
            : SyntaxDiagnosticEnhancer.CreateDiagnostic(Code, Message, span, currentToken: null, effectiveSourceText);

        foreach (var argument in Arguments)
            diagnostic = diagnostic.WithArgument(argument.Key, argument.Value);

        foreach (var suggestedFix in SuggestedFixes)
            diagnostic = diagnostic.WithSuggestedFix(suggestedFix);

        return diagnostic;
    }

    /// <summary>
    ///     Creates a SyntaxException for an unexpected token.
    /// </summary>
    public static SyntaxException UnexpectedToken(string found, string expected, string queryPart, TextSpan span)
    {
        var message = string.IsNullOrEmpty(expected)
            ? $"Unexpected token '{found}'."
            : $"Unexpected token '{found}', expected '{expected}'.";
        return new SyntaxException(message, queryPart, DiagnosticCode.MQ2001_UnexpectedToken, span);
    }

    /// <summary>
    ///     Creates a SyntaxException for a missing token.
    /// </summary>
    public static SyntaxException MissingToken(string expected, string queryPart, TextSpan span)
    {
        var message = $"Missing expected token '{expected}'.";
        return new SyntaxException(message, queryPart, DiagnosticCode.MQ2002_MissingToken, span);
    }

    /// <summary>
    ///     Creates a SyntaxException for an invalid expression.
    /// </summary>
    public static SyntaxException InvalidExpression(string context, string queryPart, TextSpan span)
    {
        var message = $"Invalid expression{(string.IsNullOrEmpty(context) ? "" : $": {context}")}.";
        return new SyntaxException(message, queryPart, DiagnosticCode.MQ2003_InvalidExpression, span);
    }

    /// <summary>
    ///     Creates a SyntaxException for an unclosed string literal.
    /// </summary>
    public static SyntaxException UnclosedString(string queryPart, TextSpan span)
    {
        return new SyntaxException("Unclosed string literal.", queryPart, DiagnosticCode.MQ1002_UnterminatedString,
            span);
    }

    /// <summary>
    ///     Creates a SyntaxException for an unclosed bracket.
    /// </summary>
    public static SyntaxException UnclosedBracket(string bracket, string queryPart, TextSpan span)
    {
        var message = $"Unclosed '{bracket}'.";
        return new SyntaxException(message, queryPart, DiagnosticCode.MQ2010_MissingClosingParenthesis, span);
    }
}
