namespace Musoq.Evaluator.IR.Optimization;

/// <summary>
/// A single ordered step in an optimization pipeline. Pairs the pass with an
/// explicit reason describing why it runs at this position, so the pipeline is
/// self-documenting and intentional repeats (such as running the same pass twice)
/// are visible at the declaration site.
/// </summary>
internal sealed record OptimizationPassStep<TPlan>(
    IPlanOptimizationPass<TPlan> Pass,
    string Reason)
{
    public string Name => Pass.Name;
}
