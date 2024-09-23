using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class GroupByNode : Node
{
    public GroupByNode(FieldNode[] fields, HavingNode node)
    {   
        Fields = fields;
        Having = node;
        var fieldsIds = fields.Length == 0 ? string.Empty : fields.Select(f => f.Id).Aggregate((a, b) => a + b);
        Id = $"{nameof(GroupByNode)}{fieldsIds}{node?.Id}";
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
        var fields = Fields.Length == 0 ? string.Empty : Fields.Select(f => f.ToString()).Aggregate((a, b) => $"{a.ToString()}, {b.ToString()}");
        var groupBy = $"group by {fields}";
            
        if (Having == null)
            return groupBy;
            
        return $"{groupBy} {Having.ToString()}";
    }
}