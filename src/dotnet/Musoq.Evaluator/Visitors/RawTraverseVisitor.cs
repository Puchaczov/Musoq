using System;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public class RawTraverseVisitor<TExpressionVisitor> : IExpressionVisitor
    where TExpressionVisitor : class, IExpressionVisitor
{
    protected readonly TExpressionVisitor Visitor;

    protected RawTraverseVisitor(TExpressionVisitor visitor)
    {
        Visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
    }

    public virtual void Visit(SelectNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(GroupSelectNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(StringNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(IntegerNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(HexIntegerNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(BinaryIntegerNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(OctalIntegerNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(BooleanNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(WordNode node)
    {
        node.Accept(Visitor);
    }

    public void Visit(NullNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(ContainsNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessMethodNode node)
    {
        node.Arguments.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessRawIdentifierNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(IsNullNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessRefreshAggregationScoreNode node)
    {
        node.Arguments.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessColumnNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(AllColumnsNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(IdentifierNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessObjectArrayNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessObjectKeyNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(PropertyValueNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(DotNode node)
    {
        node.Root.Accept(this);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessCallChainNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(WhereNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(GroupByNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);

        node.Having?.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(HavingNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(SkipNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(TakeNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
        node.SourceTable.Accept(this);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
        node.SourceTable.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(SchemaFromNode node)
    {
        node.Parameters.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(JoinSourcesTableFromNode node)
    {
        node.Expression.Accept(this);
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(Visitor);
    }

    public virtual void Visit(ApplySourcesTableFromNode node)
    {
        node.First.Accept(this);
        node.Second.Accept(this);

        node.Accept(Visitor);
    }

    public virtual void Visit(InMemoryTableFromNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(JoinFromNode node)
    {
        node.Source.Accept(this);
        node.With.Accept(this);
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ApplyFromNode node)
    {
        node.Source.Accept(this);
        node.With.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ExpressionFromNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(InterpretFromNode node)
    {
        node.InterpretCall.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(SchemaMethodFromNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(AccessMethodFromNode node)
    {
        node.AccessMethod.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(PropertyFromNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(AliasedFromNode node)
    {
        node.Args.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(CreateTransformationTableNode node)
    {
        foreach (var item in node.Fields)
            item.Accept(this);

        node.Accept(Visitor);
    }

    public virtual void Visit(RenameTableNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(TranslatedSetTreeNode node)
    {
        foreach (var item in node.Nodes)
            item.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(IntoNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(QueryScope node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(ShouldBePresentInTheTable node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(TranslatedSetOperatorNode node)
    {
        foreach (var item in node.CreateTableNodes)
            item.Accept(Visitor);

        node.FQuery.Accept(this);
        node.SQuery.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(QueryNode node)
    {
        node.From.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);
        node.Take?.Accept(this);
        node.Skip?.Accept(this);
        node.GroupBy?.Accept(this);
        node.OrderBy?.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(OrNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ShortCircuitingNodeLeft node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ShortCircuitingNodeRight node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(HyphenNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(AndNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(EqualityNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(GreaterOrEqualNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(LessOrEqualNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(GreaterNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(LessNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(DiffNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(NotNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(LikeNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(RLikeNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(InNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(BetweenNode node)
    {
        node.Expression.Accept(this);
        node.Min.Accept(this);
        node.Max.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(FieldNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(FieldOrderedNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ArgsListNode node)
    {
        foreach (var item in node.Args)
            item.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(DecimalNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(Node node)
    {
        throw new NotSupportedException();
    }

    public virtual void Visit(DescNode node)
    {
        node.From.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(StarNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(FSlashNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ModuloNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(AddNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(BitwiseAndNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(BitwiseOrNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(BitwiseXorNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(LeftShiftNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(RightShiftNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ArrayIndexNode node)
    {
        node.Array.Accept(this);
        node.Index.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(InternalQueryNode node)
    {
    }

    public virtual void Visit(RootNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(SingleSetNode node)
    {
        node.Query.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(UnionNode node)
    {
        TraverseSetOperator(node);
    }

    public virtual void Visit(UnionAllNode node)
    {
        TraverseSetOperator(node);
    }

    public virtual void Visit(ExceptNode node)
    {
        TraverseSetOperator(node);
    }

    public virtual void Visit(RefreshNode node)
    {
        foreach (var item in node.Nodes)
            item.Accept(this);

        node.Accept(Visitor);
    }

    public virtual void Visit(IntersectNode node)
    {
        TraverseSetOperator(node);
    }

    public virtual void Visit(PutTrueNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(MultiStatementNode node)
    {
        foreach (var cNode in node.Nodes)
            cNode.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(CteExpressionNode node)
    {
        node.OuterExpression.Accept(this);
        foreach (var exp in node.InnerExpression) exp.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(CteInnerExpressionNode node)
    {
        node.Value.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(JoinNode node)
    {
        node.Join.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ApplyNode node)
    {
        node.Apply.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(OrderByNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);

        node.Accept(Visitor);
    }

    public virtual void Visit(CreateTableNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(CoupleNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(StatementsArrayNode node)
    {
        foreach (var statement in node.Statements)
            statement.Accept(this);

        node.Accept(Visitor);
    }

    public virtual void Visit(StatementNode node)
    {
        node.Node.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(CaseNode node)
    {
        node.Else.Accept(this);

        for (var i = node.WhenThenPairs.Length - 1; i >= 0; --i)
        {
            node.WhenThenPairs[i].When.Accept(this);
            node.WhenThenPairs[i].Then.Accept(this);
        }

        node.Accept(Visitor);
    }

    public virtual void Visit(WhenNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ThenNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ElseNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(FieldLinkNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(InterpretCallNode node)
    {
        node.DataSource.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ParseCallNode node)
    {
        node.DataSource.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(InterpretAtCallNode node)
    {
        node.DataSource.Accept(this);
        node.Offset.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(TryInterpretCallNode node)
    {
        node.DataSource.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(TryParseCallNode node)
    {
        node.DataSource.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(PartialInterpretCallNode node)
    {
        node.DataSource.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(BinarySchemaNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(TextSchemaNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(FieldDefinitionNode node)
    {
        node.AtOffset?.Accept(this);
        node.WhenCondition?.Accept(this);
        node.Constraint?.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(ComputedFieldNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(TextFieldDefinitionNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(FieldConstraintNode node)
    {
        node.Expression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(PrimitiveTypeNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(ByteArrayTypeNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(StringTypeNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(SchemaReferenceTypeNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(ArrayTypeNode node)
    {
        node.ElementType.Accept(this);
        node.SizeExpression.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(BitsTypeNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(AlignmentNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(RepeatUntilTypeNode node)
    {
        node.Accept(Visitor);
    }

    public virtual void Visit(InlineSchemaTypeNode node)
    {
        foreach (var field in node.Fields)
            field.Accept(this);
        node.Accept(Visitor);
    }

    public virtual void Visit(FromNode node)
    {
        node.Accept(Visitor);
    }

    private void TraverseSetOperator(SetOperatorNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(Visitor);
    }
}
