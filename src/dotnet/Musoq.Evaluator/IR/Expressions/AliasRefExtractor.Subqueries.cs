using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class AliasRefExtractor
{
    protected override IReadOnlyList<string> VisitSubquery(SubqueryExpression node)
    {
        foreach (var input in node.Inputs)
            Visit(input);
        return _aliases.ToArray();
    }

    protected override IReadOnlyList<string> VisitSubqueryParameter(SubqueryParameterRef node) => [];
}
