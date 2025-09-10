using System;
using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public class ExtractRawColumnsTraverseVisitor(IQueryPartAwareExpressionVisitor visitor) : IExpressionVisitor
{
    private readonly IQueryPartAwareExpressionVisitor _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));

    public void Visit(SelectNode node)
    {
        SetQueryPart(QueryPart.Select);
        foreach (var field in node.Fields)
            field.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GroupSelectNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(StringNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(IntegerNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(HexIntegerNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(BinaryIntegerNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(OctalIntegerNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(BooleanNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(WordNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(NullNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(ContainsNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessMethodNode node)
    {
        node.Arguments.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessRawIdentifierNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(IsNullNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessRefreshAggregationScoreNode node)
    {
        node.Arguments.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessColumnNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(AllColumnsNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(IdentifierNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(AccessObjectArrayNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(AccessObjectKeyNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(PropertyValueNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(DotNode node)
    {
        var self = node;
        var theMostOuter = self;
        while (!(self is null))
        {
            theMostOuter = self;
            self = self.Root as DotNode;
        }

        var ident = (IdentifierNode) theMostOuter.Root;
        if (node == theMostOuter)
        {
            IdentifierNode column;
            if (theMostOuter.Expression is DotNode dotNode)
            {
                column = (IdentifierNode) dotNode.Root;
            }
            else
            {
                column = (IdentifierNode) theMostOuter.Expression;
            }

            Visit(new AccessColumnNode(column.Name, ident.Name, TextSpan.Empty));
            return;
        }

        self = node;
            
        while (self is not null)
        {
            self.Root.Accept(this);
            self.Expression.Accept(this);
            self.Accept(_visitor);

            self = self.Expression as DotNode;
        }
    }

    public void Visit(AccessCallChainNode node)
    {
        node.Accept(_visitor);
    }

    public virtual void Visit(WhereNode node)
    {
        SetQueryPart(QueryPart.Where);
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GroupByNode node)
    {
        SetQueryPart(QueryPart.GroupBy);

        foreach (var field in node.Fields)
            field.Accept(this);

        node.Having?.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(HavingNode node)
    {
        SetQueryPart(QueryPart.Having);
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(SkipNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(TakeNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.SourceTable.Accept(this);
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.SourceTable.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(SchemaFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.Parameters.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.Expression.Accept(this);
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(InMemoryTableFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.Accept(_visitor);
    }

    public void Visit(JoinFromNode node)
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
        join.Accept(_visitor);

        while (joins.Count > 0)
        {
            join = joins.Pop();
            join.With.Accept(this);
            join.Expression.Accept(this);
            join.Accept(_visitor);
        }
    }

    public void Visit(ApplyFromNode node)
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

        apply.Accept(_visitor);

        while (joins.Count > 0)
        {
            apply = joins.Pop();
            apply.With.Accept(this);
            apply.Accept(_visitor);
        }
    }

    public void Visit(ExpressionFromNode node)
    {
        SetQueryPart(QueryPart.From);
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessMethodFromNode node)
    {
        node.AccessMethod.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(SchemaMethodFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(PropertyFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(AliasedFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(CreateTransformationTableNode node)
    {
        SetQueryPart(QueryPart.None);
        foreach (var item in node.Fields)
            item.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(RenameTableNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(TranslatedSetTreeNode node)
    {
        foreach (var item in node.Nodes)
            item.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(IntoNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(QueryScope node)
    {
        node.Accept(_visitor);
    }

    public void Visit(ShouldBePresentInTheTable node)
    {
        node.Accept(_visitor);
    }

    public void Visit(TranslatedSetOperatorNode node)
    {
        foreach (var item in node.CreateTableNodes)
            item.Accept(_visitor);

        node.FQuery.Accept(this);
        node.SQuery.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(QueryNode node)
    {
        _visitor.QueryBegins();
        node.From.Accept(this);
        node.Where?.Accept(this);
        node.GroupBy?.Accept(this);
        node.Select.Accept(this);
        node.Skip?.Accept(this);
        node.Take?.Accept(this);
        node.OrderBy?.Accept(this);
        node.Accept(_visitor);
        SetQueryPart(QueryPart.None);
        _visitor.QueryEnds();
    }

    public void Visit(OrNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ShortCircuitingNodeLeft node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ShortCircuitingNodeRight node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(HyphenNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AndNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(EqualityNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GreaterOrEqualNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(LessOrEqualNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GreaterNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(LessNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(DiffNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(NotNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(LikeNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(RLikeNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(InNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(FieldNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(FieldOrderedNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ArgsListNode node)
    {
        foreach (var item in node.Args)
            item.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(DecimalNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(Node node)
    {
        throw new NotSupportedException("Node cannot be visited.");
    }

    public void Visit(DescNode node)
    {
        node.From.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(StarNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(FSlashNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ModuloNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AddNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(InternalQueryNode node)
    {
    }

    public void Visit(RootNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(SingleSetNode node)
    {
        node.Query.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(UnionNode node)
    {
        TraverseSetOperator(node);
    }

    public void Visit(UnionAllNode node)
    {
        TraverseSetOperator(node);
    }

    public void Visit(ExceptNode node)
    {
        TraverseSetOperator(node);
    }

    public void Visit(RefreshNode node)
    {
        foreach (var item in node.Nodes)
            item.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(IntersectNode node)
    {
        TraverseSetOperator(node);
    }

    public void Visit(PutTrueNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(MultiStatementNode node)
    {
        foreach (var cNode in node.Nodes)
            cNode.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(CteExpressionNode node)
    {
        foreach (var exp in node.InnerExpression) exp.Accept(this);
        node.OuterExpression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(CteInnerExpressionNode node)
    {
        node.Value.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(JoinNode node)
    {
        node.Join.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ApplyNode node)
    {
        node.Apply.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(FromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(OrderByNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);

        node.Accept(_visitor);
    }

    private void TraverseSetOperator(SetOperatorNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void SetQueryPart(QueryPart part)
    {
        _visitor.SetQueryPart(part);
    }

    public void Visit(CreateTableNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(CoupleNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(StatementsArrayNode node)
    {
        foreach (var statement in node.Statements)
            statement.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(StatementNode node)
    {
        node.Node.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(CaseNode node)
    {   
        node.Else.Accept(this);
            
        for (var i = node.WhenThenPairs.Length - 1; i >= 0; --i)
        {
            node.WhenThenPairs[i].When.Accept(this);
            node.WhenThenPairs[i].Then.Accept(this);
        }

        node.Accept(_visitor);
    }

    public void Visit(WhenNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ThenNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ElseNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(FieldLinkNode node)
    {
        node.Accept(_visitor);
    }
}