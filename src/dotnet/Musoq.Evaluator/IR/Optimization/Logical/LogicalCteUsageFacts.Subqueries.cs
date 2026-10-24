using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal static partial class LogicalCteUsageFacts
{
    private sealed partial class CteTableRefCollector
    {
        protected override IReadOnlyList<string> VisitSubquery(SubqueryExpression node)
        {
            foreach (var input in node.Inputs)
                Visit(input);
            _references.AddRange(CollectCteReferences(node.Query));
            return _references;
        }

        protected override IReadOnlyList<string> VisitSubqueryParameter(SubqueryParameterRef node) => [];
    }
}
