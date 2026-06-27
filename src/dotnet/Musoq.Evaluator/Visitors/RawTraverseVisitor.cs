using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;
public partial class RawTraverseVisitor<TExpressionVisitor> : IExpressionVisitor
    where TExpressionVisitor : class, IExpressionVisitor
{
    protected TExpressionVisitor Visitor { get; }
    protected RawTraverseVisitor(TExpressionVisitor visitor)
    {
        Visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
    }

    protected void TraverseChildren(Node node)
    {
        ParserNodeChildTraversal.TraverseChildren(node, this);
    }

    protected void VisitChildrenThenNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        TraverseChildren(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(SelectNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(GroupSelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(StringNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(IntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(HexIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(BinaryIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(OctalIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(BooleanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(WordNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public void Visit(NullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(ParameterBlockNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ParameterDeclarationNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ParameterReferenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    protected virtual void VisitBinaryNode(BinaryNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ContainsNode node) => VisitBinaryNode(node);

    public virtual void Visit(AccessMethodNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(AccessRawIdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(IsNullNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(AccessRefreshAggregationScoreNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(AccessColumnNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(AllColumnsNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(IdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessObjectArrayNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessObjectKeyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(PropertyValueNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(DotNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(AccessCallChainNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }
}
