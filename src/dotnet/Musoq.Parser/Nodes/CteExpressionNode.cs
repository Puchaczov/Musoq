using System.Text;

namespace Musoq.Parser.Nodes;

public class CteExpressionNode(CteInnerExpressionNode[] sets, Node outerSets) : Node
{
    public override Type ReturnType => typeof(void);

    public CteInnerExpressionNode[] InnerExpression { get; } = sets;

    public Node OuterExpression { get; } = outerSets;

    public override string Id => $"{nameof(CteExpressionNode)}{OuterExpression.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var query = new StringBuilder();

        query.Append("with");
        query.Append(' ');

        for (var i = 0; i < InnerExpression.Length - 1; i++)
        {
            query.Append('(');
            query.Append(InnerExpression[i].ToString());
            query.Append("), ");
        }

        query.Append('(');
        query.Append(InnerExpression[^1].ToString());
        query.Append(") ");
        query.Append(OuterExpression.ToString());

        return query.ToString();
    }
}
