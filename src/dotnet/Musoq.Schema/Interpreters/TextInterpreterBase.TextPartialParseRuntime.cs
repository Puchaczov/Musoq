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
        var parsedFields = CreateCompletionSet();
        var captureState = BeginPartialCapture(parsedFields);

        try
        {
            var attempt = InterpretationStatus.ReadAt(this, text, 0);
            if (attempt.Succeeded)
                return new PartialInterpretResult<TOut>(
                    attempt.Value!,
                    (IReadOnlySet<string>)parsedFields,
                    CharsConsumed
                );
            var failure = attempt.Failure!;
            return new PartialInterpretResult<TOut>(
                attempt.Value,
                (IReadOnlySet<string>)parsedFields,
                CharsConsumed,
                failure.FieldName ?? _currentFieldName ?? "Unknown",
                failure.GetPartialInterpretationMessage()
            );
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
