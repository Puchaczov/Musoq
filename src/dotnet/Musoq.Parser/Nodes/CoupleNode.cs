using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Nodes;

public class CoupleNode(SchemaMethodFromNode from, string? tableName, string? profileName, string mappedSchemaName)
    : Node
{
    public SchemaMethodFromNode SchemaMethodNode { get; } = from;

    public string MappedSchemaName { get; } = mappedSchemaName;

    public string? ProfileName { get; } = profileName;

    public string? TableName { get; } = tableName;

    public override Type ReturnType => typeof(void);

    public override string Id => $"{SchemaMethodNode.ToString()}{TableName}{ProfileName}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var options = TableName != null && ProfileName != null
            ? $"table {TableName} and settings {ProfileName}"
            : TableName != null
                ? $"table {TableName}"
                : $"settings {ProfileName}";

        return $"couple {SchemaMethodNode.ToString()} with {options} as {MappedSchemaName};";
    }
}
