using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class OrderByNode : Node
    {
        public OrderByNode(FieldNode[] fields)
        {
            Fields = fields;
            var fieldsId = fields.Length == 0 ? string.Empty : fields.Select(f => f.Id).Aggregate((a, b) => a + b);
            Id = $"{nameof(OrderByNode)}{fieldsId}";
        }

        public FieldNode[] Fields { get; }

        public override Type ReturnType { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var fieldsTxt = Fields.Length == 0
                ? string.Empty
                : Fields.Select(FieldToString).Aggregate((a, b) => $"{a}, {b}");
            return $"order by {fieldsTxt}";
        }

        private string FieldToString(FieldNode node)
        {
            return string.IsNullOrEmpty(node.FieldName) ? node.Expression.ToString() : node.FieldName;
        }
    }
}