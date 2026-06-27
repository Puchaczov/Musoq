namespace Musoq.Parser.Nodes;

public class SingleSetNode(QueryNode query) : Node
{
    public QueryNode Query { get; } = query;

    public override Type ReturnType => typeof(void);

    public override string Id => $"{nameof(SingleSetNode)}{Query.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return Query.ToString();
    }
}
