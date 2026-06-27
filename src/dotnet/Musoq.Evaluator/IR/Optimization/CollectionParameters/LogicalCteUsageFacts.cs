using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Expressions.CollectionParameters;

namespace Musoq.Evaluator.IR.Optimization;

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
