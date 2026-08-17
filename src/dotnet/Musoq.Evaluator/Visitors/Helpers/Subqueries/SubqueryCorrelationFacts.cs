using System.Collections.Generic;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed record SubqueryCorrelationFacts(
    IReadOnlySet<string> LocalAliases,
    IReadOnlySet<string> OuterAliases,
    IReadOnlySet<string> CorrelatedAliases,
    IReadOnlyList<SubqueryCorrelationKeyFact> EqualityKeys,
    SubqueryCorrelationNullSemantics NullSemantics,
    IReadOnlyList<SubqueryCardinalityContextFact> CardinalitySensitiveContexts)
{
    public bool HasEqualityKeys => EqualityKeys.Count > 0;

    public bool IsCardinalitySensitive => CardinalitySensitiveContexts.Count > 0;
}
