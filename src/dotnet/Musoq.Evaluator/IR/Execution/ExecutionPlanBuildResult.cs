namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPlanBuildResult(
    bool Supported,
    ExecutionPlan? ExecutionPlan,
    string? UnsupportedReason)
{
    public static ExecutionPlanBuildResult CreateSupported(ExecutionPlan executionPlan)
    {
        ArgumentNullException.ThrowIfNull(executionPlan);
        return new ExecutionPlanBuildResult(true, GeneratedRowContextPruner.Prune(executionPlan), null);
    }

    public static ExecutionPlanBuildResult CreateUnsupported(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new ExecutionPlanBuildResult(false, null, reason);
    }
}
