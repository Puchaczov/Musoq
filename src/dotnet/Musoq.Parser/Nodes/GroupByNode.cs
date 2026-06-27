using System.Linq;

namespace Musoq.Parser.Nodes;

public class GroupByNode : Node
{
    public GroupByNode(FieldNode[] fields, HavingNode? node)
        : this(fields, node, false, default)
    {
    }

    public GroupByNode(FieldNode[] fields, HavingNode? node, TextSpan span)
        : this(fields, node, false, span)
    {
    }

    public GroupByNode(FieldNode[] fields, HavingNode? node, bool isAll)
        : this(fields, node, isAll, default)
    {
    }

    public GroupByNode(FieldNode[] fields, HavingNode? node, bool isAll, TextSpan span)
    {
        ArgumentNullException.ThrowIfNull(fields);
        Fields = fields;
        Having = node;
        IsAll = isAll;
        var fieldsIds = fields.Length == 0 ? string.Empty : string.Concat(fields.Select(f => f.Id));
        var allPrefix = isAll ? "All" : string.Empty;
        Id = $"{nameof(GroupByNode)}{allPrefix}{fieldsIds}{node?.Id}";

        // Compute span from fields
        if (span.IsEmpty && fields.Length > 0)
        {
            Span = ComputeSpan(fields.Cast<Node>().ToArray());
            FullSpan = Span;
        }
        else
        {
            Span = span;
            FullSpan = span;
        }
    }

    public FieldNode[] Fields { get; }

    public HavingNode? Having { get; }

    public bool IsAll { get; }

    public override Type? ReturnType => null;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var fields = IsAll
            ? "all"
            : Fields.Length == 0
            ? string.Empty
            : string.Join(", ", Fields.Select(f => f.ToString()));
        var groupBy = $"group by {fields}";

        if (Having == null)
            return groupBy;

        return $"{groupBy} {Having.ToString()}";
    }
}
