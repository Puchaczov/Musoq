using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Planning.Cardinality;

internal sealed record CardinalityFactPlanningResult(
    IReadOnlyList<CardinalityFact> Facts,
    IReadOnlyList<PlanningDecision> Decisions);
