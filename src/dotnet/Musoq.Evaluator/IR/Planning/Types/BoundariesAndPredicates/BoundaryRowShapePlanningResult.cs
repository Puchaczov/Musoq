using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record BoundaryRowShapePlanningResult(
    IReadOnlyList<BoundaryRowShapePlan> Plans,
    IReadOnlyList<PlanningDecision> Decisions);
