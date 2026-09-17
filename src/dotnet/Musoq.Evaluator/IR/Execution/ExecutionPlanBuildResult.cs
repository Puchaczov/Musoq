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
        var planned = ExecutionPlanRepresentationPlanner.Plan(prunedPlan);
        ExecutionBindingInvariantValidator.Validate(planned);
        return new ExecutionPlanBuildResult(true, planned, null);
    }

    public static ExecutionPlanBuildResult CreateUnsupported(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new ExecutionPlanBuildResult(false, null, reason);
    }
}
