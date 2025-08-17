using System;
using System.Linq;

namespace Musoq.Parser.Nodes;

public class PivotNode : Node
{
    public PivotNode(Node[] aggregationExpressions, FieldNode forColumn, Node[] inValues)
    {
        AggregationExpressions = aggregationExpressions;
        ForColumn = forColumn;
        InValues = inValues;
        
        var aggregationIds = aggregationExpressions.Length == 0 ? string.Empty : 
            aggregationExpressions.Select(a => a.Id).Aggregate((a, b) => a + b);
        var inValuesIds = inValues.Length == 0 ? string.Empty : 
            inValues.Select(v => v.Id).Aggregate((a, b) => a + b);
        
        Id = $"{nameof(PivotNode)}{aggregationIds}{forColumn.Id}{inValuesIds}";
    }

    public Node[] AggregationExpressions { get; }

    public FieldNode ForColumn { get; }

    public Node[] InValues { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var aggregations = AggregationExpressions.Length == 0 ? string.Empty : 
            AggregationExpressions.Select(a => a.ToString()).Aggregate((a, b) => $"{a}, {b}");
        var inValues = InValues.Length == 0 ? string.Empty : 
            InValues.Select(v => v.ToString()).Aggregate((a, b) => $"{a}, {b}");
        
        return $"pivot ({aggregations} for {ForColumn} in ({inValues}))";
    }
}