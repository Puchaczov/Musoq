using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning.Subqueries;

internal sealed record SubqueryLoweringStrategyDecision(
    string CteName,
    SubqueryLoweringKind Kind,
    JoinKind? JoinKind,
    bool IsCorrelated,
    string Reason);
