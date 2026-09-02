using System;
using System.Collections.Generic;

namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    private Dictionary<string, object?>? _partialParsedFields;
    private string? _currentFieldName;

    /// <summary>
    ///     Sets the field currently being parsed so low-level parse errors retain field context.
    /// </summary>
    protected void SetCurrentField(string? fieldName)
    {
        _currentFieldName = fieldName;
    }

    /// <summary>
    ///     Records a successfully parsed field while a partial result is being captured.
    /// </summary>
    protected void RecordParsedField(string fieldName, object? value)
    {
        _partialParsedFields?[fieldName] = value;
    }

    /// <summary>
    ///     Parses a nested schema at the current cursor position and preserves its field path on failure.
    /// </summary>
    protected TChild ParseNested<TChild>(ITextInterpreter<TChild> interpreter, ReadOnlySpan<char> text,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(interpreter);

        try
        {
            var result = interpreter.ParseAt(text, ParsePosition);
            ParsePosition = interpreter.CharsConsumed;
            return result;
        }
        catch (ParseException ex)
        {
            if (interpreter.CharsConsumed >= ParsePosition)
                ParsePosition = interpreter.CharsConsumed;

            throw AddNestedFieldContext(ex, fieldName);
        }
    }

    /// <summary>
    ///     Parses a nested schema at an explicit cursor position and preserves its field path on failure.
    /// </summary>
    protected TChild ParseNestedAt<TChild>(ITextInterpreter<TChild> interpreter, ReadOnlySpan<char> text,
        int offset, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(interpreter);

        try
        {
            return interpreter.ParseAt(text, offset);
        }
        catch (ParseException ex)
        {
            throw AddNestedFieldContext(ex, fieldName);
        }
    }

    /// <summary>
    ///     Begins capturing successfully parsed fields for a partial result.
    /// </summary>
    private PartialCaptureState BeginPartialCapture(Dictionary<string, object?> parsedFields)
    {
        var state = new PartialCaptureState(_partialParsedFields, _currentFieldName);
        _partialParsedFields = parsedFields;
        _currentFieldName = null;
        return state;
    }

    private void EndPartialCapture(PartialCaptureState state)
    {
        _partialParsedFields = state.ParsedFields;
        _currentFieldName = state.FieldName;
    }

    private readonly record struct PartialCaptureState(
        Dictionary<string, object?>? ParsedFields,
        string? FieldName);

    private static ParseException AddNestedFieldContext(ParseException exception, string fieldName)
    {
        var nestedFieldName = exception.FieldName;
        var qualifiedFieldName = string.IsNullOrEmpty(nestedFieldName)
            ? fieldName
            : string.Equals(nestedFieldName, fieldName, StringComparison.OrdinalIgnoreCase) ||
              nestedFieldName.StartsWith(fieldName + ".", StringComparison.OrdinalIgnoreCase)
                ? nestedFieldName
                : $"{fieldName}.{nestedFieldName}";

        return new ParseException(
            exception.ErrorCode,
            exception.SchemaName,
            qualifiedFieldName,
            exception.Position,
            exception.Details,
            exception);
    }
}
