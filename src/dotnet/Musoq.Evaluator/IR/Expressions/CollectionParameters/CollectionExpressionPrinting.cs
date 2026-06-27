using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class IrExpressionPrinter
{
    protected override string VisitCollectionInCheck(CollectionInCheck node)
    {
        return $"{Visit(node.Expression)} IN {Visit(node.Collection)}";
    }
}
