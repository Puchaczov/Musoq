using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteFieldWithGroupMethodCall : CloneQueryVisitor
    {
        public RewriteFieldWithGroupMethodCall(int fieldOrder, FieldNode[] fields)
        {
            _fieldOrder = fieldOrder;
            _fields = fields;
        }

        public FieldNode Expression { get; private set; }

        private int _fieldOrder;
        private readonly FieldNode[] _fields;

        public override void Visit(FieldNode node)
        {
            base.Visit(node);
            Expression = Nodes.Pop() as FieldNode;
        }

        public override void Visit(AccessColumnNode node)
        {
            Nodes.Push(new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), string.Empty, node.ReturnType, TextSpan.Empty));
        }

        public override void Visit(DotNode node)
        {
            if (!(node.Root is DotNode) && node.Root is AccessColumnNode column)
            {
                var name = $"{NamingHelper.ToColumnName(column.Alias, column.Name)}.{node.Expression.ToString()}";
                Nodes.Push(new AccessColumnNode(name, string.Empty, node.ReturnType, TextSpan.Empty));
                return;
            }

            base.Visit(node);
        }

        public override void Visit(AccessMethodNode node)
        {
            if (node.IsAggregateMethod)
            {
                Nodes.Pop();

                var wordNode = node.Arguments.Args[0] as WordNode;
                Nodes.Push(new AccessColumnNode(wordNode.Value, string.Empty, node.ReturnType, TextSpan.Empty));
            }
            else if (_fields.Select(f => f.Expression.ToString()).Contains(node.ToString()))
            {
                Nodes.Push(new AccessColumnNode(node.ToString(), string.Empty, node.ReturnType, TextSpan.Empty));
            }
            else
            {
                base.Visit(node);
            }
        }

        public override void Visit(AccessCallChainNode node)
        {
            Nodes.Push(new AccessColumnNode(node.ToString(), string.Empty, node.ReturnType, TextSpan.Empty));
        }
    }
}
