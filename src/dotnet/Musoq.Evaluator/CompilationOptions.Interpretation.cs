namespace Musoq.Evaluator;

public partial class CompilationOptions
{
    /// <summary>Gets the limits compiled into binary and text repeat guards.</summary>
    public InterpretationExecutionLimits InterpretationLimits { get; private init; } = InterpretationExecutionLimits.Default;

    /// <summary>Copies these options with the selected interpretation limits.</summary>
    public CompilationOptions WithInterpretationLimits(InterpretationExecutionLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return Clone(interpretationLimits: limits);
    }
}
