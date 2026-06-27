using Musoq.Parser.Nodes;

namespace Musoq.Parser;

/// <summary>
///     Base class that provides empty (no-op) implementations for all IExpressionVisitor methods.
///     Derived classes can selectively override only the Visit methods they need to handle.
/// </summary>
public abstract partial class NoOpExpressionVisitor
{
    public virtual void Visit(CaseNode node)
    {
    }

    public virtual void Visit(WhenNode node)
    {
    }

    public virtual void Visit(ThenNode node)
    {
    }

    public virtual void Visit(ElseNode node)
    {
    }

    public virtual void Visit(BitwiseAndNode node)
    {
    }

    public virtual void Visit(BitwiseOrNode node)
    {
    }

    public virtual void Visit(BitwiseXorNode node)
    {
    }

    public virtual void Visit(LeftShiftNode node)
    {
    }

    public virtual void Visit(RightShiftNode node)
    {
    }

    public virtual void Visit(ArrayIndexNode node)
    {
    }


}
