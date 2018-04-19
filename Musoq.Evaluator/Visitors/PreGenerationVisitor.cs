using System.Collections.Generic;
using Musoq.Evaluator.Tables;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class PreGenerationVisitor : IExpressionVisitor
    {
        private readonly Stack<string> _names = new Stack<string>();
        public Dictionary<string, TableMetadata> TableMetadata { get; } = new Dictionary<string, TableMetadata>();
        public List<AccessMethodNode> AggregationMethods { get; } = new List<AccessMethodNode>();

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

        public void Visit(FieldNode node)
        {
        }

        public void Visit(SelectNode node)
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

        public void Visit(WordNode node)
        {
        }

        public void Visit(ContainsNode node)
        {
        }

        public void Visit(AccessMethodNode node)
        {
            var isAggregationMethod = node.IsAggregateMethod;

            if (isAggregationMethod) AggregationMethods.Add(node);
        }

        public void Visit(GroupByAccessMethodNode node)
        {
            var isAggregationMethod = node.IsAggregateMethod;

            if (isAggregationMethod) AggregationMethods.Add(node);
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
        }

        public void Visit(AccessColumnNode node)
        {
        }

        public void Visit(AllColumnsNode node)
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

        public void Visit(WhereNode node)
        {
        }

        public void Visit(TakeNode node)
        {
        }

        public void Visit(ExistingTableFromNode node)
        {
            _names.Push(node.Schema);
        }

        public void Visit(SchemaFromNode node)
        {
            _names.Push(node.Alias);
        }

        public void Visit(NestedQueryFromNode node)
        {
            _names.Push(node.Schema);
        }

        public void Visit(CteFromNode node)
        {
            _names.Push(node.VariableName);
        }

        public void Visit(JoinFromNode node)
        {
        }

        public void Visit(CreateTableNode node)
        {
            TableMetadata table;

            if (!TableMetadata.ContainsKey(node.Name))
            {
                table = new TableMetadata(null);
                TableMetadata.Add(node.Name, table);
                foreach (var field in node.Fields)
                    table.Columns.Add(new Column(field.FieldName, field.ReturnType, field.FieldOrder));
            }

            table = TableMetadata[node.Name];

            foreach (var key in node.Keys)
                table.Indexes.Add(key);
        }

        public void Visit(RenameTableNode node)
        {
            TableMetadata.Add(node.TableDestinationName, TableMetadata[node.TableSourceName]);
        }

        public void Visit(TranslatedSetTreeNode node)
        {
        }

        public void Visit(IntoNode node)
        {
        }

        public void Visit(IntoGroupNode node)
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
            Visit((SetOperatorNode) node);
        }

        public void Visit(UnionAllNode node)
        {
            Visit((SetOperatorNode) node);
        }

        public void Visit(ExceptNode node)
        {
            Visit((SetOperatorNode) node);
        }

        public void Visit(RefreshNode node)
        {
        }

        public void Visit(IntersectNode node)
        {
            Visit((SetOperatorNode) node);
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

        public void Visit(JoinsNode node)
        {
        }

        public void Visit(JoinNode node)
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

        public void Visit(SetOperatorNode node)
        {
        }
    }
}