using System;
using System.Diagnostics;
using System.Text;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{DebugInfo()}")]
public abstract class Row : IEquatable<Row>, IValue<Key>, IReadOnlyRow
{
    public abstract int Count { get; }

    public abstract object[] Values { get; }

    public virtual object[] Contexts => null;

    public bool Equals(Row other)
    {
        if (other == null)
            return false;

        if (other.Count != Count)
            return false;

        var isEqual = true;

        for (var i = 0; i < Count && isEqual; ++i)
        {
            var thisValue = this[i];
            var otherValue = other[i];

            if (thisValue == null && otherValue == null)
                continue;

            if (thisValue == null || otherValue == null)
            {
                isEqual = false;
                break;
            }

            isEqual &= thisValue.Equals(otherValue);
        }

        return isEqual;
    }

    public abstract object this[int columnNumber] { get; }

    public bool FitsTheIndex(Key key)
    {
        return key.DoesRowMatchKey(this);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Row);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        for (int i = 0, j = Count; i < j; ++i)
            hashCode.Add(this[i]);

        return hashCode.ToHashCode();
    }

    public bool CheckWithKey(Key key)
    {
        var isMatch = true;

        for (var i = 0; i < key.Columns.Length; i++)
        {
            var rowValue = this[key.Columns[i]];
            var keyValue = key.Values[i];

            if (rowValue == null && keyValue == null)
                continue;

            if (rowValue == null || keyValue == null)
            {
                isMatch = false;
                break;
            }

            isMatch &= rowValue.Equals(keyValue);
        }

        return isMatch;
    }

    internal string DebugInfo()
    {
        var rowText = new StringBuilder();

        for (var i = 0; i < Count - 1; i++)
        {
            rowText.Append(this[i]);
            rowText.Append(", ");
        }

        rowText.Append(this[Count - 1]);

        return rowText.ToString();
    }
}
