using System;
using System.Collections.Generic;

namespace Musoq.Parser.Nodes;

public class WindowSpecificationNode : Node
{
    public Node PartitionBy { get; }
    public Node OrderBy { get; }

    public WindowSpecificationNode(Node partitionBy, Node orderBy)
    {
        PartitionBy = partitionBy;
        OrderBy = orderBy;
        Id = $"{nameof(WindowSpecificationNode)}{GetHashCode()}";
    }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override string ToString()
    {
        var parts = new List<string>();
        if (PartitionBy != null)
            parts.Add($"PARTITION BY {PartitionBy}");
        if (OrderBy != null)
            parts.Add($"ORDER BY {OrderBy}");
        return string.Join(" ", parts);
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}