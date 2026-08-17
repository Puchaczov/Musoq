using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class PhysicalOptimizationState(PhysicalOptimizationSession session)
{
    public PhysicalOptimizationFacts Facts
    {
        get => session.Facts;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            session.Facts = value;
        }
    }

    public PlanProperties OptimizedProperties => session.OptimizedProperties;

    public CompilationOptions CompilationOptions => session.CompilationOptions;

    public IPlanningShapeResolver ShapeResolver => session.ShapeResolver;

    public IReadOnlyList<PlanningDecision> Decisions => session.Decisions;

    public void AddDecision(PlanningDecision decision)
    {
        session.AddDecision(decision);
    }

    public static PhysicalOptimizationState From(OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.State.PhysicalOptimizationState
               ?? throw new InvalidOperationException("Physical optimization requires physical optimization state.");
    }
}

