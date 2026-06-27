using System.Linq;

namespace Musoq.Parser.Nodes;

public class WindowSpecificationNode : Node
{
    public WindowSpecificationNode(FieldNode[]? partitionFields, FieldOrderedNode[]? orderByFields, WindowFrameNode? frame = null)
    {
        PartitionFields = partitionFields ?? [];
        OrderByFields = orderByFields ?? [];
        Frame = frame;

        var partitionId = PartitionFields.Length == 0
            ? string.Empty
            : string.Concat(PartitionFields.Select(f => f.Id));
        var orderById = OrderByFields.Length == 0
            ? string.Empty
            : string.Concat(OrderByFields.Select(f => f.Id));

        Id = $"{nameof(WindowSpecificationNode)}{partitionId}{orderById}{frame?.Id}";
    }

    public FieldNode[] PartitionFields { get; }

    public FieldOrderedNode[] OrderByFields { get; }

    public WindowFrameNode? Frame { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
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

        var frame = Frame != null ? Frame.ToString() : string.Empty;

        var spec = string.Join(" ", new[] { partition, orderBy, frame }.Where(s => s.Length > 0));
        return $"({spec})";
    }
}
