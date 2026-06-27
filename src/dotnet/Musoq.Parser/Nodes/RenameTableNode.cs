namespace Musoq.Parser.Nodes;

public class RenameTableNode(string tableSourceName, string tableDestinationName) : Node
{
    public string TableSourceName { get; } = tableSourceName;

    public string TableDestinationName { get; } = tableDestinationName;

    public override Type ReturnType => typeof(void);

    public override string Id => $"{nameof(RenameTableNode)}{TableSourceName}{TableDestinationName}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"rename {TableSourceName} as {TableDestinationName}";
    }
}
