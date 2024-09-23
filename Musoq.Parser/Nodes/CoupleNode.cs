using System;
using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Nodes;

public class CoupleNode : Node
{
    public CoupleNode(SchemaMethodFromNode from, string tableName, string mappedSchemaName)
    {
        SchemaMethodNode = from;
        TableName = tableName;
        MappedSchemaName = mappedSchemaName;
    }

    public SchemaMethodFromNode SchemaMethodNode { get; }

    public string MappedSchemaName { get; }

    public string TableName { get; }

    public override Type ReturnType => typeof(void);

    public override string Id => $"{SchemaMethodNode.ToString()}{TableName}";

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"couple {SchemaMethodNode.ToString()} with table {TableName} as {MappedSchemaName};";
    }
}