using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Parser;

/// <summary>
///     Base class that provides empty (no-op) implementations for all IExpressionVisitor methods.
///     Derived classes can selectively override only the Visit methods they need to handle.
/// </summary>
public abstract class NoOpExpressionVisitor : IExpressionVisitor
{
    public virtual void Visit(Node node)
    {
    }

    public virtual void Visit(DescNode node)
    {
    }

    public virtual void Visit(StarNode node)
    {
    }

    public virtual void Visit(FSlashNode node)
    {
    }

    public virtual void Visit(ModuloNode node)
    {
    }

    public virtual void Visit(AddNode node)
    {
    }

    public virtual void Visit(HyphenNode node)
    {
    }

    public virtual void Visit(AndNode node)
    {
    }

    public virtual void Visit(OrNode node)
    {
    }

    public virtual void Visit(ShortCircuitingNodeLeft node)
    {
    }

    public virtual void Visit(ShortCircuitingNodeRight node)
    {
    }

    public virtual void Visit(EqualityNode node)
    {
    }

    public virtual void Visit(GreaterOrEqualNode node)
    {
    }

    public virtual void Visit(LessOrEqualNode node)
    {
    }

    public virtual void Visit(GreaterNode node)
    {
    }

    public virtual void Visit(LessNode node)
    {
    }

    public virtual void Visit(DiffNode node)
    {
    }

    public virtual void Visit(NotNode node)
    {
    }

    public virtual void Visit(LikeNode node)
    {
    }

    public virtual void Visit(RLikeNode node)
    {
    }

    public virtual void Visit(InNode node)
    {
    }

    public virtual void Visit(FieldNode node)
    {
    }

    public virtual void Visit(FieldOrderedNode node)
    {
    }

    public virtual void Visit(StringNode node)
    {
    }

    public virtual void Visit(DecimalNode node)
    {
    }

    public virtual void Visit(IntegerNode node)
    {
    }

    public virtual void Visit(HexIntegerNode node)
    {
    }

    public virtual void Visit(BinaryIntegerNode node)
    {
    }

    public virtual void Visit(OctalIntegerNode node)
    {
    }

    public virtual void Visit(BooleanNode node)
    {
    }

    public virtual void Visit(WordNode node)
    {
    }

    public virtual void Visit(NullNode node)
    {
    }

    public virtual void Visit(ContainsNode node)
    {
    }

    public virtual void Visit(AccessMethodNode node)
    {
    }

    public virtual void Visit(AccessRawIdentifierNode node)
    {
    }

    public virtual void Visit(IsNullNode node)
    {
    }

    public virtual void Visit(AccessRefreshAggregationScoreNode node)
    {
    }

    public virtual void Visit(AccessColumnNode node)
    {
    }

    public virtual void Visit(AllColumnsNode node)
    {
    }

    public virtual void Visit(IdentifierNode node)
    {
    }

    public virtual void Visit(AccessObjectArrayNode node)
    {
    }

    public virtual void Visit(AccessObjectKeyNode node)
    {
    }

    public virtual void Visit(PropertyValueNode node)
    {
    }

    public virtual void Visit(DotNode node)
    {
    }

    public virtual void Visit(AccessCallChainNode node)
    {
    }

    public virtual void Visit(ArgsListNode node)
    {
    }

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

    public virtual void Visit(SingleSetNode node)
    {
    }

    public virtual void Visit(UnionNode node)
    {
    }

    public virtual void Visit(UnionAllNode node)
    {
    }

    public virtual void Visit(ExceptNode node)
    {
    }

    public virtual void Visit(RefreshNode node)
    {
    }

    public virtual void Visit(IntersectNode node)
    {
    }

    public virtual void Visit(PutTrueNode node)
    {
    }

    public virtual void Visit(MultiStatementNode node)
    {
    }

    public virtual void Visit(StatementsArrayNode node)
    {
    }

    public virtual void Visit(StatementNode node)
    {
    }

    public virtual void Visit(CteExpressionNode node)
    {
    }

    public virtual void Visit(CteInnerExpressionNode node)
    {
    }

    public virtual void Visit(JoinNode node)
    {
    }

    public virtual void Visit(ApplyNode node)
    {
    }

    public virtual void Visit(OrderByNode node)
    {
    }

    public virtual void Visit(CreateTableNode node)
    {
    }

    public virtual void Visit(CoupleNode node)
    {
    }

    public virtual void Visit(CaseNode node)
    {
    }

    public virtual void Visit(WhenNode node)
    {
    }

    public virtual void Visit(ThenNode node)
    {
    }

    public virtual void Visit(ElseNode node)
    {
    }

    public virtual void Visit(FieldLinkNode node)
    {
    }


    public virtual void Visit(BitwiseAndNode node)
    {
    }

    public virtual void Visit(BitwiseOrNode node)
    {
    }

    public virtual void Visit(BitwiseXorNode node)
    {
    }

    public virtual void Visit(LeftShiftNode node)
    {
    }

    public virtual void Visit(RightShiftNode node)
    {
    }


    public virtual void Visit(InterpretCallNode node)
    {
    }

    public virtual void Visit(ParseCallNode node)
    {
    }

    public virtual void Visit(InterpretAtCallNode node)
    {
    }

    public virtual void Visit(TryInterpretCallNode node)
    {
    }

    public virtual void Visit(TryParseCallNode node)
    {
    }

    public virtual void Visit(PartialInterpretCallNode node)
    {
    }


    public virtual void Visit(BinarySchemaNode node)
    {
    }

    public virtual void Visit(TextSchemaNode node)
    {
    }

    public virtual void Visit(FieldDefinitionNode node)
    {
    }

    public virtual void Visit(TextFieldDefinitionNode node)
    {
    }

    public virtual void Visit(ComputedFieldNode node)
    {
    }

    public virtual void Visit(FieldConstraintNode node)
    {
    }

    public virtual void Visit(PrimitiveTypeNode node)
    {
    }

    public virtual void Visit(ByteArrayTypeNode node)
    {
    }

    public virtual void Visit(StringTypeNode node)
    {
    }

    public virtual void Visit(SchemaReferenceTypeNode node)
    {
    }

    public virtual void Visit(ArrayTypeNode node)
    {
    }

    public virtual void Visit(BitsTypeNode node)
    {
    }

    public virtual void Visit(AlignmentNode node)
    {
    }

    public virtual void Visit(RepeatUntilTypeNode node)
    {
    }

    public virtual void Visit(InlineSchemaTypeNode node)
    {
    }
}
