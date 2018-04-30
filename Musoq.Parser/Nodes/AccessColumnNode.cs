using System;

namespace Musoq.Parser.Nodes
{
    public class AccessColumnNode : Node
    {
        private Type _returnType;

        public AccessColumnNode(string column, string alias, TextSpan span)
            : this(column, alias, typeof(void), span)
        {
            Id = $"{nameof(AccessColumnNode)}{column}";
        }

        public AccessColumnNode(string column, string alias, Type returnType, TextSpan span)
        {
            Alias = alias;
            Span = span;
            _returnType = returnType;
            Name = column;
            Id = $"{nameof(AccessColumnNode)}{column}{returnType.Name}";
        }

        public string Name { get; }

        public string Alias { get; }

        public TextSpan Span { get; }

        public override Type ReturnType => _returnType;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

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

    public class IdentifierNode : Node
    {
        public IdentifierNode(string name)
        {
            Name = name;
            Id = $"{nameof(IdentifierNode)}{Name}";
        }
        public string Name { get; }
        public override Type ReturnType { get; }
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public override string ToString()
        {
            return Name;
        }
    }

    public class TableReferenceNode : IdentifierNode
    {
        public TableReferenceNode(string name) 
            : base(name)
        {
        }
    }
}