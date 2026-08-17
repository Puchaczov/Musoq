namespace Musoq.Evaluator.IR.Planning;

internal sealed record PlanningDecision(
    PlanningDecisionCategory Category,
    string RuleName,
    string Target,
    string Outcome,
    PlanningConfidence Confidence,
    string Reason);
