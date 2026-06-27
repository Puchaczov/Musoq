using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Optimization;

/// <summary>
/// A named, ordered optimization pipeline. Replaces raw pass arrays so each pipeline
/// carries its stage, run mode, and the ordered steps with their reasons.
/// </summary>
internal sealed class OptimizationPassPipeline<TPlan>
{
    public OptimizationPassPipeline(
        OptimizationStage stage,
        OptimizationPassRunMode runMode,
        IReadOnlyList<OptimizationPassStep<TPlan>> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);

        foreach (var step in steps)
        {
            ArgumentNullException.ThrowIfNull(step);
            ArgumentNullException.ThrowIfNull(step.Pass);
            if (string.IsNullOrWhiteSpace(step.Reason))
                throw new ArgumentException("Optimization pipeline step reason cannot be null or whitespace.", nameof(steps));
        }

        Stage = stage;
        RunMode = runMode;
        Steps = steps;
        Passes = steps.Select(step => step.Pass).ToArray();
    }

    public OptimizationStage Stage { get; }

    public OptimizationPassRunMode RunMode { get; }

    public IReadOnlyList<OptimizationPassStep<TPlan>> Steps { get; }

    public IReadOnlyList<IPlanOptimizationPass<TPlan>> Passes { get; }
}
