using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class AggregateRefRewriter
{
    protected override IrExpression VisitCollectionInCheck(CollectionInCheck node)
    {
        var expression = Visit(node.Expression);
        return ReferenceEquals(expression, node.Expression)
            ? node
            : new CollectionInCheck(expression, node.Collection, node.ElementType, node.ReturnType);
    }
}
