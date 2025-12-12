using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class SelectNode : Node
{
    public SelectNode(FieldNode[] fields, bool isDistinct = false)
    {
        Fields = fields;
        IsDistinct = isDistinct;
        var fieldsId = fields.Length == 0 ? string.Empty : fields.Select(f => f.Id).Aggregate((a, b) => a + b);
        var distinctPrefix = isDistinct ? "Distinct" : "";
        Id = $"{distinctPrefix}{nameof(SelectNode)}{fieldsId}";
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
            : Fields.Select(FieldToString).Aggregate((a, b) => $"{a}, {b}");
        return $"select {distinctKeyword}{fieldsTxt} ";
    }

    private static string FieldToString(FieldNode node)
    {
        return node.ToString();
    }
}