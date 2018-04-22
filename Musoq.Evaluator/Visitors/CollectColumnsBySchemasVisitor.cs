using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors
{
    public class CollectColumnsBySchemasVisitor : ICollectColumnsBySchemasAwareVisitor
    {
        private readonly ISchemaProvider _schemaProvider;
        private string _currentCteName = string.Empty;
        private bool _isInsideCte = false;
        private bool _isInsideinnerCte = false;
        private List<string> _innerCteScoreTable = new List<string>();
        private bool _isJoiningPossible;
        private readonly List<string> _joinsTableName = new List<string>();
        private readonly List<ISchemaColumn> _realColumns = new List<ISchemaColumn>();
        private readonly List<string> _setOperatorTableName = new List<string>();
        private bool _isInsideSetOperator;
        private QueryPart _queryPart;

        public CollectColumnsBySchemasVisitor(ISchemaProvider schemaProvider)
        {
            _schemaProvider = schemaProvider;
        }

        public MetadataCollector CollectedColumns { get; } = new MetadataCollector();

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
            if (_queryPart == QueryPart.Select)
            {
                _realColumns.Add(new SchemaColumn(node.FieldName, _realColumns.Count, node.ReturnType));
            }
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
        }

        public void Visit(GroupByAccessMethodNode node)
        {
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

        public void Visit(SelectNode node)
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

        public void Visit(ExistingTableFromNode node)
        {
        }

        public void Visit(SchemaFromNode node)
        {
            var schema = _schemaProvider.GetSchema(node.Schema);
            var table = schema.GetTableByName(node.Method, node.Parameters);
            CollectedColumns.AddTable(node.Alias, table);

            if (_isInsideinnerCte)
            {
                _innerCteScoreTable.Add(node.Alias);
            }

            _joinsTableName.Add(node.Alias);
        }

        public void Visit(NestedQueryFromNode node)
        {
        }

        public void Visit(CteFromNode node)
        {
            _joinsTableName.Add(node.VariableName);

            if (_isInsideinnerCte)
            {
                _innerCteScoreTable.Add(node.VariableName);
            }
        }

        public void Visit(JoinFromNode node)
        {
        }

        public void Visit(CreateTableNode node)
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
            _currentCteName = node.Name;
        }

        public void Visit(JoinsNode node)
        {
        }

        public void Visit(JoinNode node)
        {
        }

        public void SetInsideCte()
        {
            _isInsideCte = true;
        }

        public void UnsetInsideCte()
        {
            _isInsideCte = false;
        }

        public void IsInsideInnerCte(string name)
        {
            _isInsideinnerCte = true;
        }

        public void IsOutsideInnerCte(string name)
        {
            _isInsideinnerCte = false;
        }

        public void SetJoiningTablesPossible()
        {
            _isJoiningPossible = true;
        }

        public void UnsetJoiningTablesPossible()
        {
            _isJoiningPossible = false;
        }

        public void SetQueryStarts()
        {
        }

        public void UnsetQueryStarts()
        {
            var name = _joinsTableName.Aggregate((a, b) => $"{a}_{b}");

            foreach(var column in _realColumns)
                CollectedColumns.AddUsedColumn(name, column);

            if (_isInsideSetOperator)
                _setOperatorTableName.Add(name);

            _joinsTableName.Clear();
        }

        public void SetInsideSetOperator()
        {
            _isInsideSetOperator = true;
        }

        public void UnsetInsideSetOperator()
        {
            var name = _setOperatorTableName.Aggregate((a, b) => $"{a}__{b}");
            var table = new DynamicTable(CollectedColumns.GetUsedColumns(_setOperatorTableName[0]));
            CollectedColumns.AddTable(name, table);

            foreach(var column in table.Columns)
                CollectedColumns.AddUsedColumn(name, column);

            _isInsideSetOperator = false;

            _setOperatorTableName.Clear();
        }

        public void SetQueryPart(QueryPart part)
        {
            _queryPart = part;
        }
    }

    public enum QueryPart
    {
        Select,
        From,
        Where,
        Join,
        GroupBy,
        Having
    }
}
