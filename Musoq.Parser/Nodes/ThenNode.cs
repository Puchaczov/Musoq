using System;

namespace Musoq.Parser.Nodes;

public class ThenNode : UnaryNode
{
    public ThenNode(Node expression) 
        : base(expression)
    {
        Id = $"{nameof(ThenNode)}{Expression.Id}";
    }

    public override Type ReturnType => Expression.ReturnType;
    
    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"then {Expression.ToString()}";
    }
}