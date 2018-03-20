using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors
{
    public class RewriteFieldWithGroupMethodCall : RewriteTreeVisitor
    {
        public RewriteFieldWithGroupMethodCall(TransitionSchemaProvider schemaProvider, int fieldOrder, FieldNode[] fields) 
            : base(schemaProvider)
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

        public override void Visit(AccessMethodNode node)
        {
            if (node.IsAggregateMethod)
            {
                Nodes.Pop();

                var wordNode = node.Arguments.Args[0] as WordNode;
                Nodes.Push(new DetailedAccessColumnNode(wordNode.Value, _fieldOrder++, node.ReturnType));
            }
            else if (_fields.Select(f => f.Expression.ToString()).Contains(node.ToString()))
            {
                Nodes.Push(new DetailedAccessColumnNode(node.ToString(), _fieldOrder++, node.ReturnType));
            }
            else
            {
                base.Visit(node);
            }
        }

        public override void Visit(AccessCallChainNode node)
        {
            Nodes.Push(new DetailedAccessColumnNode(node.ToString(), _fieldOrder++, node.ReturnType));
        }
    }
}
