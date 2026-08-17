using Musoq.Evaluator.IR.Optimization.Physical;

namespace Musoq.Evaluator.IR.Optimization;

/// <summary>
/// Typed optimizer collaborators that replace stringly-typed optimizer state entries.
/// </summary>
internal sealed record OptimizationContextState
{
    public DiagnosticContext? DiagnosticContext { get; init; }

    public PhysicalOptimizationState? PhysicalOptimizationState { get; init; }

    public static OptimizationContextState Empty { get; } = new();
}
