using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class OrderByNode : Node
{
    public OrderByNode(FieldOrderedNode[] fields)
    {
        Fields = fields;
        var fieldsId = fields.Length == 0 ? string.Empty : string.Concat(fields.Select(f => f.Id));
        Id = $"{nameof(OrderByNode)}{fieldsId}";
    }

    public FieldOrderedNode[] Fields { get; }

    public override Type ReturnType { get; } = typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var fieldsTxt = Fields.Length == 0
            ? string.Empty
            : string.Join(", ", Fields.Select(FieldToString));
        return $"order by {fieldsTxt}";
    }

    private static string FieldToString(FieldOrderedNode node)
    {
        return node.ToString();
    }
}
