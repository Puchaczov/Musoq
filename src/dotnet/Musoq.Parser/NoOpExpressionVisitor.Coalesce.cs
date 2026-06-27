using Musoq.Parser.Nodes;

namespace Musoq.Parser;

public abstract partial class NoOpExpressionVisitor
{
    public virtual void Visit(CoalesceNode node)
    {
    }
}