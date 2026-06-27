using System.Diagnostics;
using System.Text;
using Musoq.Schema;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{DebugInfo()}")]
public abstract class Row : IEquatable<Row>, IValue<Key>, IReadOnlyRow
{
    public abstract int Count { get; }

    public virtual object[] Values
    {
        get
        {
            if (Count == 0)
                return Array.Empty<object>();

            var values = new object[Count];
            for (var index = 0; index < values.Length; index++)
                values[index] = this[index];

            return values;
        }
    }

    /// <summary>
    /// Source objects retained for operators that need source-row context. Final query output rows may return null when no
    /// downstream semantics require those contexts.
    /// </summary>
    public virtual object?[]? Contexts => null;

    public virtual object this[string name] =>
        throw new NotSupportedException("String-keyed access is not supported on Row. Use integer indexing instead.");

    public virtual bool HasColumn(string name) => false;

    public virtual void AssignValue(int columnNumber, object value)
    {
        throw new NotSupportedException("Column value assignment is not supported on this Row.");
    }

    public bool Equals(Row? other)
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
        ArgumentNullException.ThrowIfNull(key);
        return key.DoesRowMatchKey(this);
    }

    public override bool Equals(object? obj)
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
        ArgumentNullException.ThrowIfNull(key);
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
        if (Count == 0)
            return string.Empty;

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
