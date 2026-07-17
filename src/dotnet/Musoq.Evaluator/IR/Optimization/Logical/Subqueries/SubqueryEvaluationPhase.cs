namespace Musoq.Evaluator.IR.Optimization.Logical.Subqueries;

internal enum SubqueryEvaluationPhase
{
    Source,
    Filter,
    Grouping,
    Having,
    Window,
    Qualify,
    Projection,
    Ordering,
    RowLimit
}
