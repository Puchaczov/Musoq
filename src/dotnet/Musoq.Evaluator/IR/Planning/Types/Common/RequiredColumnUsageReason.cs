namespace Musoq.Evaluator.IR.Planning;

internal enum RequiredColumnUsageReason
{
    Projection,
    SourceArgument,
    Where,
    JoinPredicate,
    ApplyCorrelation,
    GroupBy,
    AggregateSetArgument,
    AggregateGetArgument,
    Having,
    OrderBy,
    WindowPartition,
    WindowOrder,
    WindowValue,
    Qualify,
    SetOperationKey,
    HiddenIntermediateProjection
}
