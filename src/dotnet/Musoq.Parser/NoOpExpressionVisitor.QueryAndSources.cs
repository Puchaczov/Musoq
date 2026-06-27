using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser;

/// <summary>
///     Base class that provides empty (no-op) implementations for all IExpressionVisitor methods.
///     Derived classes can selectively override only the Visit methods they need to handle.
/// </summary>
public abstract partial class NoOpExpressionVisitor
{
    public virtual void Visit(SelectNode node)
    {
    }

    public virtual void Visit(GroupSelectNode node)
    {
    }

    public virtual void Visit(WhereNode node)
    {
    }

    public virtual void Visit(GroupByNode node)
    {
    }

    public virtual void Visit(HavingNode node)
    {
    }

    public virtual void Visit(SkipNode node)
    {
    }

    public virtual void Visit(TakeNode node)
    {
    }

    public virtual void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
    }

    public virtual void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
    }

    public virtual void Visit(SchemaFromNode node)
    {
    }

    public virtual void Visit(AliasedFromNode node)
    {
    }

    public virtual void Visit(JoinSourcesTableFromNode node)
    {
    }

    public virtual void Visit(ApplySourcesTableFromNode node)
    {
    }

    public virtual void Visit(InMemoryTableFromNode node)
    {
    }

    public virtual void Visit(ValuesFromNode node)
    {
    }

    public virtual void Visit(JoinFromNode node)
    {
    }

    public virtual void Visit(ApplyFromNode node)
    {
    }

    public virtual void Visit(ExpressionFromNode node)
    {
    }

    public virtual void Visit(InterpretFromNode node)
    {
    }

    public virtual void Visit(SchemaMethodFromNode node)
    {
    }

    public virtual void Visit(PropertyFromNode node)
    {
    }

    public virtual void Visit(AccessMethodFromNode node)
    {
    }

    public virtual void Visit(CreateTransformationTableNode node)
    {
    }

    public virtual void Visit(RenameTableNode node)
    {
    }

    public virtual void Visit(TranslatedSetTreeNode node)
    {
    }

    public virtual void Visit(IntoNode node)
    {
    }

    public virtual void Visit(QueryScope node)
    {
    }

    public virtual void Visit(ShouldBePresentInTheTable node)
    {
    }

    public virtual void Visit(TranslatedSetOperatorNode node)
    {
    }

    public virtual void Visit(QueryNode node)
    {
    }

    public virtual void Visit(InternalQueryNode node)
    {
    }

    public virtual void Visit(RootNode node)
    {
    }

}
