using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Evaluator.RuntimeScripts;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Evaluator.Visitors
{
    public class RuntimeGeneratorVisitor : IExpressionVisitor
    {
        private readonly MainMethod _mainMethod = new MainMethod();
        private readonly Stack<string> _code = new Stack<string>();

        public void Visit(Node node)
        {
        }

        public void Visit(DescNode node)
        {
        }

        public void Visit(StarNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} * {b}");
        }

        public void Visit(FSlashNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} / {b}");
        }

        public void Visit(ModuloNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} % {b}");
        }

        public void Visit(AddNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} + {b}");
        }

        public void Visit(HyphenNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} - {b}");
        }

        public void Visit(AndNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} && {b}");
        }

        public void Visit(OrNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} || {b}");
        }

        public void Visit(ShortCircuitingNodeLeft node)
        {
            var b = _code.Pop();
            var a = _code.Pop();

            switch (node.UsedFor)
            {
                case TokenType.And:
                    _code.Push($"{a} && {b}");
                    break;
                case TokenType.Or:
                    _code.Push($"{a} || {b}");
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void Visit(ShortCircuitingNodeRight node)
        {
            var b = _code.Pop();
            var a = _code.Pop();

            switch (node.UsedFor)
            {
                case TokenType.And:
                    _code.Push($"{a} && {b}");
                    break;
                case TokenType.Or:
                    _code.Push($"{a} || {b}");
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public void Visit(EqualityNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} == {b}");
        }

        public void Visit(GreaterOrEqualNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} >= {b}");
        }

        public void Visit(LessOrEqualNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} <= {b}");
        }

        public void Visit(GreaterNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} > {b}");
        }

        public void Visit(LessNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} < {b}");
        }

        public void Visit(DiffNode node)
        {
            var b = _code.Pop();
            var a = _code.Pop();
            _code.Push($"{a} <> {b}");
        }

        public void Visit(NotNode node)
        {
            var a = _code.Pop();
            _code.Push($"!{a}");
        }

        public void Visit(LikeNode node)
        {
            var right = _code.Pop();
            var left = _code.Pop();
            _code.Push($"lib.Like({left}, {right})");
        }

        public void Visit(FieldNode node)
        {
        }

        public void Visit(StringNode node)
        {
            _code.Push($"\"{node.Value}\"");
        }

        public void Visit(DecimalNode node)
        {
            _code.Push($"{node.Value}m");
        }

        public void Visit(IntegerNode node)
        {
            _code.Push($"{node.Value}");
        }

        public void Visit(WordNode node)
        {
            _code.Push($"{node.Value}");
        }

        public void Visit(ContainsNode node)
        {
            var right = _code.Pop();
            var left = _code.Pop();
            _code.Push($"lib.Contains({left}, {right})"); //TO DO
        }

        public void Visit(AccessMethodNode node)
        {
            _code.Push($"lib.{node}");
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

        public void Visit(AccessPropertyNode node)
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
            _mainMethod.RowSourceMethod = node.Method;
            _mainMethod.RowSourceArguments = node.Parameters.Aggregate((a, b) => a + "," + b);
        }

        public void Visit(SchemaFromNode node)
        {
            _mainMethod.RowSourceMethod = node.Method;
            _mainMethod.RowSourceArguments = node.Parameters.Aggregate((a, b) => a + "," + b);
        }

        public void Visit(NestedQueryFromNode node)
        {
            _mainMethod.RowSourceMethod = node.Method;
            _mainMethod.RowSourceArguments = node.Parameters.Aggregate((a, b) => a + "," + b);
        }

        public void Visit(CteFromNode node)
        {
            _mainMethod.RowSourceMethod = node.Method;
            _mainMethod.RowSourceArguments = node.Parameters.Aggregate((a, b) => a + "," + b);
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
        }

        public string Script => _mainMethod.TransformText();
    }
}
