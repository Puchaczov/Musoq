using System;

namespace Musoq.Parser.Nodes.From
{
    public class InMemoryGroupedFromNode : FromNode
    {
        internal InMemoryGroupedFromNode(string alias)
            : base(alias)
        {
            Id = $"{nameof(InMemoryTableFromNode)}{Alias}";
        }
        
        public InMemoryGroupedFromNode(string alias, Type returnType)
            : base(alias, returnType)
        {
            Id = $"{nameof(InMemoryTableFromNode)}{Alias}";
        }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Alias;
        }
    }
}