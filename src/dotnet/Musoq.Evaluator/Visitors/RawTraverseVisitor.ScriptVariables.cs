using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class RawTraverseVisitor<TExpressionVisitor>
    where TExpressionVisitor : class, IExpressionVisitor
{
    public virtual void Visit(ScriptVariableDeclarationNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ScriptVariableReferenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }
}
