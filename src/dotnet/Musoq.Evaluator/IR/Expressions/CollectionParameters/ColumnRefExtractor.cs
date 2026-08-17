using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ColumnRefExtractor
{
    protected override IReadOnlyList<ColumnRef> VisitCollectionInCheck(CollectionInCheck node)
    {
        Visit(node.Expression);
        return _columns;
    }
}
