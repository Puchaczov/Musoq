using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors;

public class GetSelectFieldsVisitor : IQueryPartAwareExpressionVisitor
{
    private readonly List<ISchemaColumn> _collectedFieldNames = [];
    private QueryPart _queryPart;
    private ISchemaColumn[] _cachedFieldNames;
    private bool _fieldNamesCacheValid;

    public ISchemaColumn[] CollectedFieldNames
    {
        get
        {
            if (_fieldNamesCacheValid)
                return _cachedFieldNames;
                
            _cachedFieldNames = _collectedFieldNames.ToArray();
            _fieldNamesCacheValid = true;
            return _cachedFieldNames;
        }
    }
    
    /// <summary>
    ///     Provides direct access to the list for efficient enumeration when modification is not needed.
    /// </summary>
    public IReadOnlyList<ISchemaColumn> CollectedFieldNamesList => _collectedFieldNames;

    public void SetQueryPart(QueryPart part)
    {
        _queryPart = part;
    }

    public void QueryBegins()
    {
    }

    public void QueryEnds()
    {
    }

    public void Visit(Node node)
    {
    }

    public void Visit(DescNode node)
    {
    }

    public void Visit(StarNode node)
    {
    }

    public void Visit(FSlashNode node)
    {
    }

    public void Visit(ModuloNode node)
    {
    }

    public void Visit(AddNode node)
    {
    }

    public void Visit(HyphenNode node)
    {
    }

    public void Visit(BitwiseAndNode node)
    {
    }

    public void Visit(BitwiseOrNode node)
    {
    }

    public void Visit(BitwiseXorNode node)
    {
    }

    public void Visit(LeftShiftNode node)
    {
    }

    public void Visit(RightShiftNode node)
    {
    }

    public void Visit(ArrayIndexNode node)
    {
    }

    public void Visit(AndNode node)
    {
    }

    public void Visit(OrNode node)
    {
    }

    public void Visit(ShortCircuitingNodeLeft node)
    {
    }

    public void Visit(ShortCircuitingNodeRight node)
    {
    }

    public void Visit(EqualityNode node)
    {
    }

    public void Visit(GreaterOrEqualNode node)
    {
    }

    public void Visit(LessOrEqualNode node)
    {
    }

    public void Visit(GreaterNode node)
    {
    }

    public void Visit(LessNode node)
    {
    }

    public void Visit(DiffNode node)
    {
    }

    public void Visit(NotNode node)
    {
    }

    public void Visit(LikeNode node)
    {
    }

    public void Visit(RLikeNode node)
    {
    }

    public void Visit(InNode node)
    {
    }

    public void Visit(BetweenNode node)
    {
    }

    public void Visit(FieldNode node)
    {
        if (_queryPart == QueryPart.Select && _collectedFieldNames.All(field => field.ColumnName != node.FieldName))
            _collectedFieldNames.Add(new SchemaColumn(node.FieldName, _collectedFieldNames.Count, node.ReturnType));
    }

    public void Visit(FieldOrderedNode node)
    {
    }

    public void Visit(StringNode node)
    {
    }

    public void Visit(DecimalNode node)
    {
    }

    public void Visit(IntegerNode node)
    {
    }

    public void Visit(HexIntegerNode node)
    {
    }

    public void Visit(BinaryIntegerNode node)
    {
    }

    public void Visit(OctalIntegerNode node)
    {
    }

    public void Visit(BooleanNode node)
    {
    }

    public void Visit(WordNode node)
    {
    }

    public void Visit(NullNode node)
    {
    }

    public void Visit(ContainsNode node)
    {
    }

    public void Visit(AccessMethodNode node)
    {
    }

    public void Visit(AccessRawIdentifierNode node)
    {
    }

    public void Visit(IsNullNode node)
    {
    }

    public void Visit(AccessRefreshAggregationScoreNode node)
    {
    }

    public void Visit(AccessColumnNode node)
    {
    }

    public void Visit(AllColumnsNode node)
    {
    }

    public void Visit(IdentifierNode node)
    {
    }

    public void Visit(AccessObjectArrayNode node)
    {
    }

    public void Visit(AccessObjectKeyNode node)
    {
    }

    public void Visit(PropertyValueNode node)
    {
    }

    public void Visit(DotNode node)
    {
    }

    public void Visit(AccessCallChainNode node)
    {
    }

    public void Visit(ArgsListNode node)
    {
    }

    public void Visit(SelectNode node)
    {
    }

    public void Visit(GroupSelectNode node)
    {
    }

    public void Visit(WhereNode node)
    {
    }

    public void Visit(GroupByNode node)
    {
    }

    public void Visit(HavingNode node)
    {
    }

    public void Visit(SkipNode node)
    {
    }

    public void Visit(TakeNode node)
    {
    }

    public void Visit(JoinInMemoryWithSourceTableFromNode node)
    {
    }

    public void Visit(ApplyInMemoryWithSourceTableFromNode node)
    {
    }

    public void Visit(SchemaFromNode node)
    {
    }

    public void Visit(JoinSourcesTableFromNode node)
    {
    }

    public void Visit(ApplySourcesTableFromNode node)
    {
    }

    public void Visit(InMemoryTableFromNode node)
    {
    }

    public void Visit(JoinFromNode node)
    {
    }

    public void Visit(ApplyFromNode node)
    {
    }

    public void Visit(ExpressionFromNode node)
    {
    }

    public void Visit(InterpretFromNode node)
    {
    }

    public void Visit(AccessMethodFromNode node)
    {
    }

    public void Visit(SchemaMethodFromNode node)
    {
    }

    public void Visit(PropertyFromNode node)
    {
    }

    public void Visit(AliasedFromNode node)
    {
    }

    public void Visit(CreateTransformationTableNode node)
    {
    }

    public void Visit(RenameTableNode node)
    {
    }

    public void Visit(TranslatedSetTreeNode node)
    {
    }

    public void Visit(IntoNode node)
    {
    }

    public void Visit(QueryScope node)
    {
    }

    public void Visit(ShouldBePresentInTheTable node)
    {
    }

    public void Visit(TranslatedSetOperatorNode node)
    {
    }

    public void Visit(QueryNode node)
    {
    }

    public void Visit(InternalQueryNode node)
    {
    }

    public void Visit(RootNode node)
    {
    }

    public void Visit(SingleSetNode node)
    {
    }

    public void Visit(UnionNode node)
    {
    }

    public void Visit(UnionAllNode node)
    {
    }

    public void Visit(ExceptNode node)
    {
    }

    public void Visit(RefreshNode node)
    {
    }

    public void Visit(IntersectNode node)
    {
    }

    public void Visit(PutTrueNode node)
    {
    }

    public void Visit(MultiStatementNode node)
    {
    }

    public void Visit(CteExpressionNode node)
    {
    }

    public void Visit(CteInnerExpressionNode node)
    {
    }

    public void Visit(JoinNode node)
    {
    }

    public void Visit(ApplyNode node)
    {
    }

    public void Visit(OrderByNode node)
    {
    }

    public void Visit(CreateTableNode node)
    {
    }

    public void Visit(CoupleNode node)
    {
    }

    public void Visit(StatementsArrayNode node)
    {
    }

    public void Visit(StatementNode node)
    {
    }

    public void Visit(CaseNode node)
    {
    }

    public void Visit(WhenNode node)
    {
    }

    public void Visit(ThenNode node)
    {
    }

    public void Visit(ElseNode node)
    {
    }

    public void Visit(FieldLinkNode node)
    {
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

    public void Visit(TextFieldDefinitionNode node)
    {
    }

    public void Visit(ComputedFieldNode node)
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
