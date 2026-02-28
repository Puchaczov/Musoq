using System;
using System.Diagnostics;
using System.Text;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{ToString()}")]
public class GroupKey : IEquatable<GroupKey>
{
    public readonly object[] Values;
    private readonly int _cachedHashCode;

    public GroupKey(params object[] values)
    {
        Values = values;
        _cachedHashCode = ComputeHash(values);
    }

    public bool Equals(GroupKey other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        if (_cachedHashCode != other._cachedHashCode)
            return false;

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

    public override bool Equals(object obj)
    {
        return obj is GroupKey other && Equals(other);
    }

    public override int GetHashCode() => _cachedHashCode;

    private static int ComputeHash(object[] values)
    {
        return values.Length switch
        {
            0 => 0,
            1 => values[0]?.GetHashCode() ?? 0,
            2 => HashCode.Combine(values[0], values[1]),
            3 => HashCode.Combine(values[0], values[1], values[2]),
            _ => ComputeFullHash(values)
        };
    }

    private static int ComputeFullHash(object[] values)
    {
        var hashCode = new HashCode();

        for (var i = 0; i < values.Length; ++i)
            hashCode.Add(values[i]);

        return hashCode.ToHashCode();
    }
}
