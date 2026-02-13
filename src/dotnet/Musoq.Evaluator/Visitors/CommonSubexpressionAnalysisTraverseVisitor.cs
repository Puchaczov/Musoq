using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Traverses the query tree to visit all expressions for CSE analysis.
///     This traverser visits expressions in all relevant clauses (SELECT, WHERE, HAVING, ORDER BY, etc.)
///     to count occurrences for potential caching.
/// </summary>
public class CommonSubexpressionAnalysisTraverseVisitor : IExpressionVisitor
{
    private readonly CommonSubexpressionAnalysisVisitor _visitor;

    public CommonSubexpressionAnalysisTraverseVisitor(CommonSubexpressionAnalysisVisitor visitor)
    {
        _visitor = visitor;
    }

    public void Visit(Node node)
    {
        node.Accept(_visitor);
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

    public void Visit(HyphenNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(BitwiseAndNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(BitwiseOrNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(BitwiseXorNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(LeftShiftNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(RightShiftNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ArrayIndexNode node)
    {
        node.Array.Accept(this);
        node.Index.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(AndNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
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

    public void Visit(BetweenNode node)
    {
        node.Expression.Accept(this);
        node.Min.Accept(this);
        node.Max.Accept(this);
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

    public void Visit(StringNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(DecimalNode node)
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

    public void Visit(ArgsListNode node)
    {
        foreach (var arg in node.Args) arg.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(SelectNode node)
    {
        foreach (var field in node.Fields) field.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GroupSelectNode node)
    {
        foreach (var field in node.Fields) field.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(WhereNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(GroupByNode node)
    {
        _visitor.InSeparateScopeContext = true;
        foreach (var field in node.Fields) field.Accept(this);
        _visitor.InSeparateScopeContext = false;
        node.Accept(_visitor);
    }

    public void Visit(HavingNode node)
    {
        _visitor.InSeparateScopeContext = true;
        node.Expression.Accept(this);
        _visitor.InSeparateScopeContext = false;
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
        node.Accept(_visitor);
    }

    public void Visit(AliasedFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
        node.First.Accept(this);
        node.Second.Accept(this);
        node.Expression.Accept(this);
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
        node.Source.Accept(this);
        node.With.Accept(this);
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ApplyFromNode node)
    {
        node.Source.Accept(this);
        node.With.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ExpressionFromNode node)
    {
        node.Expression.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(InterpretFromNode node)
    {
        node.InterpretCall.Accept(this);
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

    public void Visit(AccessMethodFromNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(CreateTransformationTableNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(RenameTableNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(TranslatedSetTreeNode node)
    {
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
        node.Accept(_visitor);
    }

    public void Visit(QueryNode node)
    {
        node.From?.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);
        node.GroupBy?.Accept(this);
        node.OrderBy?.Accept(this);
        node.Skip?.Accept(this);
        node.Take?.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(InternalQueryNode node)
    {
        node.From?.Accept(this);
        node.Where?.Accept(this);
        node.Select.Accept(this);
        node.GroupBy?.Accept(this);
        node.Skip?.Accept(this);
        node.Take?.Accept(this);
        node.Refresh?.Accept(this);
        node.Accept(_visitor);
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
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(UnionAllNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(ExceptNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(RefreshNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(IntersectNode node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(PutTrueNode node)
    {
        node.Accept(_visitor);
    }

    public void Visit(MultiStatementNode node)
    {
        foreach (var statement in node.Nodes) statement.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(StatementsArrayNode node)
    {
        foreach (var statement in node.Statements) statement.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(StatementNode node)
    {
        node.Node.Accept(this);
        node.Accept(_visitor);
    }

    public void Visit(CteExpressionNode node)
    {
        foreach (var innerCte in node.InnerExpression) innerCte.Accept(this);
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
        node.Accept(_visitor);
    }

    public void Visit(OrderByNode node)
    {
        _visitor.InSeparateScopeContext = true;
        foreach (var field in node.Fields)
            field.Accept(this);
        _visitor.InSeparateScopeContext = false;

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

    public void Visit(CaseNode node)
    {
        _visitor.InPassThroughUnsafeContext = true;
        foreach (var whenNode in node.WhenThenPairs)
        {
            whenNode.When.Accept(this);
            whenNode.Then.Accept(this);
        }

        node.Else?.Accept(this);
        _visitor.InPassThroughUnsafeContext = false;

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

    public void Visit(InterpretCallNode node)
    {
    }

    public void Visit(ParseCallNode node)
    {
    }

    public void Visit(InterpretAtCallNode node)
    {
    }

    public void Visit(TryInterpretCallNode node)
    {
    }

    public void Visit(TryParseCallNode node)
    {
    }

    public void Visit(PartialInterpretCallNode node)
    {
    }

    public void Visit(BinarySchemaNode node)
    {
    }

    public void Visit(TextSchemaNode node)
    {
    }

    public void Visit(FieldDefinitionNode node)
    {
    }

    public void Visit(ComputedFieldNode node)
    {
    }

    public void Visit(TextFieldDefinitionNode node)
    {
    }

    public void Visit(FieldConstraintNode node)
    {
    }

    public void Visit(PrimitiveTypeNode node)
    {
    }

    public void Visit(ByteArrayTypeNode node)
    {
    }

    public void Visit(StringTypeNode node)
    {
    }

    public void Visit(SchemaReferenceTypeNode node)
    {
    }

    public void Visit(ArrayTypeNode node)
    {
    }

    public void Visit(BitsTypeNode node)
    {
    }

    public void Visit(AlignmentNode node)
    {
    }

    public void Visit(RepeatUntilTypeNode node)
    {
    }

    public void Visit(InlineSchemaTypeNode node)
    {
    }
}
