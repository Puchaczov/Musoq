using System;

namespace Musoq.Parser.Lexing;

/// <summary>
///     Base exception for all lexer-related errors.
///     Provides context about the position in the input where the error occurred.
/// </summary>
public class LexerException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LexerException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="position">The position in the input where the error occurred.</param>
    public LexerException(string message, int position) : base(message)
    {
        Position = position;
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
    }

    /// <summary>
    ///     Gets the position in the input where the error occurred.
    /// </summary>
    public int Position { get; }
}
