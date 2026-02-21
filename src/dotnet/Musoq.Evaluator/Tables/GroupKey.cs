using System;
using System.Diagnostics;
using System.Text;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{ToString()}")]
public class GroupKey(params object[] values) : IEquatable<GroupKey>
{
    public readonly object[] Values = values;

    public bool Equals(GroupKey? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        if (Values.Length != other.Values.Length)
            return false;

        for (var i = 0; i < Values.Length; i++)
        {
            var thisValue = Values[i];
            var otherValue = other.Values[i];

            if (thisValue == null && otherValue == null)
                continue;

            if (thisValue == null || otherValue == null)
                return false;

            if (!thisValue.Equals(otherValue))
                return false;
        }

        return true;
    }

    public override string ToString()
    {
        var key = new StringBuilder();

        for (var i = 0; i < Values.Length - 1; i++)
        {
            var value = Values[i] == null ? "null" : Values[i].ToString();
            key.Append(value);
            key.Append(',');
        }

        var lastValue = Values[^1] == null ? "null" : Values[^1].ToString();
        key.Append(lastValue);

        return key.ToString();
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((GroupKey)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            for (var i = 0; i < Values.Length; ++i)
            {
                var val = Values[i];
                hash = hash * 31 + (val != null ? val.GetHashCode() : 0);
            }

            return hash;
        }
    }
}
