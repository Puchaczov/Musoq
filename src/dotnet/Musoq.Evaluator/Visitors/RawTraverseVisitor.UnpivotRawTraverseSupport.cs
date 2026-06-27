using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class RawTraverseVisitor<TExpressionVisitor>
{
    public virtual void Visit(UnpivotFromNode node)
    {
        VisitChildrenThenNode(node);
    }
}
