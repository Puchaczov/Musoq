using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.SourcePlanning;
using Musoq.Evaluator.IR.Planning;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed class PhysicalOptimizationState(
    PlanProperties properties,
    CompilationOptions compilationOptions,
    ExecutionShapeResolver? shapeResolver = null)
{
    private readonly List<PlanningDecision> _decisions = [];

    public PlanProperties Properties { get; set; } = properties;

    public CompilationOptions CompilationOptions { get; } = compilationOptions;

    public ExecutionShapeResolver? ShapeResolver { get; } = shapeResolver;

    public IReadOnlyList<PlanningDecision> Decisions => _decisions;

    public void AddDecision(PlanningDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        _decisions.Add(decision);
    }

    public static PhysicalOptimizationState From(OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.State.PhysicalOptimizationState
               ?? throw new InvalidOperationException("Physical optimization requires physical optimization state.");
    }
}
