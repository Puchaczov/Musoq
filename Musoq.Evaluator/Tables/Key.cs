using System;
using System.Diagnostics;
using System.Text;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{ToString()}")]
public class Key : IEquatable<Key>
{
    public readonly int[] Columns;
    public readonly object[] Values;

    public Key(object[] values, int[] columns)
    {
        Values = values;
        Columns = columns;
    }

    public bool Equals(Key other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        var equals = Equals(Columns, other.Columns);

        for (var i = 0; i < Columns.Length && equals; i++) equals &= Values[i].Equals(other.Values[i]);

        return equals;
    }

    public override string ToString()
    {
        var key = new StringBuilder();

        for (var i = 0; i < Columns.Length - 1; i++)
            key.Append($"{Columns[i]}({Values[i]}), ");

        key.Append($"{Columns[Columns.Length - 1]}({Values[Values.Length - 1]})");

        return key.ToString();
    }

    public bool DoesRowMatchKey(Row row)
    {
        return row.CheckWithKey(this);
    }

    public override bool Equals(object obj)
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
                hash += Values[i].GetHashCode();
            }

            return hash;
        }
    }

    private static bool Equals<T>(T[] first, T[] second)
    {
        if (first.Length != second.Length)
            return false;

        var areEqual = true;

        for (var i = 0; i < first.Length; i++) areEqual &= first[i].Equals(second[i]);

        return areEqual;
    }
}