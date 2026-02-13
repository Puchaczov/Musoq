#nullable enable
using System;

namespace Musoq.Parser.Nodes;

public class DescNode : Node
{
    public DescNode(FromNode from, DescForType type, Node? column)
    {
        Type = type;
        From = from;
        Column = column;
        Id = column != null
            ? $"{nameof(DescNode)}{from.Id}_{column.Id}"
            : $"{nameof(DescNode)}{from.Id}";
    }

    public DescNode(FromNode from, DescForType type)
        : this(from, type, null)
    {
    }

    public DescForType Type { get; set; }

    public FromNode From { get; }

    public Node? Column { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Column != null
            ? $"desc {From.ToString()} column {Column.ToString()}"
            : $"desc {From.ToString()}";
    }
}
