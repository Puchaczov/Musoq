using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal static partial class LogicalCteUsageFacts
{
    private sealed partial class CteTableRefCollector
    {
        protected override IReadOnlyList<string> VisitCollectionInCheck(CollectionInCheck node)
        {
            Visit(node.Expression);
            return _references;
        }
    }
}

