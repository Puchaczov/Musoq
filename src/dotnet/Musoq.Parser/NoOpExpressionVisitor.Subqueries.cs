using Musoq.Parser.Nodes;

namespace Musoq.Parser;

public abstract partial class NoOpExpressionVisitor
{
    public virtual void Visit(InQueryNode node)
    {
    }

    public virtual void Visit(ExistsQueryNode node)
    {
    }

    public virtual void Visit(ScalarSubqueryNode node)
    {
    }
}
