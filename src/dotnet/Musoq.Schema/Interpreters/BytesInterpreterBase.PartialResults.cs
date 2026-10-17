using System;
using System.Collections.Generic;

namespace Musoq.Schema.Interpreters;

public abstract partial class BytesInterpreterBase<TOut>
{
    private HashSet<string>? _partialParsedFields;
    private TOut? _partialParsedValue;
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
            throw ParseException.ForBytes(
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
    protected void RecordParsedField(string fieldName)
    {
        _partialParsedFields?.Add(fieldName);
    }

    protected void RecordParsedValue(TOut value)
    {
        if (_partialParsedFields != null)
        {
            _partialParsedValue = value;
            AttachPartialFieldNames(value, _partialParsedFields);
        }
    }

    protected virtual void AttachPartialFieldNames(TOut value, IReadOnlySet<string> completedFields)
    {
    }

    /// <summary>
    ///     Interprets a nested schema at the current cursor position and preserves its field path on failure.
    /// </summary>
    protected TChild InterpretNested<TChild>(IBytesInterpreter<TChild> interpreter, ReadOnlySpan<byte> data,
        string fieldName)
    {
        ArgumentNullException.ThrowIfNull(interpreter);
        AlignToBits(data, 8);

        try
        {
            var result = interpreter.InterpretAt(data, ParsePosition);
            ResumeAfterNested(interpreter);
            return result;
        }
        catch (ParseException ex)
        {
            if (interpreter.BytesConsumed >= ParsePosition)
                ResumeAfterNested(interpreter);

            throw AddNestedFieldContext(ex, fieldName);
        }
    }

    private void ResumeAfterNested<TChild>(IBytesInterpreter<TChild> interpreter)
    {
        ParsePosition = interpreter.BytesConsumed;
        BitOffset = interpreter is BytesInterpreterBase<TChild> nested ? nested.BitOffset : 0;
    }

    /// <summary>
    ///     Interprets a nested schema at an explicit cursor position and preserves its field path on failure.
    /// </summary>
    protected TChild InterpretNestedAt<TChild>(IBytesInterpreter<TChild> interpreter, ReadOnlySpan<byte> data,
        int offset, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(interpreter);

        var parentPosition = ParsePosition;

        try
        {
            return interpreter.InterpretAt(data, offset);
        }
        catch (ParseException ex)
        {
            var nestedConsumed = interpreter.BytesConsumed;
            if (nestedConsumed >= offset)
                ParsePosition = parentPosition + (nestedConsumed - offset);

            var rebasedPosition = (long)parentPosition + ex.Position - offset;
            var position = rebasedPosition switch
            {
                > int.MaxValue => int.MaxValue,
                < int.MinValue => int.MinValue,
                _ => (int)rebasedPosition
            };

            throw AddNestedFieldContext(ex, fieldName, position);
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
        var parsedFields = new HashSet<string>(StringComparer.Ordinal);
        var previousParsedValue = _partialParsedValue;
        _partialParsedValue = default;
        var previousParsedFields = _partialParsedFields;
        var previousFieldName = _currentFieldName;
        _partialParsedFields = parsedFields;
        _currentFieldName = null;

        try
        {
            var result = Interpret(data);

            return new PartialInterpretResult<TOut>(result, parsedFields, BytesConsumed);
        }
        catch (ParseException ex)
        {
            return new PartialInterpretResult<TOut>(_partialParsedValue, parsedFields, BytesConsumed,
                ex.FieldName ?? _currentFieldName ?? "Unknown", ex.GetPartialInterpretationMessage());
        }
        catch (Exception ex)
        {
            return new PartialInterpretResult<TOut>(_partialParsedValue, parsedFields, BytesConsumed,
                _currentFieldName ?? "Unknown", ex.Message);
        }
        finally
        {
            _partialParsedFields = previousParsedFields;
            _partialParsedValue = previousParsedValue;
            _currentFieldName = previousFieldName;
        }
    }

    private static ParseException AddNestedFieldContext(
        ParseException exception,
        string fieldName,
        int? positionOverride = null)
    {
        var nestedFieldName = exception.FieldName;
        var qualifiedFieldName = string.IsNullOrEmpty(nestedFieldName)
            ? fieldName
            : string.Equals(nestedFieldName, fieldName, StringComparison.OrdinalIgnoreCase) ||
              nestedFieldName.StartsWith(fieldName + ".", StringComparison.OrdinalIgnoreCase)
                ? nestedFieldName
                : $"{fieldName}.{nestedFieldName}";

        return ParseException.ForBytes(
            exception.ErrorCode,
            exception.SchemaName,
            qualifiedFieldName,
            positionOverride ?? exception.Position,
            exception.Details,
            exception);
    }
}
