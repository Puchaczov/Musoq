using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class WindowSpecificationNode : Node
{
    public WindowSpecificationNode(FieldNode[] partitionFields, FieldOrderedNode[] orderByFields)
    {
        PartitionFields = partitionFields ?? [];
        OrderByFields = orderByFields ?? [];

        var partitionId = PartitionFields.Length == 0
            ? string.Empty
            : string.Concat(PartitionFields.Select(f => f.Id));
        var orderById = OrderByFields.Length == 0
            ? string.Empty
            : string.Concat(OrderByFields.Select(f => f.Id));

        Id = $"{nameof(WindowSpecificationNode)}{partitionId}{orderById}";
    }

    public FieldNode[] PartitionFields { get; }

    public FieldOrderedNode[] OrderByFields { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var partition = PartitionFields.Length > 0
            ? $"partition by {string.Join(", ", PartitionFields.Select(f => f.Expression.ToString()))}"
            : string.Empty;

        var orderBy = OrderByFields.Length > 0
            ? $"order by {string.Join(", ", OrderByFields.Select(f => f.ToString()))}"
            : string.Empty;

        var spec = string.Join(" ", new[] { partition, orderBy }.Where(s => s.Length > 0));
        return $"({spec})";
    }
}
