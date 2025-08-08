using System;
using System.Collections.Generic;

namespace Musoq.Parser.Nodes;

public class WindowSpecificationNode : Node
{
    public Node PartitionBy { get; }
    public Node OrderBy { get; }
    public WindowFrameNode WindowFrame { get; }

    public WindowSpecificationNode(Node partitionBy, Node orderBy, WindowFrameNode windowFrame = null)
    {
        PartitionBy = partitionBy;
        OrderBy = orderBy;
        WindowFrame = windowFrame;
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
        if (WindowFrame != null)
            parts.Add(WindowFrame.ToString());
        return string.Join(" ", parts);
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}