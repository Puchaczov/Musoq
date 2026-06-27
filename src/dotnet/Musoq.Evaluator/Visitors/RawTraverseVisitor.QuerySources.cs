using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;
public partial class RawTraverseVisitor<TExpressionVisitor>    where TExpressionVisitor : class, IExpressionVisitor

{
    public virtual void Visit(WhereNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(GroupByNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(HavingNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(SkipNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(TakeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(SchemaFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(JoinSourcesTableFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ApplySourcesTableFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(InMemoryTableFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ValuesFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(JoinFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ApplyFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(ExpressionFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(InterpretFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(SchemaMethodFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessMethodFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(PropertyFromNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(AliasedFromNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(CreateTransformationTableNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(RenameTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(TranslatedSetTreeNode node)
    {
        VisitChildrenThenNode(node);
    }

    public virtual void Visit(IntoNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(QueryScope node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(ShouldBePresentInTheTable node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }

    public virtual void Visit(TranslatedSetOperatorNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        foreach (var item in node.CreateTableNodes)
            item.Accept(Visitor);

        node.FQuery.Accept(this);
        node.SQuery.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(QueryNode node)
    {
        VisitChildrenThenNode(node);
    }
}
