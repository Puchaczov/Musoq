using System;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Exceptions;

/// <summary>
///     Exception thrown when a syntax error is encountered during parsing.
///     Provides detailed location and context information for error reporting.
/// </summary>
public class SyntaxException : Exception, IDiagnosticException
{
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
    /// </summary>
    public SyntaxException(string message, string queryPart, Exception innerException)
        : base(message, innerException)
    {
        QueryPart = queryPart;
        Code = DiagnosticCode.MQ2001_UnexpectedToken;
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
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        return Diagnostic.Error(Code, Message, span);
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
