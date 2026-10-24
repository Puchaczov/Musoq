using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class RowPresenceAliasExtractor
{
    protected override IReadOnlyList<string> VisitSubquery(SubqueryExpression node)
    {
        foreach (var input in node.Inputs)
            Visit(input);
        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitSubqueryParameter(SubqueryParameterRef node) => [];
}
