using Musoq.Parser.Nodes;

namespace Musoq.Parser
{
    public interface IExpressionVisitor
    {
        void Visit(Node node);
        void Visit(StarNode node);
        void Visit(FSlashNode node);
        void Visit(ModuloNode node);
        void Visit(AddNode node);
        void Visit(HyphenNode node);
        void Visit(AndNode node);
        void Visit(OrNode node);
        void Visit(ShortCircuitingNodeLeft node);
        void Visit(ShortCircuitingNodeRight node);
        void Visit(EqualityNode node);
        void Visit(GreaterOrEqualNode node);
        void Visit(LessOrEqualNode node);
        void Visit(GreaterNode node);
        void Visit(LessNode node);
        void Visit(DiffNode node);
        void Visit(NotNode node);
        void Visit(LikeNode node);
        void Visit(FieldNode node);
        void Visit(StringNode node);
        void Visit(DecimalNode node);
        void Visit(IntegerNode node);
        void Visit(WordNode node);
        void Visit(ContainsNode node);
        void Visit(AccessMethodNode node);
        void Visit(GroupByAccessMethodNode node);
        void Visit(AccessRefreshAggreationScoreNode node);
        void Visit(AccessColumnNode node);
        void Visit(AllColumnsNode node);
        void Visit(AccessObjectArrayNode node);
        void Visit(AccessObjectKeyNode node);
        void Visit(PropertyValueNode node);
        void Visit(AccessPropertyNode node);
        void Visit(AccessCallChainNode node);
        void Visit(ArgsListNode node);
        void Visit(SelectNode node);
        void Visit(WhereNode node);
        void Visit(GroupByNode node);
        void Visit(HavingNode node);
        void Visit(SkipNode node);
        void Visit(TakeNode node);
        void Visit(ExistingTableFromNode node);
        void Visit(SchemaFromNode node);
        void Visit(NestedQueryFromNode node);
        void Visit(CteFromNode node);
        void Visit(CreateTableNode node);
        void Visit(RenameTableNode node);
        void Visit(TranslatedSetTreeNode node);
        void Visit(IntoNode node);
        void Visit(IntoGroupNode node);
        void Visit(ShouldBePresentInTheTable node);
        void Visit(TranslatedSetOperatorNode node);
        void Visit(QueryNode node);
        void Visit(InternalQueryNode node);
        void Visit(RootNode node);
        void Visit(SingleSetNode node);
        void Visit(UnionNode node);
        void Visit(UnionAllNode node);
        void Visit(ExceptNode node);
        void Visit(RefreshNode node);
        void Visit(IntersectNode node);
        void Visit(PutTrueNode node);
        void Visit(MultiStatementNode node);
        void Visit(CteExpressionNode node);
        void Visit(CteInnerExpressionNode node);
    }
}