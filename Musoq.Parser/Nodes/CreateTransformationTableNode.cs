using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class CreateTransformationTableNode : Node
    {
        public CreateTransformationTableNode(string name, string[] keys, FieldNode[] fields, bool forGrouping)
        {
            Name = name;
            Keys = keys;
            Fields = fields;
            ForGrouping = forGrouping;
            var keysId = keys.Length == 0 ? string.Empty : keys.Aggregate((a, b) => a + b);
            Id = $"{nameof(CreateTransformationTableNode)}{name}{keysId}";
        }

        public string[] Keys { get; }

        public FieldNode[] Fields { get; }

        public bool ForGrouping { get; }

        public string Name { get; }

        public override Type ReturnType => null;

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"create transform table {Name}";
        }
    }
}