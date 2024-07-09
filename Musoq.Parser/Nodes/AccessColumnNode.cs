using System;

namespace Musoq.Parser.Nodes
{
    public class AccessColumnNode(string column, string alias, Type type, TextSpan span)
        : IdentifierNode(column)
    {
        public AccessColumnNode(string column, string alias, TextSpan span)
            : this(column, alias, typeof(void), span)
        {
            Id = $"{nameof(AccessColumnNode)}{column}";
        }

        public string Alias { get; } = alias;

        public TextSpan Span { get; } = span;

        public override Type ReturnType => type;

        public override string Id { get; } = $"{nameof(AccessColumnNode)}{column}{type.Name}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Alias))
                return Name;
            return $"{Alias}.{Name}";
        }

        public void ChangeReturnType(Type returnType)
        {
            type = returnType;
        }
    }
}