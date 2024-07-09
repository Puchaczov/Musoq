using System;

namespace Musoq.Schema;

public class SchemaTableMetadata(Type tableEntityType)
{
    public Type TableEntityType { get; } = tableEntityType;
}