using System.Collections.Generic;

namespace Musoq.Schema.Interpreters;

/// <summary>Concrete decoded fields and completion metadata from a partial interpretation.</summary>
/// <typeparam name="TOut">The concrete schema payload type.</typeparam>
public sealed class PartialInterpretResult<TOut>
{
    public PartialInterpretResult(TOut result, IReadOnlySet<string> completedFields, int bytesConsumed)
    {
        IsSuccess = true;
        Result = result;
        ParsedFields = result;
        CompletedFields = completedFields;
        BytesConsumed = bytesConsumed;
    }

    public PartialInterpretResult(TOut? parsedFields, IReadOnlySet<string> completedFields, int bytesConsumed,
        string errorField, string errorMessage)
    {
        ParsedFields = parsedFields;
        CompletedFields = completedFields;
        BytesConsumed = bytesConsumed;
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
}
