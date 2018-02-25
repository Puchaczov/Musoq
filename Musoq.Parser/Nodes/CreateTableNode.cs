using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class CreateTableNode : Node
    {
        public CreateTableNode(string name, string[] keys, FieldNode[] fields)
        {
            Name = name;
            Keys = keys;
            Fields = fields;
            var keysId = keys.Length == 0 ? string.Empty : keys.Aggregate((a, b) => a + b);
            Id = $"{nameof(CreateTableNode)}{name}{keysId}";
        }


        public string[] Keys { get; }

        public FieldNode[] Fields { get; }

        public string Name { get; }

        public override Type ReturnType => null;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"CREATE TABLE {Name}";
        }
    }
}