using Musoq.Evaluator.IR.Execution;

namespace Musoq.Converter.Build;

/// <summary>
/// Typed view of the execution stage output: the lowered execution plan and its
/// build result, including the optional textual dump.
/// </summary>
internal sealed record ExecutionBuildArtifacts
{
    public ExecutionPlanBuildResult? ExecutionPlanBuildResult { get; init; }

    public ExecutionPlan? InitialExecutionPlan { get; init; }

    public ExecutionPlan? OptimizedExecutionPlan { get; init; }

    public ExecutionPlan? ExecutionPlan { get; init; }

    public string? ExecutionPlanText { get; init; }
}
