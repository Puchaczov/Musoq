using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Planning.Cardinality;

internal sealed record CardinalityFactPlanningResult(
    IReadOnlyList<CardinalityFact> Facts,
    IReadOnlyList<PlanningDecision> Decisions);
