using System.Linq;
using System.Text;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Service.Visitors
{
    public class QueryCacheStringifier : IExpressionVisitor
    {
        private readonly StringBuilder _script = new StringBuilder();

        public string CacheKey => _script.ToString();

        public void AddText(string value)
        {
            _script.Append(value);
        }

        public void Visit(Node node)
        {
        }
        public void Visit(DescNode node)
        {
            _script.Append("desc");
        }

        public void Visit(StarNode node)
        {
            _script.Append("*");
        }

        public void Visit(FSlashNode node)
        {
            _script.Append("/");
        }

        public void Visit(ModuloNode node)
        {
            _script.Append("%");
        }

        public void Visit(AddNode node)
        {
            _script.Append("+");
        }

        public void Visit(HyphenNode node)
        {
            _script.Append("-");
        }

        public void Visit(AndNode node)
        {
            _script.Append("and");
        }

        public void Visit(OrNode node)
        {
            _script.Append("or");
        }

        public void Visit(ShortCircuitingNodeLeft node)
        {
        }

        public void Visit(ShortCircuitingNodeRight node)
        {
        }

        public void Visit(EqualityNode node)
        {
            _script.Append("=");
        }

        public void Visit(GreaterOrEqualNode node)
        {
            _script.Append(">=");
        }

        public void Visit(LessOrEqualNode node)
        {
            _script.Append("<=");
        }

        public void Visit(GreaterNode node)
        {
            _script.Append(">");
        }

        public void Visit(LessNode node)
        {
            _script.Append("<");
        }

        public void Visit(DiffNode node)
        {
            _script.Append("<>");
        }

        public void Visit(NotNode node)
        {
            _script.Append("not");
        }

        public void Visit(LikeNode node)
        {
            _script.Append("like");
        }

        public void Visit(FieldNode node)
        {
        }

        public void Visit(StringNode node)
        {
            _script.Append($"'{node.Value}'");
        }

        public void Visit(DecimalNode node)
        {
            _script.Append(node.Value);
        }

        public void Visit(IntegerNode node)
        {
            _script.Append(node.Value);
        }

        public void Visit(WordNode node)
        {
            _script.Append(node.Value);
        }

        public void Visit(ContainsNode node)
        {
            _script.Append("contains");
        }

        public void Visit(AccessMethodNode node)
        {
            _script.Append(node.Name);
        }

        public void Visit(GroupByAccessMethodNode node)
        {
            _script.Append("group by");
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
            _script.Append("refresh");
        }

        public void Visit(AccessColumnNode node)
        {
            _script.Append(node.Name);
        }

        public void Visit(AllColumnsNode node)
        {
            _script.Append("*");
        }

        public void Visit(AccessObjectArrayNode node)
        {
            _script.Append($"{node.ObjectName}[{node.Token.Index}]");
        }

        public void Visit(AccessObjectKeyNode node)
        {
            _script.Append($"{node.ObjectName}[{node.Token.Key}]");
        }

        public void Visit(PropertyValueNode node)
        {
            _script.Append($".{node.Name}");
        }

        public void Visit(AccessPropertyNode node)
        {
            _script.Append($".{node.Name}");
        }

        public void Visit(AccessCallChainNode node)
        {
            _script.Append(node.ColumnName);
        }

        public void Visit(ArgsListNode node)
        {
        }

        public void Visit(SelectNode node)
        {
            _script.Append("select");
        }

        public void Visit(WhereNode node)
        {
            _script.Append("where");
        }

        public void Visit(GroupByNode node)
        {
            _script.Append("group by");
        }

        public void Visit(HavingNode node)
        {
            _script.Append("having");
        }

        public void Visit(SkipNode node)
        {
            _script.Append($"skip{node.Value}");
        }

        public void Visit(TakeNode node)
        {
            _script.Append($"take{node.Value}");
        }

        public void Visit(ExistingTableFromNode node)
        {
            var parameters = node.Parameters.Length == 0 ? "()" : node.Parameters.Aggregate((a, b) => a + "," + b);
            _script.Append($"{node.Schema}.{node.Method}({parameters})");
        }

        public void Visit(SchemaFromNode node)
        {
            var parameters = node.Parameters.Length == 0 ? "()" : node.Parameters.Aggregate((a, b) => a + "," + b);
            _script.Append($"{node.Schema}.{node.Method}({parameters})");
        }

        public void Visit(NestedQueryFromNode node)
        {
        }

        public void Visit(CteFromNode node)
        {
            _script.Append(node.VariableName);
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
            var keys = node.Keys.Length == 0 ? string.Empty : node.Keys.Aggregate((a, b) => a + ", " + b);
            _script.Append($"union({keys})");
        }

        public void Visit(UnionAllNode node)
        {
            var keys = node.Keys.Length == 0 ? string.Empty : node.Keys.Aggregate((a, b) => a + ", " + b);
            _script.Append($"unionall({keys})");
        }

        public void Visit(ExceptNode node)
        {
            var keys = node.Keys.Length == 0 ? string.Empty : node.Keys.Aggregate((a, b) => a + ", " + b);
            _script.Append($"except({keys})");
        }

        public void Visit(RefreshNode node)
        {
        }

        public void Visit(IntersectNode node)
        {
            var keys = node.Keys.Length == 0 ? string.Empty : node.Keys.Aggregate((a, b) => a + ", " + b);
            _script.Append($"intersect({keys})");
        }

        public void Visit(PutTrueNode node)
        {
        }

        public void Visit(MultiStatementNode node)
        {
        }

        public void Visit(CteExpressionNode node)
        {
            _script.Append("with");
        }

        public void Visit(CteInnerExpressionNode node)
        {
            _script.Append($"{node.Name}as");
        }
    }
}
