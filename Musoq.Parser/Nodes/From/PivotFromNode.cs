using System;

namespace Musoq.Parser.Nodes.From;

public class PivotFromNode : FromNode
{
    public PivotFromNode(FromNode source, PivotNode pivot)
        : base(source.Alias)
    {
        Source = source;
        Pivot = pivot;
        Id = $"{nameof(PivotFromNode)}{source.Id}{pivot.Id}";
    }

    public FromNode Source { get; }
    
    public PivotNode Pivot { get; }

    public override string Id { get; }

    public override Type ReturnType => Source.ReturnType;

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Source} {Pivot}";
    }
}