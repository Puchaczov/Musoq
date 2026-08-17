namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal enum SubqueryCardinalityContextKind
{
    ScalarSubquery,
    Distinct,
    GroupBy,
    OrderBy,
    Skip,
    Take,
    Window,
    Qualify,
    SetOperation
}
