using System.Linq;

namespace Musoq.Parser.Nodes;

public class ArgsListNode : Node
{
    public ArgsListNode(Node[] args)
        : this(args, null, default)
    {
    }

    public ArgsListNode(Node[] args, TextSpan span)
        : this(args, null, span)
    {
    }

    public ArgsListNode(Node[] args, ArgumentName?[]? argumentNames, TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (argumentNames != null && argumentNames.Length != args.Length)
            throw new ArgumentException("Argument-name metadata must align with the argument expressions.", nameof(argumentNames));

        Args = args;
        ArgumentNames = argumentNames ?? new ArgumentName?[args.Length];

        var argsId = args.Length == 0
            ? string.Empty
            : string.Concat(args.Select((f, index) =>
                ArgumentNames[index] is { } name
                    ? $"{name.Name}:{f.Id}"
                    : f.Id));
        Id = $"{nameof(ArgsListNode)}{argsId}";

        // If no explicit span provided, compute from first and last args
        if (span.IsEmpty && args.Length > 0)
        {
            Span = ComputeSpan(args);
            foreach (var argumentName in ArgumentNames)
                if (argumentName is { Span.IsEmpty: false } name)
                    Span = Span.Through(name.Span);
            FullSpan = Span;
        }
        else
        {
            Span = span;
            FullSpan = span;
        }
    }

    public static ArgsListNode Empty => new([]);

    public Node[] Args { get; }

    /// <summary>
    ///     Gets optional labels aligned with <see cref="Args"/>.
    /// </summary>
    public ArgumentName?[] ArgumentNames { get; }

    public bool HasNamedArguments => ArgumentNames.Any(static name => name.HasValue);

    public override Type? ReturnType => Args.Length == 0 ? null : Args[0].ReturnType;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Args.Length == 0
            ? string.Empty
            : string.Join(", ", Args.Select((f, index) =>
                ArgumentNames[index] is { } name
                    ? $"{name.Name}: {f.ToString()}"
                    : f.ToString()));
    }

    public string ToStringWithBrackets()
    {
        var str = Args.Length == 0
            ? string.Empty
            : string.Join(", ", Args.Select((f, index) =>
                ArgumentNames[index] is { } name
                    ? $"{name.Name}: {f.ToString()}"
                    : f.ToString()));
        return $"({str})";
    }
}
