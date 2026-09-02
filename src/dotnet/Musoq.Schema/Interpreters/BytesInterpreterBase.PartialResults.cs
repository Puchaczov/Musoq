using System;
using System.Collections.Generic;

namespace Musoq.Schema.Interpreters;

public abstract partial class BytesInterpreterBase<TOut>
{
    private Dictionary<string, object?>? _partialParsedFields;
    private string? _currentFieldName;

    /// <summary>
    ///     Validates and initializes the parse cursor for a top-level interpretation.
    /// </summary>
    /// <param name="data">The input data.</param>
    /// <param name="offset">The byte offset at which parsing should begin.</param>
    /// <exception cref="ParseException">Thrown when the offset is outside the input.</exception>
    protected void InitializeParsePosition(ReadOnlySpan<byte> data, int offset)
    {
        if ((uint)offset > (uint)data.Length)
            throw new ParseException(
                ParseErrorCode.InvalidPosition,
                SchemaName,
                null,
                offset,
                $"Parse position {offset} is {(offset < 0 ? "negative" : "past the end of the input")}; valid range is 0 through {data.Length}");

        ParsePosition = offset;
        BitOffset = 0;
    }

    /// <summary>
    ///     Sets the field currently being interpreted so low-level parse errors retain field context.
    /// </summary>
    protected void SetCurrentField(string? fieldName)
    {
        _currentFieldName = fieldName;
    }

    /// <summary>
    ///     Records a successfully interpreted field while a partial result is being captured.
    /// </summary>
    protected void RecordParsedField(string fieldName, object? value)
    {
        _partialParsedFields?[fieldName] = value;
    }

    /// <summary>
    ///     Interprets a nested schema at the current cursor position and preserves its field path on failure.
    /// </summary>
    protected TChild InterpretNested<TChild>(IBytesInterpreter<TChild> interpreter, ReadOnlySpan<byte> data,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(interpreter);

        try
        {
            var result = interpreter.InterpretAt(data, ParsePosition);
            ParsePosition = interpreter.BytesConsumed;
            return result;
        }
        catch (ParseException ex)
        {
            if (interpreter.BytesConsumed >= ParsePosition)
                ParsePosition = interpreter.BytesConsumed;

            throw AddNestedFieldContext(ex, fieldName);
        }
    }

    /// <summary>
    ///     Interprets a nested schema at an explicit cursor position and preserves its field path on failure.
    /// </summary>
    protected TChild InterpretNestedAt<TChild>(IBytesInterpreter<TChild> interpreter, ReadOnlySpan<byte> data,
        int offset, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(interpreter);

        try
        {
            return interpreter.InterpretAt(data, offset);
        }
        catch (ParseException ex)
        {
            throw AddNestedFieldContext(ex, fieldName);
        }
    }

    /// <summary>
    ///     Parses text embedded in a binary field and preserves the binary field path on failure.
    /// </summary>
    protected TChild ParseNested<TChild>(ITextInterpreter<TChild> interpreter, string text, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(interpreter);

        try
        {
            return interpreter.Parse(text.AsSpan());
        }
        catch (ParseException ex)
        {
            throw AddNestedFieldContext(ex, fieldName);
        }
    }

    /// <summary>
    ///     Interprets binary data with partial result capture for debugging malformed data.
    /// </summary>
    /// <param name="data">The binary data to interpret.</param>
    /// <returns>A PartialInterpretResult containing either the full result or error information with partial fields.</returns>
    public virtual PartialInterpretResult<TOut> PartialInterpret(ReadOnlySpan<byte> data)
    {
        var parsedFields = new Dictionary<string, object?>();
        var previousParsedFields = _partialParsedFields;
        var previousFieldName = _currentFieldName;
        _partialParsedFields = parsedFields;
        _currentFieldName = null;

        try
        {
            var result = Interpret(data);

            foreach (var property in typeof(TOut).GetProperties())
                if (property.CanRead)
                    parsedFields[property.Name] = property.GetValue(result);

            return new PartialInterpretResult<TOut>(result, parsedFields, BytesConsumed);
        }
        catch (ParseException ex)
        {
            return new PartialInterpretResult<TOut>(parsedFields, BytesConsumed,
                ex.FieldName ?? _currentFieldName ?? "Unknown", ex.Message);
        }
        catch (Exception ex)
        {
            return new PartialInterpretResult<TOut>(parsedFields, BytesConsumed,
                _currentFieldName ?? "Unknown", ex.Message);
        }
        finally
        {
            _partialParsedFields = previousParsedFields;
            _currentFieldName = previousFieldName;
        }
    }

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
