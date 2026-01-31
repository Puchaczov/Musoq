namespace Musoq.Parser.Nodes;

public abstract class UnaryNode : Node
{
    protected UnaryNode(Node expression)
        : this(expression, default)
    {
    }

    protected UnaryNode(Node expression, TextSpan span)
    {
        Expression = expression;

        // Inherit span from expression if not provided
        if (span.IsEmpty && expression?.HasSpan == true)
        {
            Span = expression.Span;
            FullSpan = expression.Span;
        }
        else
        {
            Span = span;
            FullSpan = span;
        }
    }

    public Node Expression { get; }

    protected static string CalculateId<T>(T node)
        where T : UnaryNode
    {
        return $"{typeof(T).Name}{node.Expression.Id}{node.ReturnType.Name}";
    }
}
