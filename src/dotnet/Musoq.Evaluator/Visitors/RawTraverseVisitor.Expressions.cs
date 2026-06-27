using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class RawTraverseVisitor<TExpressionVisitor>    where TExpressionVisitor : class, IExpressionVisitor

{
    public virtual void Visit(OrNode node) => VisitBinaryNode(node);

    public virtual void Visit(ShortCircuitingNodeLeft node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ShortCircuitingNodeRight node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(HyphenNode node) => VisitBinaryNode(node);

    public virtual void Visit(AndNode node) => VisitBinaryNode(node);

    public virtual void Visit(EqualityNode node) => VisitBinaryNode(node);

    public virtual void Visit(IsDistinctFromNode node) => VisitBinaryNode(node);

    public virtual void Visit(GreaterOrEqualNode node) => VisitBinaryNode(node);

    public virtual void Visit(LessOrEqualNode node) => VisitBinaryNode(node);

    public virtual void Visit(GreaterNode node) => VisitBinaryNode(node);

    public virtual void Visit(LessNode node) => VisitBinaryNode(node);

    public virtual void Visit(DiffNode node) => VisitBinaryNode(node);

    public virtual void Visit(NotNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(LikeNode node) => VisitBinaryNode(node);

    public virtual void Visit(RLikeNode node) => VisitBinaryNode(node);

    public virtual void Visit(InNode node) => VisitBinaryNode(node);

    public virtual void Visit(CollectionInNode node) => VisitBinaryNode(node);

    public virtual void Visit(InQueryNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ExistsQueryNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ScalarSubqueryNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(BetweenNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(FieldNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(FieldOrderedNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ArgsListNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(DecimalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(Node node)
    {
        throw new NotSupportedException();
    }

    public virtual void Visit(DescNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(StarNode node) => VisitBinaryNode(node);

    public virtual void Visit(FSlashNode node) => VisitBinaryNode(node);

    public virtual void Visit(ModuloNode node) => VisitBinaryNode(node);

    public virtual void Visit(AddNode node) => VisitBinaryNode(node);

    public virtual void Visit(BitwiseAndNode node) => VisitBinaryNode(node);

    public virtual void Visit(BitwiseOrNode node) => VisitBinaryNode(node);

    public virtual void Visit(BitwiseXorNode node) => VisitBinaryNode(node);

    public virtual void Visit(LeftShiftNode node) => VisitBinaryNode(node);

    public virtual void Visit(RightShiftNode node) => VisitBinaryNode(node);

    public virtual void Visit(CoalesceNode node) => VisitBinaryNode(node);

    public virtual void Visit(ArrayIndexNode node)
    {
        VisitChildrenThenNode(node);
    }
}
