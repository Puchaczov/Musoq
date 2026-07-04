using System.Collections.Generic;
using Musoq.Evaluator;

namespace Musoq.Evaluator.IR.Optimization;

/// <summary>
/// Typed optimizer switches that replace stringly-typed optimizer flags.
/// </summary>
internal sealed record OptimizationOptions
{
    public bool ConstantFoldingEnabled { get; init; } = true;

    public bool FieldReadDiscoveryEnabled { get; init; } = true;

    public bool ExpressionCseEnabled { get; init; } = true;

    public bool CrossNodeExpressionCseEnabled { get; init; }

    public static OptimizationOptions Default { get; } = new();
}
