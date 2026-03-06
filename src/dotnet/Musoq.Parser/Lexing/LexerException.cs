using System;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Lexing;

/// <summary>
///     Base exception for all lexer-related errors.
///     Provides context about the position in the input where the error occurred.
/// </summary>
public class LexerException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LexerException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="position">The position in the input where the error occurred.</param>
    public LexerException(string message, int position) : base(message)
    {
        Position = position;
        Code = DiagnosticCode.MQ1001_UnknownToken;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LexerException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="position">The position in the input where the error occurred.</param>
    /// <param name="code">The diagnostic code.</param>
    public LexerException(string message, int position, DiagnosticCode code) : base(message)
    {
        Position = position;
        Code = code;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LexerException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="position">The position in the input where the error occurred.</param>
    /// <param name="innerException">The inner exception.</param>
    public LexerException(string message, int position, Exception innerException)
        : base(message, innerException)
    {
        Position = position;
        Code = DiagnosticCode.MQ1001_UnknownToken;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LexerException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="position">The position in the input where the error occurred.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <param name="innerException">The inner exception.</param>
    public LexerException(string message, int position, DiagnosticCode code, Exception innerException)
        : base(message, innerException)
    {
        Position = position;
        Code = code;
    }

    /// <summary>
    ///     Gets the position in the input where the error occurred.
    /// </summary>
    public int Position { get; }

    /// <summary>
    ///     Gets the diagnostic code for this error.
    /// </summary>
    public DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the span of the error.
    /// </summary>
    public virtual TextSpan? Span => new TextSpan(Position, 1);

    /// <summary>
    ///     Converts this exception to a diagnostic.
    /// </summary>
    /// <param name="sourceText">The source text for location information.</param>
    /// <returns>A diagnostic representing this error.</returns>
    public virtual Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? new TextSpan(Position, 1);

        if (sourceText is null)
            return Diagnostic.Error(Code, Message, span);

        var location = sourceText.GetLocation(Position);
        var contextSnippet = sourceText.GetContextSnippet(span);

        return new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            Message,
            location,
            null,
            contextSnippet);
    }
}
