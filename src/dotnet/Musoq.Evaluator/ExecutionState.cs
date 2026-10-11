namespace Musoq.Evaluator;

/// <summary>Owns the concrete parameter slots captured for one execution.</summary>
public sealed class ExecutionState
{
    public static readonly ExecutionState Empty = new(TypedParameterSnapshot.Empty);

    private ExecutionState(TypedParameterSnapshot typedParameters) => TypedParameters = typedParameters;

    public TypedParameterSnapshot TypedParameters { get; }

    public static ExecutionState Capture(TypedParameterSnapshot typedParameters)
    {
        ArgumentNullException.ThrowIfNull(typedParameters);
        return typedParameters.Count == 0 ? Empty : new ExecutionState(typedParameters);
    }
}
