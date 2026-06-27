using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class RawTraverseVisitor<TExpressionVisitor> : IExpressionVisitor
    where TExpressionVisitor : class, IExpressionVisitor
{
    public virtual void Visit(RowPresenceNode node)
    {
        VisitChildrenThenNode(node);
    }
}
