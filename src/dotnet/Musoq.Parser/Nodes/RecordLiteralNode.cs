using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Musoq.Parser.Nodes;

/// <summary>
///     A named field inside a structural record literal or declaration.
/// </summary>
public sealed class RecordFieldNode
{
    public RecordFieldNode(string name, Node expression, TextSpan nameSpan, TextSpan span)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Field name cannot be empty.", nameof(name)) : name;
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        NameSpan = nameSpan;
        Span = span;
    }

    public RecordFieldNode(string name, Node expression)
        : this(name, expression, TextSpan.Empty, expression?.Span ?? TextSpan.Empty)
    {
    }

    public string Name { get; }

    public Node Expression { get; }

    public TextSpan NameSpan { get; }

    public TextSpan Span { get; }

    public string Id => $"{nameof(RecordFieldNode)}{Name}{Expression.Id}";

    public override string ToString() => $"{Name}: {Expression}";
}

/// <summary>
///     A parenthesized named structural record value.
///
///     It derives from <see cref="ArgsListNode"/> so existing visitors and argument
///     traversal continue to visit the child expressions while the semantic binder can
///     distinguish a record from an ordinary call argument list.
/// </summary>
public sealed class RecordLiteralNode : ArgsListNode
{
    public RecordLiteralNode(IReadOnlyList<RecordFieldNode> fields, TextSpan span)
        : base(
            fields?.Select(static field => field.Expression).ToArray() ?? throw new ArgumentNullException(nameof(fields)),
            fields.Select(static field => new ArgumentName(field.Name, field.NameSpan)).Cast<ArgumentName?>().ToArray(),
            span)
    {
        Fields = new ReadOnlyCollection<RecordFieldNode>(fields.ToArray());
    }

    public RecordLiteralNode(IReadOnlyList<RecordFieldNode> fields)
        : this(fields, TextSpan.Empty)
    {
    }

    public IReadOnlyList<RecordFieldNode> Fields { get; }

    public override Type? ReturnType => null;

    public override string Id => $"{nameof(RecordLiteralNode)}{string.Concat(Fields.Select(static item => item.Id))}";

    public override string ToString() => $"({base.ToString()})";
}
