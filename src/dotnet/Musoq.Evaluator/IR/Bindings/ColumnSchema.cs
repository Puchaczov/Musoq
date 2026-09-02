using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.IR.Bindings;

public sealed record ColumnSchema
{
    public ColumnSchema(
        string name,
        Type type,
        int index,
        string? intendedTypeName = null)
        : this(name, type, index, intendedTypeName, type, null)
    {
    }

    public ColumnSchema(
        string name,
        Type type,
        int index,
        string? intendedTypeName,
        Type sourceReadType,
        EnumTypeDescriptor? enumType)
    {
        Name = name;
        Type = type;
        Index = index;
        IntendedTypeName = intendedTypeName;
        SourceReadType = sourceReadType;
        EnumType = enumType;
    }

    public string Name { get; init; }

    public Type Type { get; init; }

    public int Index { get; init; }

    public string? IntendedTypeName { get; init; }

    public Type SourceReadType { get; init; }

    public EnumTypeDescriptor? EnumType { get; init; }

    public ColumnStability Stability { get; init; } = ColumnStability.Stable;

    public ISchemaColumn ToSchemaColumn()
    {
        return new SchemaColumn(
            Name,
            Index,
            Type,
            SourceReadType,
            EnumType,
            IntendedTypeName,
            null,
            Stability);
    }
}
