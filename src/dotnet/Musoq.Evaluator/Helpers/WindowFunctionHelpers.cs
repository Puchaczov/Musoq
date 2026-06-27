using System.Runtime.CompilerServices;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? CompositeKey(params object?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        return parts.Length switch
        {
            0 => 0,
            1 => parts[0],
            _ => new CompositeKeyValue(parts)
        };
    }

    internal sealed class CompositeKeyValue : IEquatable<CompositeKeyValue>, IComparable<CompositeKeyValue>, IComparable
    {
        private readonly object?[] _parts;
        private readonly int _hashCode;

        public CompositeKeyValue(object?[] parts)
        {
            _parts = parts;
            var hash = new HashCode();
            foreach (var part in parts)
                hash.Add(part);
            _hashCode = hash.ToHashCode();
        }

        public bool Equals(CompositeKeyValue? other)
        {
            if (other == null || _parts.Length != other._parts.Length)
                return false;

            if (_hashCode != other._hashCode)
                return false;

            for (var i = 0; i < _parts.Length; i++)
            {
                if (!Equals(_parts[i], other._parts[i]))
                    return false;
            }

            return true;
        }

        public int CompareTo(CompositeKeyValue? other)
        {
            return CompareTo(other, []);
        }

        public int CompareTo(CompositeKeyValue? other, bool[] descendingFlags)
        {
            if (other == null)
                return 1;

            var len = Math.Min(_parts.Length, other._parts.Length);
            for (var i = 0; i < len; i++)
            {
                var descending = i < descendingFlags.Length && descendingFlags[i];
                var ca = _parts[i] as IComparable;
                var cb = other._parts[i] as IComparable;

                if (ca == null && cb == null) continue;
                if (ca == null) return descending ? 1 : -1;
                if (cb == null) return descending ? -1 : 1;

                var cmp = ca.CompareTo(cb);
                if (cmp != 0)
                    return descending ? -cmp : cmp;
            }

            return 0;
        }

        int IComparable.CompareTo(object? obj)
        {
            if (obj is CompositeKeyValue other)
                return CompareTo(other);
            return 1;
        }

        public override bool Equals(object? obj) => obj is CompositeKeyValue other && Equals(other);

        public override int GetHashCode() => _hashCode;
    }
}
