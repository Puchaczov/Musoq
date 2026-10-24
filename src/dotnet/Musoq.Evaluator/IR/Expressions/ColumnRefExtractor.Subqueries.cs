using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ColumnRefExtractor
{
    protected override IReadOnlyList<ColumnRef> VisitSubquery(SubqueryExpression node)
    {
        foreach (var input in node.Inputs)
            Visit(input);
        return _columns;
    }

    protected override IReadOnlyList<ColumnRef> VisitSubqueryParameter(SubqueryParameterRef node) => [];
}
