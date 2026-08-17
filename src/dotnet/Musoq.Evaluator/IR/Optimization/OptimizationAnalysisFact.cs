using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record OptimizationAnalysisFact(
    string Key,
    object? Value,
    Type ValueType,
    OptimizationAnalysisInvalidationRule InvalidationRule,
    OptimizationStage ProducedAtStage,
    string? ProducedByPass,
    int ProducedAtIteration,
    IReadOnlyList<string> Consumers)
{
    internal int ProducedInPassRun { get; init; }
}
