using System.Collections.Generic;

namespace Musoq.Schema.Interpreters;

/// <summary>Concrete decoded fields and completion metadata from a partial interpretation.</summary>
/// <typeparam name="TOut">The concrete schema payload type.</typeparam>
public sealed class PartialInterpretResult<TOut>
{
    public PartialInterpretResult(TOut result, IReadOnlySet<string> completedFields, int bytesConsumed)
        : this(result, completedFields, bytesConsumed, 0)
    {
    }

    public PartialInterpretResult(TOut result, IReadOnlySet<string> completedFields, int bytesConsumed, int bitOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bitOffset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bitOffset, 7);
        IsSuccess = true;
        Result = result;
        ParsedFields = result;
        CompletedFields = completedFields;
        BytesConsumed = bytesConsumed;
        BitOffset = bitOffset;
    }

    public PartialInterpretResult(TOut? parsedFields, IReadOnlySet<string> completedFields, int bytesConsumed,
        string errorField, string errorMessage)
        : this(parsedFields, completedFields, bytesConsumed, errorField, errorMessage, 0)
    {
    }

    public PartialInterpretResult(TOut? parsedFields, IReadOnlySet<string> completedFields, int bytesConsumed,
        string errorField, string errorMessage, int bitOffset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bitOffset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(bitOffset, 7);
        ParsedFields = parsedFields;
        CompletedFields = completedFields;
        BytesConsumed = bytesConsumed;
        BitOffset = bitOffset;
        ErrorField = errorField;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public TOut? Result { get; }
    public TOut? ParsedFields { get; }
    public IReadOnlySet<string> CompletedFields { get; }
    public string? ErrorField { get; }
    public string? ErrorMessage { get; }
    public int BytesConsumed { get; }
    public int BitOffset { get; }
}
