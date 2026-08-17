using System.Collections.Generic;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class PhysicalOptimizationSession
{
    private readonly List<PlanningDecision> _decisions = [];

    public PhysicalOptimizationSession(
        PlanProperties properties,
        CompilationOptions compilationOptions,
        IPlanningShapeResolver shapeResolver)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(compilationOptions);
        ArgumentNullException.ThrowIfNull(shapeResolver);

        InitialProperties = properties;
        Facts = PhysicalOptimizationFacts.From(properties);
        CompilationOptions = compilationOptions;
        ShapeResolver = shapeResolver;
    }

    public PlanProperties InitialProperties { get; }

    public PhysicalOptimizationFacts Facts { get; set; }

    public CompilationOptions CompilationOptions { get; }

    public IPlanningShapeResolver ShapeResolver { get; }

    public IReadOnlyList<PlanningDecision> Decisions => _decisions;

    public PlanProperties OptimizedProperties => Facts.ApplyTo(InitialProperties);

    public void AddDecision(PlanningDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        _decisions.Add(decision);
    }
}

