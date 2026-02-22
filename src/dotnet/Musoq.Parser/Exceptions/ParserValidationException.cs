using System;
using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Exceptions;

/// <summary>
///     Exception thrown when input validation fails in the parser.
///     Provides detailed information about validation failures with helpful guidance.
/// </summary>
public class ParserValidationException : ArgumentException, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of ParserValidationException.
    /// </summary>
    public ParserValidationException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ2001_UnexpectedToken;
    }

    /// <summary>
    ///     Initializes a new instance of ParserValidationException with an inner exception.
    /// </summary>
    public ParserValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Code = DiagnosticCode.MQ2001_UnexpectedToken;
    }

    /// <summary>
    ///     Initializes a new instance of ParserValidationException with diagnostic information.
    /// </summary>
    public ParserValidationException(string message, DiagnosticCode code, TextSpan? span = null)
        : base(message)
    {
        Code = code;
        Span = span;
    }

    /// <summary>
    ///     Gets the diagnostic code for this validation error.
    /// </summary>
    public DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the source location span where this error occurred, if known.
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
    ///     Creates a ParserValidationException for null input.
    /// </summary>
    public static ParserValidationException ForNullInput()
    {
        return new ParserValidationException(
            "The SQL query input cannot be null. Please provide a valid SQL query string.",
            DiagnosticCode.MQ2016_IncompleteStatement
        );
    }

    /// <summary>
    ///     Creates a ParserValidationException for empty input.
    /// </summary>
    public static ParserValidationException ForEmptyInput()
    {
        return new ParserValidationException(
            "The SQL query input cannot be empty or contain only whitespace. Please provide a valid SQL query.",
            DiagnosticCode.MQ2016_IncompleteStatement
        );
    }

    /// <summary>
    ///     Creates a ParserValidationException for invalid input with a specific reason.
    /// </summary>
    public static ParserValidationException ForInvalidInput(string input, string reason)
    {
        return new ParserValidationException(
            $"The SQL query input '{input}' is invalid: {reason}. Please check your query syntax and try again.",
            DiagnosticCode.MQ2001_UnexpectedToken
        );
    }

    /// <summary>
    ///     Creates a ParserValidationException for an invalid query at a specific location.
    /// </summary>
    public static ParserValidationException ForInvalidInput(string input, string reason, TextSpan span)
    {
        return new ParserValidationException(
            $"The SQL query input is invalid at position {span.Start}: {reason}.",
            DiagnosticCode.MQ2001_UnexpectedToken,
            span
        );
    }
}
