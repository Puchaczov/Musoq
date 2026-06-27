using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed record SubqueryCorrelationInfo(
    Node Node,
    IReadOnlySet<string> LocalAliases,
    IReadOnlySet<string> OuterAliases,
    IReadOnlySet<string> CorrelatedAliases,
    IReadOnlySet<string> IllegalOuterConsumingCteAliases,
    SubqueryCorrelationFacts Facts,
    bool IsInsideCteDefinition)
{
    public bool IsCorrelated => CorrelatedAliases.Count > 0;

    public bool HasIllegalOuterConsumingCteReferences => IllegalOuterConsumingCteAliases.Count > 0;
}
