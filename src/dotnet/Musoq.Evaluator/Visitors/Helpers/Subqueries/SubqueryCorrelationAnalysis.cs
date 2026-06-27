using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed record SubqueryCorrelationAnalysis(
    IReadOnlyList<SubqueryCorrelationInfo> Subqueries,
    IReadOnlySet<string> IllegalOuterConsumingCteAliases)
{
    public bool HasIllegalOuterConsumingCteReferences => IllegalOuterConsumingCteAliases.Count > 0;
}
