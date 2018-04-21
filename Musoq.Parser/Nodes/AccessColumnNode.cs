using System;

namespace Musoq.Parser.Nodes
{
    public class AccessColumnNode : Node
    {
        private Type _returnType;

        public AccessColumnNode(string column, TextSpan span)
            : this(column, 0, typeof(void), span)
        {
            Id = $"{nameof(AccessColumnNode)}{column}";
        }

        public AccessColumnNode(string column, int argFieldOrder, Type returnType, TextSpan span)
        {
            Order = argFieldOrder;
            Span = span;
            _returnType = returnType;
            Name = column;
            Id = $"{nameof(AccessColumnNode)}{column}{returnType.Name}";
        }

        public string Name { get; }

        public int Order { get; }

        public TextSpan Span { get; }

        public override Type ReturnType => _returnType;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return Name;
        }

        public void ChangeReturnType(Type returnType)
        {
            _returnType = returnType;
        }
    }
}