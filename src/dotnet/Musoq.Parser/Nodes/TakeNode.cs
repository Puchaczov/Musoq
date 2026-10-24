namespace Musoq.Parser.Nodes;

public class TakeNode : UnaryNode
{
    public TakeNode(IntegerNode expression) : this((Node)expression)
    {
    }

    public TakeNode(Node expression) : base(expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (expression is not IntegerNode and not ParameterReferenceNode and not ScriptVariableReferenceNode)
            throw new ArgumentException("Paging count must be an integer literal or a direct script reference.", nameof(expression));
        Id = $"{nameof(TakeNode)}{ReturnType.Name}{Expression.Id}";
    }

    public override Type ReturnType => typeof(long);

    public long Value => Expression is IntegerNode integer
        ? Convert.ToInt64(integer.ObjValue, System.Globalization.CultureInfo.InvariantCulture)
        : throw new InvalidOperationException("A parameterized paging count has no constant value.");

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"take {Expression.ToString()}";
    }
}
