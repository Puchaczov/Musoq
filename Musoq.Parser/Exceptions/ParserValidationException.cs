using System;

namespace Musoq.Parser.Exceptions;

/// <summary>
///     Exception thrown when input validation fails in the parser.
///     Provides detailed information about validation failures with helpful guidance.
/// </summary>
public class ParserValidationException : ArgumentException
{
    public ParserValidationException(string message) : base(message)
    {
    }

    public ParserValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public static ParserValidationException ForNullInput()
    {
        return new ParserValidationException(
            "The SQL query input cannot be null. Please provide a valid SQL query string."
        );
    }

    public static ParserValidationException ForEmptyInput()
    {
        return new ParserValidationException(
            "The SQL query input cannot be empty or contain only whitespace. Please provide a valid SQL query."
        );
    }

    public static ParserValidationException ForInvalidInput(string input, string reason)
    {
        return new ParserValidationException(
            $"The SQL query input '{input}' is invalid: {reason}. Please check your query syntax and try again."
        );
    }
}