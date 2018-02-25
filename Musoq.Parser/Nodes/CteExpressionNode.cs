using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Parser.Nodes
{
    public class CteExpressionNode : Node
    {
        private readonly string _name;
        private readonly Node _sets;
        private readonly Node _outerSets;

        public CteExpressionNode(string name, Node sets, Node outerSets)
        {
            _name = name;
            _sets = sets;
            _outerSets = outerSets;
        }

        public override Type ReturnType => typeof(void);
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public string Name => _name;

        public Node InnerExpression => _sets;

        public Node OuterExpression => _outerSets;

        public override string Id => $"{nameof(CteExpressionNode)}{_name}{_sets.Id}{_outerSets.Id}";

        public override string ToString()
        {
            return $"with {_name} as ({_sets.ToString()}) {_outerSets.ToString()}";
        }
    }
}
