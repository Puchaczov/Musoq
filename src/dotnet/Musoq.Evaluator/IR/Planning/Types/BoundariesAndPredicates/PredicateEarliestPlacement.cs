namespace Musoq.Evaluator.IR.Planning;

internal enum PredicateEarliestPlacement
{
    ConstantPredicate,
    SourcePushdown,
    SourceRuntimeFilter,
    PreInnerJoinLeft,
    PreInnerJoinRight,
    PreApplyRight,
    PostJoin,
    PostAggregate,
    PostWindow,
    RuntimeFilter
}
