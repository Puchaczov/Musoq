using System.Collections.Generic;

namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    private ISet<string>? _partialParsedFields;
    private TOut? _partialParsedValue;
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
    protected void RecordParsedField(string fieldName)
    {
        _partialParsedFields?.Add(fieldName);
    }

    protected void RecordParsedValue(TOut value)
    {
        if (_partialParsedFields != null)
        {
            _partialParsedValue = value;
            AttachPartialFieldNames(value, (IReadOnlySet<string>)_partialParsedFields);
        }
    }

    protected virtual void AttachPartialFieldNames(TOut value, IReadOnlySet<string> completedFields) { }

    /// <summary>
    ///     Parses a nested schema at the current cursor position and preserves its field path on failure.
    /// </summary>
    protected TChild ParseNested<TChild>(
        ITextInterpreter<TChild> interpreter,
        ReadOnlySpan<char> text,
        string fieldName
    )
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
            ParsePosition = interpreter.CharsConsumed;

            throw AddNestedFieldContext(ex, fieldName);
        }
    }

    /// <summary>
    ///     Parses a nested schema at an explicit cursor position and preserves its field path on failure.
    /// </summary>
    protected TChild ParseNestedAt<TChild>(
        ITextInterpreter<TChild> interpreter,
        ReadOnlySpan<char> text,
        int offset,
        string fieldName
    )
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
    private PartialCaptureState BeginPartialCapture(ISet<string> parsedFields)
    {
        var state = new PartialCaptureState(_partialParsedFields, _partialParsedValue, _currentFieldName);
        _partialParsedFields = parsedFields;
        _partialParsedValue = default;
        _currentFieldName = null;
        return state;
    }

    private void EndPartialCapture(PartialCaptureState state)
    {
        _partialParsedFields = state.ParsedFields;
        _partialParsedValue = state.ParsedValue;
        _currentFieldName = state.FieldName;
    }

    private readonly record struct PartialCaptureState(
        ISet<string>? ParsedFields,
        TOut? ParsedValue,
        string? FieldName
    );

    private static ParseException AddNestedFieldContext(ParseException exception, string fieldName)
    {
        return ParseException.ForText(
            exception.ErrorCode,
            exception.SchemaName,
            ParserFieldPath.Qualify(exception.FieldName, fieldName),
            exception.Position,
            exception.Details,
            exception
        );
    }
}
