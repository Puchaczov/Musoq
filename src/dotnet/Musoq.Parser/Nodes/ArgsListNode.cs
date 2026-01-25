using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class ArgsListNode : Node
{
    public ArgsListNode(Node[] args)
    {
        Args = args;

        var argsId = args.Length == 0 ? string.Empty : string.Concat(args.Select(f => f.Id));
        Id = $"{nameof(ArgsListNode)}{argsId}";
    }

    public static ArgsListNode Empty => new([]);

    public Node[] Args { get; }

    public override Type ReturnType => Args[0].ReturnType;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Args.Length == 0
            ? string.Empty
            : string.Join(", ", Args.Select(f => f.ToString()));
    }

    public string ToStringWithBrackets()
    {
        var str = Args.Length == 0
            ? string.Empty
            : string.Join(", ", Args.Select(f => f.ToString()));
        return $"({str})";
    }
}
