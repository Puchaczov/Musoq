using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{ToString()}")]
public class Key(object?[] values, int[] columns) : IEquatable<Key>
{
    public readonly int[] Columns = columns;
    public readonly object?[] Values = values;

    public bool Equals(Key? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        var equals = Equals(Columns, other.Columns);

        for (var i = 0; i < Columns.Length && equals; i++)
        {
            var thisValue = Values[i];
            var otherValue = other.Values[i];

            if (thisValue == null && otherValue == null)
                continue;

            if (thisValue == null || otherValue == null)
            {
                equals = false;
                break;
            }

            equals &= thisValue.Equals(otherValue);
        }

        return equals;
    }

    public override string ToString()
    {
        var key = new StringBuilder();

        for (var i = 0; i < Columns.Length - 1; i++)
        {
            key.Append(Columns[i]);
            key.Append('(');
            key.Append(Values[i]);
            key.Append("), ");
        }

        key.Append(Columns[Columns.Length - 1]);
        key.Append('(');
        key.Append(Values[Values.Length - 1]);
        key.Append(')');

        return key.ToString();
    }

    public bool DoesRowMatchKey(Row row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return row.CheckWithKey(this);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Key)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 0;
            for (var i = 0; i < Columns.Length; ++i)
            {
                hash += Columns[i].GetHashCode();
                hash += Values[i]?.GetHashCode() ?? 0;
            }

            return hash;
        }
    }

    private static bool Equals<T>(T[] first, T[] second)
    {
        if (first.Length != second.Length)
            return false;

        var areEqual = true;

        for (var i = 0; i < first.Length; i++) areEqual &= EqualityComparer<T>.Default.Equals(first[i], second[i]);

        return areEqual;
    }
}
