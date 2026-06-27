using System.Collections.Generic;
using Musoq.Evaluator;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed record OptimizationTraceEntry(
    OptimizationStage Stage,
    string PassName,
    int Iteration,
    bool IsChanged,
    string Outcome,
    string Reason);
