using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Expressions;

public abstract partial class ExpressionArrayRewriter
{
    protected override IrExpression VisitCollectionInCheck(CollectionInCheck node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var expression = Visit(node.Expression);
        return ReferenceEquals(expression, node.Expression)
            ? node
            : node with { Expression = expression };
    }
}
