using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Parser.Nodes
{
    public class CteExpressionNode : Node
    {
        private AccessColumnNode _name;
        private Node _sets;
        private readonly Node _outerSets;

        public CteExpressionNode(AccessColumnNode name, Node sets, Node outerSets)
        {
            _name = name;
            _sets = sets;
            _outerSets = outerSets;
        }

        public override Type ReturnType => typeof(void);
        public override void Accept(IExpressionVisitor visitor)
        {
        }

        public override string Id { get; }
        public override string ToString()
        {
            return null;
        }
    }
}
