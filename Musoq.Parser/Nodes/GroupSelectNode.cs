namespace Musoq.Parser.Nodes;

public class GroupSelectNode : SelectNode
{
    public GroupSelectNode(FieldNode[] fields)
        : base(fields)
    {
    }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }
}
