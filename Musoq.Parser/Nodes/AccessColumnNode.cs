using System;

namespace Musoq.Parser.Nodes
{
    public class AccessColumnNode : IdentifierNode
    {
        private Type _returnType;

        public AccessColumnNode(string column, string alias, TextSpan span)
            : this(column, alias, typeof(void), span)
        {
            Id = $"{nameof(AccessColumnNode)}{column}";
        }

        public AccessColumnNode(string column, string alias, Type returnType, TextSpan span)
            : base(column)
        {
            Alias = alias;
            Span = span;
            _returnType = returnType;
            Id = $"{nameof(AccessColumnNode)}{column}{returnType.Name}";
        }

        public string Alias { get; }

        public TextSpan Span { get; }

        public override Type ReturnType => _returnType;

        public override string Id { get; }

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
            _returnType = returnType;
        }
    }
}