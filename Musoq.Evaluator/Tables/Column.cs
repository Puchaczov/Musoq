using System;
using System.Collections.Generic;
using System.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{ColumnIndex}. {ColumnName}: {ColumnType.Name}")]
public class Column : IEquatable<Column>, ISchemaColumn
{
    public Column(string name, Type columnType, int columnOrder)
    {
        ColumnName = name;
        ColumnType = columnType;
        ColumnIndex = columnOrder;
    }

    public bool Equals(Column other)
    {
        return other != null &&
               ColumnName == other.ColumnName &&
               EqualityComparer<Type>.Default.Equals(ColumnType, other.ColumnType) &&
               ColumnIndex == other.ColumnIndex;
    }

    public string ColumnName { get; }

    public Type ColumnType { get; }

    public int ColumnIndex { get; }

    public override bool Equals(object obj)
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
