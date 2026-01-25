#nullable enable
using System;

namespace Musoq.Parser.Nodes;

public class DescNode(FromNode from, DescForType type, Node? column) : Node
{
    public DescNode(FromNode from, DescForType type)
        : this(from, type, null)
    {
    }

    public DescForType Type { get; set; } = type;

    public FromNode From { get; } = from;

    public Node? Column { get; } = column;

    public override Type ReturnType { get; }

    public override string Id { get; } = column != null
        ? $"{nameof(DescNode)}{from.Id}_{column.Id}"
        : $"{nameof(DescNode)}{from.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return column != null
            ? $"desc {From.ToString()} column {column.ToString()}"
            : $"desc {From.ToString()}";
    }
}
