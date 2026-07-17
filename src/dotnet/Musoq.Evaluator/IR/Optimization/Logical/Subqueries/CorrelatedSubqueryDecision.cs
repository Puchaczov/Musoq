namespace Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

internal sealed record CorrelatedSubqueryDecision(
    CorrelatedSubqueryRewriteRequest Request,
    CorrelatedSubqueryStrategyKind Strategy,
    string Reason);
