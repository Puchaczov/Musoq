using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

internal sealed record CorrelatedSubqueryRewriteRequest(
    Node Node,
    SubqueryCorrelationFacts Correlation,
    SubqueryEvaluationPhase EvaluationPhase,
    bool IsDirectFilter,
    bool IsNegated)
{
    public bool IsScalar => Node is ScalarSubqueryNode;

    public bool IsPredicate => Node is InQueryNode or ExistsQueryNode;
}
