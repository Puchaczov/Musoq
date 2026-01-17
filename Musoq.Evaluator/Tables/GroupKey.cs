using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Musoq.Evaluator.Tables;

[DebuggerDisplay("{ToString()}")]
public class GroupKey(params object[] values)
{
    public readonly object[] Values = values;

    public bool Equals(GroupKey other)
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

        string value;
        for (var i = 0; i < Values.Length - 1; i++)
        {
            value = Values[i] == null ? "null" : Values[i].ToString();
            key.Append($"{value},");
        }

        value = Values[^1] == null ? "null" : Values[^1].ToString();
        key.Append(value);

        return key.ToString();
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals(Values, ((GroupKey) obj).Values);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 0;
            for (var i = 0; i < Values.Length; ++i)
            {
                var val = Values[i];

                if(val == null)
                    continue;

                hash += val.GetHashCode();
            }

            return hash;
        }
    }

    private static bool Equals<T>(IReadOnlyList<T> first, IReadOnlyList<T> second)
    {
        if (first.Count != second.Count)
            return false;

        var areEqual = true;

        for (var i = 0; i < first.Count && areEqual; i++)
        {
            var f = first[i];
            var s = second[i];

            if (f == null && s == null)
                continue;

            if (f != null && s == null)
            {
                areEqual = false;
                continue;
            }

            if (f == null)
            {
                areEqual = false;
                continue;
            }

            areEqual &= f.Equals(s);
        }

        return areEqual;
    }
}