using System;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class WindowFunctionNode : Node
{
    public WindowFunctionNode(AccessMethodNode function, OverClauseNode overClause)
    {
        Function = function;
        OverClause = overClause;
        Id = $"{nameof(WindowFunctionNode)}{function.Id}{overClause.Id}";
    }

    public AccessMethodNode Function { get; }
    
    public OverClauseNode OverClause { get; }

    public override Type ReturnType => Function.ReturnType;

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Function} {OverClause}";
    }
}

public class OverClauseNode : Node
{
    public OverClauseNode(Node partitionBy, Node orderBy)
    {
        PartitionBy = partitionBy;
        OrderBy = orderBy;
        Id = $"{nameof(OverClauseNode)}{partitionBy?.Id ?? ""}{orderBy?.Id ?? ""}";
    }

    public Node PartitionBy { get; }
    
    public Node OrderBy { get; }

    public override Type ReturnType => typeof(void);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        
        if (PartitionBy != null)
            parts.Add($"PARTITION BY {PartitionBy}");
            
        if (OrderBy != null)
            parts.Add($"ORDER BY {OrderBy}");
            
        return $"OVER ({string.Join(" ", parts)})";
    }
}