namespace Musoq.Parser.Nodes;

public class AccessRawIdentifierNode(string name, Type? returnType = null) : IdentifierNode(name, returnType)
{
    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }
}
