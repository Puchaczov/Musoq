using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class SelectNode : Node
{
    public SelectNode(FieldNode[] fields, bool isDistinct = false)
        : this(fields, isDistinct, default)
    {
    }

    public SelectNode(FieldNode[] fields, bool isDistinct, TextSpan span)
    {
        Fields = fields;
        IsDistinct = isDistinct;
        var fieldsId = fields.Length == 0 ? string.Empty : string.Concat(fields.Select(f => f.Id));
        var distinctPrefix = isDistinct ? "Distinct" : "";
        Id = $"{distinctPrefix}{nameof(SelectNode)}{fieldsId}";

        // If no explicit span provided, compute from fields
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

    public bool IsDistinct { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var distinctKeyword = IsDistinct ? "distinct " : "";
        var fieldsTxt = Fields.Length == 0
            ? string.Empty
            : string.Join(", ", Fields.Select(FieldToString));
        return $"select {distinctKeyword}{fieldsTxt} ";
    }

    private static string FieldToString(FieldNode node)
    {
        return node.ToString();
    }
}
