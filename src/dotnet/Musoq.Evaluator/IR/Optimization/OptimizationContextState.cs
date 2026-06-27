using System.Collections.Generic;
using Musoq.Evaluator;

namespace Musoq.Evaluator.IR.Optimization;

/// <summary>
/// Typed optimizer collaborators that replace the deprecated stringly-typed
/// <see cref="OptimizationContext.Properties"/> state entries.
/// </summary>
internal sealed record OptimizationContextState
{
    public DiagnosticContext? DiagnosticContext { get; init; }

    public PhysicalOptimizationState? PhysicalOptimizationState { get; init; }

    public static OptimizationContextState Empty { get; } = new();
}
