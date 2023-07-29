using System;

namespace Musoq.Schema;

public class SchemaTableMetadata
{
    public SchemaTableMetadata(Type tableEntityType)
    {
        TableEntityType = tableEntityType;
    }

    public Type TableEntityType { get; }
}