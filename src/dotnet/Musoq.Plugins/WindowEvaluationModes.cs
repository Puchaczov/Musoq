namespace Musoq.Plugins;

/// <summary>Declares window evaluation modes implemented by a typed kernel.</summary>
[Flags]
public enum WindowEvaluationModes
{
    /// <summary>No typed evaluation mode is advertised.</summary>
    None = 0,
    /// <summary>Evaluates a complete partition.</summary>
    WholePartition = 1,
    /// <summary>Evaluates prefixes in partition order.</summary>
    Running = 2,
    /// <summary>Recomputes frames using reset and accumulation operations.</summary>
    RecomputeFrames = 4,
    /// <summary>Retracts values while preserving frame results and failures.</summary>
    RetractableFrames = 8,
    /// <summary>Provides specialized typed batch evaluation.</summary>
    Batch = 16
}
