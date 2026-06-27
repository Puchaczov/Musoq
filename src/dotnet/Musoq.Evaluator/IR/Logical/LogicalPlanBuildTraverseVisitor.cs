using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.IR.Logical;

public sealed class LogicalPlanBuildTraverseVisitor(LogicalPlanBuilder visitor)
    : RawTraverseVisitor<LogicalPlanBuilder>(visitor)
{
    public LogicalNode? Result => Visitor.Result;

    public override void Visit(QueryNode node)
    {
        TraverseQuery(node);
        node.Accept(Visitor);
    }

    public override void Visit(InternalQueryNode node)
    {
        TraverseQuery(node);
        node.Refresh?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(SelectNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(WhereNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(GroupByNode node)
    {
        node.Having?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(HavingNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(OrderByNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(SkipNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(TakeNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(QualifyNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(SchemaFromNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(InMemoryTableFromNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        node.First.Accept(this);
        node.Second.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
        node.First.Accept(this);
        node.Second.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(UnpivotFromNode node)
    {
        node.Source.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(JoinFromNode node)
    {
        node.Source.Accept(this);
        node.With.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(ApplyFromNode node)
    {
        node.Source.Accept(this);
        node.With.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(FieldNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(FieldOrderedNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(WindowFunctionNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(DescNode node)
    {
        if (node.Type == DescForType.Query)
            node.Query?.Accept(this);

        node.Accept(Visitor);
    }

    public override void Visit(CteExpressionNode node)
    {
        foreach (var inner in node.InnerExpression)
            inner.Accept(this);
        node.OuterExpression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(CteInnerExpressionNode node)
    {
        node.Value.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(UnionNode node)
    {
        TraverseSetOperator(node);
    }

    public override void Visit(UnionAllNode node)
    {
        TraverseSetOperator(node);
    }

    public override void Visit(ExceptNode node)
    {
        TraverseSetOperator(node);
    }

    public override void Visit(IntersectNode node)
    {
        TraverseSetOperator(node);
    }

    private void TraverseSetOperator(SetOperatorNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(RootNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(MultiStatementNode node)
    {
        Visitor.EnterMultiStatement();
        foreach (var child in node.Nodes)
            child.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(SingleSetNode node)
    {
        node.Query.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(Node node)
    {
        if (node is InMemoryGroupedFromNode)
        {
            node.Accept(Visitor);
            return;
        }

        base.Visit(node);
    }

    private void TraverseQuery(QueryNode node)
    {
        node.From.Accept(this);
        node.Where?.Accept(this);
        node.Skip?.Accept(this);
        node.Take?.Accept(this);
        node.GroupBy?.Accept(this);
        node.Window?.Accept(this);
        node.Qualify?.Accept(this);
        node.Select.Accept(this);
        node.OrderBy?.Accept(this);
    }
}
