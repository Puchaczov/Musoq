using System;

namespace FQL.Parser.Nodes
{
    public class WordNode : Node
    {
        public WordNode(string value)
        {
            Value = value;
            Id = $"{nameof(WordNode)}{value}{ReturnType.Name}";
        }

        public string Value { get; }

        public override Type ReturnType => typeof(string);

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"'{Value}'";
        }
    }
}