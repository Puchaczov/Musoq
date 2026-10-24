namespace Musoq.Evaluator;

/// <summary>Immutable compile-time limits for binary and text interpretation.</summary>
public sealed record InterpretationExecutionLimits
{
    /// <summary>The historical default maximum number of iterations per repeat field.</summary>
    public const int DefaultMaxRepeatIterations = 10_000;

    /// <summary>Gets the default limits.</summary>
    public static InterpretationExecutionLimits Default { get; } = new();

    /// <summary>Creates limits with a positive maximum number of iterations per repeat field.</summary>
    public InterpretationExecutionLimits(int maxRepeatIterations = DefaultMaxRepeatIterations)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRepeatIterations);
        MaxRepeatIterations = maxRepeatIterations;
    }

    /// <summary>Gets the maximum number of iterations for each repeat field.</summary>
    public int MaxRepeatIterations { get; }
}
