using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private static void ThrowImplicitLateralDerivedTable(
        DerivedTableFromNode derived,
        SubqueryCorrelationInfo correlation)
    {
        ThrowUnsupportedDerivedCorrelation(derived,
            $"Plain derived tables are not lateral. Use CROSS APPLY or OUTER APPLY for references to outer alias '{correlation.CorrelatedAliases.First()}'.",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["constraint"] = "non-lateral-derived-table",
                ["alias"] = derived.Alias,
                ["outerAlias"] = correlation.CorrelatedAliases.First(),
                ["allowedOperators"] = "CROSS APPLY, OUTER APPLY"
            });
    }
}
