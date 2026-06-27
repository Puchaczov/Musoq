using Musoq.Parser.Nodes.From;

namespace Musoq.Parser;

public abstract partial class NoOpExpressionVisitor
{
    public virtual void Visit(UnpivotFromNode node)
    {
    }
}
