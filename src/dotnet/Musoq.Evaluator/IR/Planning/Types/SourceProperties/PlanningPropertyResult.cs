using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record PlanningPropertyResult(
    PlanningFacts Facts,
    IReadOnlyList<PlanningDecision> Decisions)
{
    public PlanProperties Properties => Facts.ToPlanProperties();
}
