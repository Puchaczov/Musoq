using System.Collections.Generic;

namespace Musoq.Schema.Interpreters;

public abstract partial class TextInterpreterBase<TOut>
{
    /// <summary>
    ///     Parses text with partial result capture for debugging malformed data.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <returns>A PartialInterpretResult containing either the full result or error information.</returns>
    public virtual PartialInterpretResult<TOut> PartialParse(ReadOnlySpan<char> text)
    {
        var parsedFields = new HashSet<string>(StringComparer.Ordinal);
        var captureState = BeginPartialCapture(parsedFields);

        try
        {
            var result = Parse(text);

            return new PartialInterpretResult<TOut>(result, parsedFields, CharsConsumed);
        }
        catch (ParseException ex)
        {
            return new PartialInterpretResult<TOut>(_partialParsedValue, parsedFields, CharsConsumed,
                ex.FieldName ?? _currentFieldName ?? "Unknown", ex.GetPartialInterpretationMessage());
        }
        catch (Exception ex)
        {
            return new PartialInterpretResult<TOut>(_partialParsedValue, parsedFields, CharsConsumed,
                _currentFieldName ?? "Unknown", ex.Message);
        }
        finally
        {
            EndPartialCapture(captureState);
        }
    }

    /// <summary>
    ///     Parses text with partial result capture for debugging malformed data.
    /// </summary>
    /// <param name="text">The text string to parse.</param>
    /// <returns>A PartialInterpretResult containing either the full result or error information.</returns>
    public PartialInterpretResult<TOut> PartialParse(string text)
    {
        return PartialParse(text.AsSpan());
    }
}
