using System.Text;

namespace Musoq.Parser.Nodes;

public class CteExpressionNode : Node
{
    public CteExpressionNode(CteInnerExpressionNode[] sets, Node outerSets)
        : this(sets, outerSets, false)
    {
    }

    public CteExpressionNode(CteInnerExpressionNode[] sets, Node outerSets, bool isRecursive)
    {
        ArgumentNullException.ThrowIfNull(sets);
        ArgumentNullException.ThrowIfNull(outerSets);
        InnerExpression = sets;
        OuterExpression = outerSets;
        IsRecursive = isRecursive;
    }

    public override Type ReturnType => typeof(void);

    public CteInnerExpressionNode[] InnerExpression { get; }

    public Node OuterExpression { get; }

    public bool IsRecursive { get; }

    public override string Id => $"{nameof(CteExpressionNode)}{(IsRecursive ? "Recursive" : string.Empty)}{OuterExpression.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var query = new StringBuilder();

        query.Append("with");
        if (IsRecursive)
            query.Append(" recursive");
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
