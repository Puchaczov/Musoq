using System.Collections.Generic;
using Musoq.Parser;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed record SubqueryCorrelationAnalysis(
    IReadOnlyList<SubqueryCorrelationInfo> Subqueries,
    IReadOnlySet<string> IllegalOuterConsumingCteAliases,
    TextSpan IllegalOuterConsumingCteReferenceSpan)
{
    public bool HasIllegalOuterConsumingCteReferences => IllegalOuterConsumingCteAliases.Count > 0;
}
