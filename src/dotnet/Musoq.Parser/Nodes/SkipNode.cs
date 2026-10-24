namespace Musoq.Parser.Nodes;

public class SkipNode : UnaryNode
{
    public SkipNode(IntegerNode expression) : this((Node)expression)
    {
    }

    public SkipNode(Node expression) : base(expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        if (expression is not IntegerNode and not ParameterReferenceNode and not ScriptVariableReferenceNode)
            throw new ArgumentException("Paging count must be an integer literal or a direct script reference.", nameof(expression));
        Id = $"{nameof(SkipNode)}{ReturnType.Name}{Expression.Id}";
    }

    public long Value => Expression is IntegerNode integer
        ? Convert.ToInt64(integer.ObjValue, System.Globalization.CultureInfo.InvariantCulture)
        : throw new InvalidOperationException("A parameterized paging count has no constant value.");

    public override Type ReturnType => typeof(long);

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"skip {Expression.ToString()}";
    }
}
