#nullable enable

using System.Collections.Generic;

namespace Musoq.Schema.Interpreters;

/// <summary>
///     Represents the result of a partial interpretation attempt.
///     Used for debugging malformed data by returning successfully parsed fields
///     along with error information about where parsing failed.
/// </summary>
/// <typeparam name="TOut">The type of the successfully parsed result if parsing completes.</typeparam>
public class PartialInterpretResult<TOut>
{
    /// <summary>
    ///     Creates a successful partial interpret result.
    /// </summary>
    /// <param name="result">The successfully parsed result.</param>
    /// <param name="parsedFields">Dictionary of successfully parsed field names and values.</param>
    /// <param name="bytesConsumed">Number of bytes successfully processed.</param>
    public PartialInterpretResult(TOut result, Dictionary<string, object?> parsedFields, int bytesConsumed)
    {
        IsSuccess = true;
        Result = result;
        ParsedFields = parsedFields;
        BytesConsumed = bytesConsumed;
        ErrorField = null;
        ErrorMessage = null;
    }

    /// <summary>
    ///     Creates a failed partial interpret result with error information.
    /// </summary>
    /// <param name="parsedFields">Dictionary of successfully parsed field names and values before failure.</param>
    /// <param name="bytesConsumed">Number of bytes successfully processed before failure.</param>
    /// <param name="errorField">Name of the field where parsing failed.</param>
    /// <param name="errorMessage">Error description.</param>
    public PartialInterpretResult(Dictionary<string, object?> parsedFields, int bytesConsumed, string errorField,
        string errorMessage)
    {
        IsSuccess = false;
        Result = default;
        ParsedFields = parsedFields;
        BytesConsumed = bytesConsumed;
        ErrorField = errorField;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    ///     Gets whether the interpretation was fully successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    ///     Gets the fully parsed result if parsing was successful, otherwise default.
    /// </summary>
    public TOut? Result { get; }

    /// <summary>
    ///     Gets the dictionary of successfully parsed field names and their values.
    /// </summary>
    public Dictionary<string, object?> ParsedFields { get; }

    /// <summary>
    ///     Gets the name of the field where parsing failed, or null if successful.
    /// </summary>
    public string? ErrorField { get; }

    /// <summary>
    ///     Gets the error description, or null if successful.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    ///     Gets the number of bytes successfully processed.
    /// </summary>
    public int BytesConsumed { get; }
}
