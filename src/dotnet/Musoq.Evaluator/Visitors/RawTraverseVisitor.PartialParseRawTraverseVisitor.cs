using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class RawTraverseVisitor<TExpressionVisitor>
    where TExpressionVisitor : class, IExpressionVisitor
{
    public virtual void Visit(PartialParseCallNode node)
    {
        VisitChildrenThenNode(node);
    }
}
