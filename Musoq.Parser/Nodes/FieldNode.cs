using System;

namespace FQL.Parser.Nodes
{
    public class FieldNode : Node
    {
        private readonly string _fieldName;

        public FieldNode(Node expression, int fieldOrder, string fieldName)
        {
            Expression = expression;
            FieldOrder = fieldOrder;
            _fieldName = fieldName;
            Id = $"{nameof(FieldNode)}{expression.Id}";
        }

        public Node Expression { get; }

        public int FieldOrder { get; }

        public string FieldName => string.IsNullOrEmpty(_fieldName) ? Expression.ToString() : _fieldName;

        public override Type ReturnType => Expression.ReturnType;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(_fieldName)
                ? $"{Expression.ToString()}"
                : $"{Expression.ToString()} as {_fieldName}";
        }
    }
}