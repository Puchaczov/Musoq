using System.Collections.Generic;
using System.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{ColumnIndex}. {ColumnName}: {ColumnType.Name}")]
public class Column(string name, Type columnType, int columnOrder) : IEquatable<Column>, ISchemaColumn
{
    public bool Equals(Column? other)
    {
        return other != null &&
               ColumnName == other.ColumnName &&
               EqualityComparer<Type>.Default.Equals(ColumnType, other.ColumnType) &&
               ColumnIndex == other.ColumnIndex;
    }

    public string ColumnName { get; } = name;

    public Type ColumnType { get; } = columnType;

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
        hashCode = hashCode * -1521134295 + ColumnIndex.GetHashCode();
        return hashCode;
    }
}
