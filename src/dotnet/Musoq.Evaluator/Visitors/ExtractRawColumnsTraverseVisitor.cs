using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public class ExtractRawColumnsTraverseVisitor(IQueryPartAwareExpressionVisitor visitor)
    : RawTraverseVisitor<IQueryPartAwareExpressionVisitor>(visitor)
{
    public override void Visit(SelectNode node)
    {
        SetQueryPart(QueryPart.Select);
        foreach (var field in node.Fields)
            field.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(GroupSelectNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(DotNode node)
    {
        var self = node;
        var theMostOuter = self;
        while (self is not null)
        {
            theMostOuter = self;
            self = self.Root as DotNode;
        }

        var ident = theMostOuter.Root as IdentifierNode;
        if (ident != null && node == theMostOuter)
        {
            IdentifierNode column;
            if (theMostOuter.Expression is DotNode dotNode)
                column = dotNode.Root as IdentifierNode;
            else
                column = theMostOuter.Expression as IdentifierNode;

            if (column != null)
            {
                Visit(new AccessColumnNode(column.Name, ident.Name, TextSpan.Empty));
                return;
            }
        }

        self = node;

        while (self is not null)
        {
            self.Root.Accept(this);
            self.Expression.Accept(this);
            self.Accept(Visitor);

            self = self.Expression as DotNode;
        }
    }

    public override void Visit(WhereNode node)
    {
        SetQueryPart(QueryPart.Where);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(GroupByNode node)
    {
        SetQueryPart(QueryPart.GroupBy);

        foreach (var field in node.Fields)
            field.Accept(this);

        node.Having?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(HavingNode node)
    {
        SetQueryPart(QueryPart.Having);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.SourceTable.Accept(this);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.SourceTable.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(SchemaFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.Parameters.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(JoinSourcesTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.Expression.Accept(this);
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(Visitor);
    }

    public override void Visit(ApplySourcesTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(Visitor);
    }

    public override void Visit(InMemoryTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.Accept(Visitor);
    }

    public override void Visit(JoinFromNode node)
    {
        SetQueryPart(QueryPart.From);
        var joins = new Stack<JoinFromNode>();

        var join = node;
        while (join != null)
        {
            joins.Push(join);
            join = join.Source as JoinFromNode;
        }

        join = joins.Pop();
        join.Source.Accept(this);
        join.With.Accept(this);

        join.Expression.Accept(this);
        join.Accept(Visitor);

        while (joins.Count > 0)
        {
            join = joins.Pop();
            join.With.Accept(this);
            join.Expression.Accept(this);
            join.Accept(Visitor);
        }
    }

    public override void Visit(ApplyFromNode node)
    {
        SetQueryPart(QueryPart.From);
        var joins = new Stack<ApplyFromNode>();

        var apply = node;
        while (apply != null)
        {
            joins.Push(apply);
            apply = apply.Source as ApplyFromNode;
        }

        apply = joins.Pop();
        apply.Source.Accept(this);
        apply.With.Accept(this);

        apply.Accept(Visitor);

        while (joins.Count > 0)
        {
            apply = joins.Pop();
            apply.With.Accept(this);
            apply.Accept(Visitor);
        }
    }

    public override void Visit(ExpressionFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(InterpretFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.InterpretCall.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(AliasedFromNode node)
    {
        node.Accept(Visitor);
    }

    public override void Visit(CreateTransformationTableNode node)
    {
        SetQueryPart(QueryPart.None);
        foreach (var item in node.Fields)
            item.Accept(this);

        node.Accept(Visitor);
    }

    public override void Visit(QueryNode node)
    {
        Visitor.QueryBegins();
        node.From.Accept(this);
        node.Where?.Accept(this);
        node.GroupBy?.Accept(this);
        node.Select.Accept(this);
        node.Skip?.Accept(this);
        node.Take?.Accept(this);
        node.Window?.Accept(this);
        node.OrderBy?.Accept(this);
        node.Accept(Visitor);
        SetQueryPart(QueryPart.None);
        Visitor.QueryEnds();
    }

    public override void Visit(CteExpressionNode node)
    {
        foreach (var exp in node.InnerExpression) exp.Accept(this);
        node.OuterExpression.Accept(this);
        node.Accept(Visitor);
    }

    public void SetQueryPart(QueryPart part)
    {
        Visitor.SetQueryPart(part);
    }
}
