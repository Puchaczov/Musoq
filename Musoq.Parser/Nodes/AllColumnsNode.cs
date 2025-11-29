using System;

namespace Musoq.Parser.Nodes;

public class AllColumnsNode(string alias = null) : Node
{
    public string Alias { get; private set; } = alias;

    public override Type ReturnType => typeof(object[]);

    public override string Id => $"{nameof(AllColumnsNode)}{Alias ?? string.Empty}*";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(Alias))
            return $"{Alias}.*";
            
        return "*";
    }
}