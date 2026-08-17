using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourcePredicatePlan(
    string SourceContextId,
    string Alias,
    WhereNode PushedWhereNode,
    IrExpression[] PushedPredicates,
    string Reason,
    PlanningConfidence Confidence);
