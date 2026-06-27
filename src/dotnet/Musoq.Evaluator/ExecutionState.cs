using System.Collections.Generic;

namespace Musoq.Evaluator;

public sealed class ExecutionState
{
    public static readonly ExecutionState Empty = new(ParameterSnapshot.EmptyReadOnly);

    private ExecutionState(IReadOnlyDictionary<string, object?> parameters)
    {
        Parameters = parameters;
    }

    public IReadOnlyDictionary<string, object?> Parameters { get; }

    public static ExecutionState Capture(IEnumerable<KeyValuePair<string, object?>> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var snapshot = ParameterSnapshot.CaptureReadOnlyOrEmpty(parameters);
        return ParameterSnapshot.IsEmpty(snapshot)
            ? Empty
            : new ExecutionState(snapshot);
    }
}
