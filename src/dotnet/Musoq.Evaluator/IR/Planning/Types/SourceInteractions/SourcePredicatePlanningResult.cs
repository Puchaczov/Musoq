using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourcePredicatePlanningResult(
    IReadOnlyDictionary<string, SourcePredicatePlan> PlansBySourceId,
    IReadOnlyDictionary<string, IrExpression[]> PushedPredicatesBySourceId,
    IReadOnlyList<PlanningDecision> Decisions);
