using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionPlan(
    string Identifier,
    IReadOnlyList<RowShape> Shapes,
    ExecutionBlock Body,
    FinalShapeResult? FinalResult = null);
