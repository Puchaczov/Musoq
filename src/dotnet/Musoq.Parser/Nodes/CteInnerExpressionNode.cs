namespace Musoq.Parser.Nodes;

public class CteInnerExpressionNode : Node
{
    public CteInnerExpressionNode(Node value, string name)
        : this(value, name, [], false)
    {
    }

    public CteInnerExpressionNode(Node value, string name, CteColumnName[] columns)
        : this(value, name, columns, false)
    {
    }

    public CteInnerExpressionNode(
        Node value,
        string name,
        CteColumnName[] columns,
        bool isRecursiveDefinition)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(columns);

        Value = value;
        Name = name;
        Columns = columns;
        IsRecursiveDefinition = isRecursiveDefinition;
    }

    public Node Value { get; }

    public string Name { get; }

    public CteColumnName[] Columns { get; }

    public string[] ColumnNames => Array.ConvertAll(Columns, static column => column.Name);

    public bool IsRecursiveDefinition { get; }

    public override Type ReturnType => typeof(void);

    public override string Id =>
        $"{nameof(CteInnerExpressionNode)}{(IsRecursiveDefinition ? "Recursive" : string.Empty)}{Value.Id}{string.Concat(Array.ConvertAll(Columns, static column => column.Name))}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var columns = Columns.Length == 0
            ? string.Empty
            : $" ({string.Join(", ", Array.ConvertAll(Columns, static column => column.Name))})";

        return $"{Name}{columns} as {Value.ToString()}";
    }
}
