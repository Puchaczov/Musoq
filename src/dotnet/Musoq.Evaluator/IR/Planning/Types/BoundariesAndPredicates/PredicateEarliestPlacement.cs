namespace Musoq.Evaluator.IR.Planning;

internal enum PredicateEarliestPlacement
{
    ConstantPredicate,
    SourcePushdown,
    SourceRuntimeFilter,
    PreInnerJoinLeft,
    PreInnerJoinRight,
    PostJoin,
    PostAggregate,
    PostWindow,
    RuntimeFilter
}
