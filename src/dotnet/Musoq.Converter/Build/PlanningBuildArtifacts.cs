using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Converter.Build;

/// <summary>
/// Typed view of the planning stage output: logical and physical plans together
/// with the planner result and its textual diagnostics.
/// </summary>
internal sealed record PlanningBuildArtifacts
{
    public LogicalNode? InitialLogicalPlan { get; init; }

    public LogicalNode? OptimizedLogicalPlan { get; init; }

    public LogicalNode? LogicalPlan { get; init; }

    public PlanningResult? PlanningResult { get; init; }

    public string? PlanningText { get; init; }

    public PhysicalNode? InitialPhysicalPlan { get; init; }

    public PhysicalNode? OptimizedPhysicalPlan { get; init; }

    public PhysicalNode? PhysicalPlan { get; init; }
}
