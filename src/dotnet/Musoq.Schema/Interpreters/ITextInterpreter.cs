using System;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Interface for interpreting text data according to a schema definition.
///     Implementations are generated from text schema definitions.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public interface ITextInterpreter<TOut> : IInterpreter<TOut>
{
    /// <summary>
    ///     Gets the number of characters consumed by the last successful parse.
    /// </summary>
    int CharsConsumed { get; }

    /// <summary>
    ///     Parses text data starting from position 0.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>The parsed object.</returns>
    /// <exception cref="ParseException">Thrown when parsing fails.</exception>
    TOut Parse(ReadOnlySpan<char> text);

    /// <summary>
    ///     Parses text data starting from the specified position.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="position">The character position to start parsing from.</param>
    /// <returns>The parsed object.</returns>
    /// <exception cref="ParseException">Thrown when parsing fails.</exception>
    TOut ParseAt(ReadOnlySpan<char> text, int position);
}
