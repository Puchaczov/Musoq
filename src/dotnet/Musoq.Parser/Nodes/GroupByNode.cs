using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class GroupByNode : Node
{
    public GroupByNode(FieldNode[] fields, HavingNode node)
        : this(fields, node, default)
    {
    }

    public GroupByNode(FieldNode[] fields, HavingNode node, TextSpan span)
    {
        Fields = fields;
        Having = node;
        var fieldsIds = fields.Length == 0 ? string.Empty : string.Concat(fields.Select(f => f.Id));
        Id = $"{nameof(GroupByNode)}{fieldsIds}{node?.Id}";

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

    public HavingNode Having { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var fields = Fields.Length == 0
            ? string.Empty
            : string.Join(", ", Fields.Select(f => f.ToString()));
        var groupBy = $"group by {fields}";

        if (Having == null)
            return groupBy;

        return $"{groupBy} {Having.ToString()}";
    }
}
