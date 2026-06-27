using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed partial class LogicalConstantExpressionFolder
{
    protected override IrExpression VisitCollectionInCheck(CollectionInCheck node)
    {
        var expression = Visit(node.Expression);
        return ReferenceEquals(expression, node.Expression)
            ? node
            : node with { Expression = expression };
    }
}
