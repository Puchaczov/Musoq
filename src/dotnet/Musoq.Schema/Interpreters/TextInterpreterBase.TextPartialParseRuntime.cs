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
        var parsedFields = new Dictionary<string, object?>();
        var captureState = BeginPartialCapture(parsedFields);

        try
        {
            var result = Parse(text);

            foreach (var property in typeof(TOut).GetProperties())
                if (property.CanRead)
                    parsedFields[property.Name] = property.GetValue(result);

            return new PartialInterpretResult<TOut>(result, parsedFields, CharsConsumed);
        }
        catch (ParseException ex)
        {
            return new PartialInterpretResult<TOut>(parsedFields, CharsConsumed,
                ex.FieldName ?? _currentFieldName ?? "Unknown", ex.Message);
        }
        catch (Exception ex)
        {
            return new PartialInterpretResult<TOut>(parsedFields, CharsConsumed,
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
