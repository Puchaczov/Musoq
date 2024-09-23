using System;

namespace Musoq.Evaluator.Tables;

public class TableIndex : IEquatable<TableIndex>
{
    public TableIndex(string name)
    {
        ColumnName = name;
    }

    public string ColumnName { get; }

    public bool Equals(TableIndex other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(ColumnName, other.ColumnName);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TableIndex) obj);
    }

    public override int GetHashCode()
    {
        return ColumnName != null ? ColumnName.GetHashCode() : 0;
    }
}