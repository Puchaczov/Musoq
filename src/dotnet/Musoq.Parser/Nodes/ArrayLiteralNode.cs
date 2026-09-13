using System.Collections.Generic;
using System.Linq;

namespace Musoq.Parser.Nodes;

/// <summary>
///     A contextual <c>array { ... }</c> structural collection value.
/// </summary>
public sealed class ArrayLiteralNode : ArgsListNode
{
    public ArrayLiteralNode(IReadOnlyList<Node> elements, TextSpan span)
        : base(elements?.ToArray() ?? throw new ArgumentNullException(nameof(elements)), span)
    {
    }

    public ArrayLiteralNode(IReadOnlyList<Node> elements)
        : this(elements, TextSpan.Empty)
    {
    }

    public IReadOnlyList<Node> Elements => Args;

    public override Type? ReturnType => null;

    public override string Id => $"{nameof(ArrayLiteralNode)}{string.Concat(Elements.Select(static element => element.Id))}";

    public override string ToString() => $"array {{ {base.ToString()} }}";
}
