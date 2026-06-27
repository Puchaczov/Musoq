using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser;

/// <summary>
///     Base class that provides empty (no-op) implementations for all IExpressionVisitor methods.
///     Derived classes can selectively override only the Visit methods they need to handle.
/// </summary>
public abstract partial class NoOpExpressionVisitor
{
    public virtual void Visit(SingleSetNode node)
    {
    }

    public virtual void Visit(UnionNode node)
    {
    }

    public virtual void Visit(UnionAllNode node)
    {
    }

    public virtual void Visit(ExceptNode node)
    {
    }

    public virtual void Visit(RefreshNode node)
    {
    }

    public virtual void Visit(IntersectNode node)
    {
    }

    public virtual void Visit(PutTrueNode node)
    {
    }

    public virtual void Visit(MultiStatementNode node)
    {
    }

    public virtual void Visit(StatementsArrayNode node)
    {
    }

    public virtual void Visit(StatementNode node)
    {
    }

    public virtual void Visit(CteExpressionNode node)
    {
    }

    public virtual void Visit(CteInnerExpressionNode node)
    {
    }

    public virtual void Visit(JoinNode node)
    {
    }

    public virtual void Visit(ApplyNode node)
    {
    }

    public virtual void Visit(OrderByNode node)
    {
    }

    public virtual void Visit(CreateTableNode node)
    {
    }

    public virtual void Visit(CoupleNode node)
    {
    }

}
