using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{

    public void Visit(StringNode node) { }
    public void Visit(IntegerNode node) { }
    public void Visit(HexIntegerNode node) { }
    public void Visit(BinaryIntegerNode node) { }
    public void Visit(OctalIntegerNode node) { }
    public void Visit(BooleanNode node) { }
    public void Visit(WordNode node) { }
    public void Visit(NullNode node) { }
    public void Visit(ParameterBlockNode node) { }
    public void Visit(ParameterDeclarationNode node) { }
    public void Visit(ParameterReferenceNode node) { }
    public void Visit(ScriptVariableDeclarationNode node) { }
    public void Visit(ScriptVariableReferenceNode node) { }
    public void Visit(ContainsNode node) { }
    public void Visit(CollectionInNode node) { }

    public void Visit(AccessMethodNode node) { }
    public void Visit(AccessRawIdentifierNode node) { }
    public void Visit(IsNullNode node) { }
    public void Visit(RowPresenceNode node) { }
    public void Visit(AccessColumnNode node) { }
    public void Visit(AllColumnsNode node) { }
    public void Visit(IdentifierNode node) { }
    public void Visit(AccessObjectArrayNode node) { }
    public void Visit(AccessObjectKeyNode node) { }
    public void Visit(PropertyValueNode node) { }
    public void Visit(DotNode node) { }
    public void Visit(AccessCallChainNode node) { }

    public void Visit(OrNode node) { }
    public void Visit(ShortCircuitingNodeLeft node) { }
    public void Visit(ShortCircuitingNodeRight node) { }
    public void Visit(HyphenNode node) { }
    public void Visit(AndNode node) { }
    public void Visit(EqualityNode node) { }
    public void Visit(IsDistinctFromNode node) { }
    public void Visit(GreaterOrEqualNode node) { }
    public void Visit(LessOrEqualNode node) { }
    public void Visit(GreaterNode node) { }
    public void Visit(LessNode node) { }
    public void Visit(DiffNode node) { }
    public void Visit(NotNode node) { }
    public void Visit(LikeNode node) { }
    public void Visit(RLikeNode node) { }
    public void Visit(InNode node) { }

    public void Visit(InQueryNode node) { }

    public void Visit(ExistsQueryNode node) { }

    public void Visit(ScalarSubqueryNode node) { }

    public void Visit(BetweenNode node) { }

    public void Visit(FieldNode node) { }
    public void Visit(FieldOrderedNode node) { }
    public void Visit(ArgsListNode node) { }
    public void Visit(DecimalNode node) { }

    public void Visit(StarNode node) { }
    public void Visit(FSlashNode node) { }
    public void Visit(ModuloNode node) { }
    public void Visit(AddNode node) { }
    public void Visit(BitwiseAndNode node) { }
    public void Visit(BitwiseOrNode node) { }
    public void Visit(BitwiseXorNode node) { }
    public void Visit(LeftShiftNode node) { }
    public void Visit(RightShiftNode node) { }
    public void Visit(CoalesceNode node) { }
    public void Visit(ArrayIndexNode node) { }

    public void Visit(PutTrueNode node) { }

    public void Visit(JoinNode node) { }
    public void Visit(ApplyNode node) { }
    public void Visit(CreateTransformationTableNode node) { }
    public void Visit(RenameTableNode node) { }
    public void Visit(TranslatedSetTreeNode node) { }
    public void Visit(IntoNode node) { }
    public void Visit(QueryScope node) { }
    public void Visit(ShouldBePresentInTheTable node) { }
    public void Visit(TranslatedSetOperatorNode node) { }

    public void Visit(CreateTableNode node) { }
    public void Visit(EnumDeclarationNode node) { }
    public void Visit(EnumMemberNode node) { }
    public void Visit(CoupleNode node) { }
    public void Visit(StatementsArrayNode node) { }
    public void Visit(StatementNode node) { }
    public void Visit(CaseNode node) { }
    public void Visit(WhenNode node) { }
    public void Visit(ThenNode node) { }
    public void Visit(ElseNode node) { }
    public void Visit(InterpretCallNode node) { }
    public void Visit(ParseCallNode node) { }
    public void Visit(InterpretAtCallNode node) { }
    public void Visit(TryInterpretCallNode node) { }
    public void Visit(TryParseCallNode node) { }
    public void Visit(PartialInterpretCallNode node) { }
    public void Visit(PartialParseCallNode node) { }
    public void Visit(BinarySchemaNode node) { }
    public void Visit(TextSchemaNode node) { }
    public void Visit(FieldDefinitionNode node) { }
    public void Visit(TextFieldDefinitionNode node) { }
    public void Visit(ComputedFieldNode node) { }
    public void Visit(FieldConstraintNode node) { }
    public void Visit(FieldValueValidationNode node) { }
    public void Visit(PrimitiveTypeNode node) { }
    public void Visit(ByteArrayTypeNode node) { }
    public void Visit(BinarySwitchTypeNode node) { }
    public void Visit(StringTypeNode node) { }
    public void Visit(SchemaReferenceTypeNode node) { }
    public void Visit(ArrayTypeNode node) { }
    public void Visit(BitsTypeNode node) { }
    public void Visit(AlignmentNode node) { }
    public void Visit(RepeatUntilTypeNode node) { }
    public void Visit(SubstreamTypeNode node) { }
    public void Visit(InlineSchemaTypeNode node) { }
    public void Visit(WindowFunctionNode node) { }
    public void Visit(WindowSpecificationNode node) { }
    public void Visit(WindowFrameNode node) { }
    public void Visit(WindowFrameBoundNode node) { }
    public void Visit(WindowDefinitionNode node) { }
}
