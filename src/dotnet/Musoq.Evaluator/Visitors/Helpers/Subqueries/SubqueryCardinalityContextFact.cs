namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed record SubqueryCardinalityContextFact(
    SubqueryCardinalityContextKind Kind,
    string Reason);
