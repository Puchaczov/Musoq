using System;
using System.Collections.Generic;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public class RewriteQueryTraverseVisitor : IExpressionVisitor
{
    private readonly IScopeAwareExpressionVisitor _visitor;
    private ScopeWalker _walker;

    public RewriteQueryTraverseVisitor(IScopeAwareExpressionVisitor visitor, ScopeWalker walker)
    {
        _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
        _walker = walker;
    }

    public void Visit(SelectNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GroupSelectNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);
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
        node.Root.Accept(this);
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessCallChainNode node)
    {
        node.Accept(_visitor);
    }

    public virtual void Visit(WhereNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GroupByNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);

        node.Having?.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(HavingNode node)
    {
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
        node.SourceTable.Accept(this);
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        node.SourceTable.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(SchemaFromNode node)
    {
        node.Parameters.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
        node.Expression.Accept(this);
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(_visitor);
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
        node.First.Accept(this);
        node.Second.Accept(this);
            
        node.Accept(_visitor);
    }

    public void Visit(InMemoryTableFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(JoinFromNode node)
    {
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

        while (joins.Count > 1)
        {
            join = joins.Pop();
            join.With.Accept(this);
            join.Expression.Accept(this);
            join.Accept(_visitor);
        }

        if (joins.Count <= 0) return;

        join = joins.Pop();
        join.With.Accept(this);
        join.Expression.Accept(this);
        join.Accept(_visitor);
    }

    public void Visit(ApplyFromNode node)
    {
        var applies = new Stack<ApplyFromNode>();

        var apply = node;
        while (apply != null)
        {
            applies.Push(apply);
            apply = apply.Source as ApplyFromNode;
        }

        apply = applies.Pop();

        apply.Source.Accept(this);
        apply.With.Accept(this);
        apply.Accept(_visitor);

        while (applies.Count > 1)
        {
            apply = applies.Pop();
            apply.With.Accept(this);
            apply.Accept(_visitor);
        }

        if (applies.Count <= 0) return;

        apply = applies.Pop();
        apply.With.Accept(this);
        apply.Accept(_visitor);
    }

    public void Visit(ExpressionFromNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AccessMethodFromNode node)
    {
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
        _walker = _walker.NextChild();
        _visitor.SetScope(_walker.Scope);

        node.From.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);

        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.OrderBy?.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
        _visitor.SetScope(_walker.Scope);
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
        throw new NotSupportedException();
    }

    public void Visit(DescNode node)
    {
        _walker = _walker.NextChild();
        _visitor.SetScope(_walker.Scope);

        node.From.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
        _visitor.SetScope(_walker.Scope);
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
        _walker = _walker.NextChild();

        node.From.Accept(this);
        node.Where.Accept(this);
        node.Select.Accept(this);

        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.Refresh?.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
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
        _walker = _walker.NextChild();
        _visitor.SetScope(_walker.Scope);

        foreach (var exp in node.InnerExpression) exp.Accept(this);
        node.OuterExpression.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
        _visitor.SetScope(_walker.Scope);
    }

    public void Visit(CteInnerExpressionNode node)
    {
        _walker = _walker.NextChild();
        _visitor.SetScope(_walker.Scope);

        node.Value.Accept(this);
        node.Accept(_visitor);

        _walker = _walker.Parent();
        _visitor.SetScope(_walker.Scope);
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

        for (int i = node.WhenThenPairs.Length - 1; i >= 0; --i)
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

    private void TraverseSetOperator(SetOperatorNode node)
    {
        _walker = _walker.NextChild();
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
        _walker = _walker.Parent();
    }
}