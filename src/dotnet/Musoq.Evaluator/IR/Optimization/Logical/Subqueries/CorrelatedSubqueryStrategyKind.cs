namespace Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

internal enum CorrelatedSubqueryStrategyKind
{
    HashSemiJoin,
    HashAntiJoin,
    HashMarkJoin,
    HashSingleJoin,
    PartitionedTopOffset,
    Apply,
    Unsupported
}
