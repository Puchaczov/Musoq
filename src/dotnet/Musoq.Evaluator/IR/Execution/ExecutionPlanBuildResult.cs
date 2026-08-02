namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPlanBuildResult(
    bool Supported,
    ExecutionPlan? ExecutionPlan,
    string? UnsupportedReason)
{
    public bool IsBuilt => Supported;
    public static ExecutionPlanBuildResult CreateSupported(ExecutionPlan executionPlan)
    {
        ArgumentNullException.ThrowIfNull(executionPlan);
        var prunedPlan = GeneratedRowContextPruner.Prune(executionPlan);
        ExecutionBindingInvariantValidator.Validate(prunedPlan);
        return new ExecutionPlanBuildResult(true, prunedPlan, null);
    }

    public static ExecutionPlanBuildResult CreateUnsupported(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new ExecutionPlanBuildResult(false, null, reason);
    }
}
