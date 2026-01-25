using System;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Interface for interpreting binary data according to a schema definition.
///     Implementations are generated from binary schema definitions.
/// </summary>
/// <typeparam name="TOut">The type of the parsed result object.</typeparam>
public interface IBytesInterpreter<TOut> : IInterpreter<TOut>
{
    /// <summary>
    ///     Gets the number of bytes consumed by the last successful interpretation.
    /// </summary>
    int BytesConsumed { get; }

    /// <summary>
    ///     Interprets binary data starting from offset 0.
    /// </summary>
    /// <param name="data">The byte array to interpret.</param>
    /// <returns>The parsed object.</returns>
    /// <exception cref="ParseException">Thrown when parsing fails.</exception>
    TOut Interpret(ReadOnlySpan<byte> data);

    /// <summary>
    ///     Interprets binary data from a byte array starting from offset 0.
    /// </summary>
    /// <param name="data">The byte array to interpret.</param>
    /// <returns>The parsed object.</returns>
    /// <exception cref="ParseException">Thrown when parsing fails.</exception>
    TOut Interpret(byte[] data);

    /// <summary>
    ///     Interprets binary data starting from the specified offset.
    /// </summary>
    /// <param name="data">The byte array to interpret.</param>
    /// <param name="offset">The offset in bytes to start interpreting from.</param>
    /// <returns>The parsed object.</returns>
    /// <exception cref="ParseException">Thrown when parsing fails.</exception>
    TOut InterpretAt(ReadOnlySpan<byte> data, int offset);
}
