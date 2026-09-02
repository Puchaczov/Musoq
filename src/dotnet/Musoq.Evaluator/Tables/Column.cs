using System.Collections.Generic;
using System.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{ColumnIndex}. {ColumnName}: {ColumnType.Name}")]
public class Column(string name, Type columnType,
    int columnOrder,
    Type? sourceReadType = null,
    EnumTypeDescriptor? enumType = null) : IEquatable<Column>, ISchemaColumn
{
    public bool Equals(Column? other)
    {
        return other != null &&
               ColumnName == other.ColumnName &&
               EqualityComparer<Type>.Default.Equals(ColumnType, other.ColumnType) &&
               EqualityComparer<Type>.Default.Equals(SourceReadType, other.SourceReadType) &&
               EqualityComparer<EnumTypeDescriptor?>.Default.Equals(EnumType, other.EnumType) &&
               ColumnIndex == other.ColumnIndex;
    }

    public string ColumnName { get; } = name;

    public Type ColumnType { get; } = columnType;

    public Type SourceReadType { get; } = sourceReadType ?? columnType;

    public EnumTypeDescriptor? EnumType { get; } = enumType;

    public int ColumnIndex { get; } = columnOrder;

    public override bool Equals(object? obj)
    {
        return Equals(obj as Column);
    }

    public override int GetHashCode()
    {
        var hashCode = -1716540554;
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ColumnName);
        hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(ColumnType);
        hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(SourceReadType);
        hashCode = hashCode * -1521134295 + (EnumType?.GetHashCode() ?? 0);
        hashCode = hashCode * -1521134295 + ColumnIndex.GetHashCode();
        return hashCode;
    }
}
