using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class TranslatedSetTreeNode : Node
    {
        public TranslatedSetTreeNode(List<TranslatedSetOperatorNode> nodes)
        {
            Nodes = nodes;
            var setId = nodes.Count == 0 ? string.Empty : nodes.Select(f => f.Id).Aggregate((a, b) => a + b);
            Id = $"{nameof(TranslatedSetTreeNode)}{setId}";
        }

        public List<TranslatedSetOperatorNode> Nodes { get; }

        public override Type ReturnType { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return Nodes.ConvertAll(f => f.ToString()).Aggregate((a, b) => $"{a}{Environment.NewLine}{b}");
        }
    }
}