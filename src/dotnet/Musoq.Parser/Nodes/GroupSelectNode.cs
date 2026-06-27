namespace Musoq.Parser.Nodes;

public class GroupSelectNode(FieldNode[] fields) : SelectNode(fields)
{
    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
