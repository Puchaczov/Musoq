using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    private readonly struct AscendingPartitionSortKey<T>(T value) : IComparable<AscendingPartitionSortKey<T>>
        where T : IComparable<T>
    {
        private readonly T _value = value;

        public int CompareTo(AscendingPartitionSortKey<T> other)
        {
            if (_value == null)
                return other._value == null ? 0 : -1;

            return other._value == null
                ? 1
                : _value.CompareTo(other._value);
        }
    }

    private readonly struct DescendingPartitionSortKey<T>(T value) : IComparable<DescendingPartitionSortKey<T>>
        where T : IComparable<T>
    {
        private readonly T _value = value;

        public int CompareTo(DescendingPartitionSortKey<T> other)
        {
            if (_value == null)
                return other._value == null ? 0 : 1;

            return other._value == null
                ? -1
                : other._value.CompareTo(_value);
        }
    }

    private sealed class BoxedTypedPartitionSetComparer<T>(object[] orderKeys, bool descending) : IComparer<int>
        where T : IComparable<T>
    {
        public int Compare(int x, int y)
        {
            var left = orderKeys[x];
            var right = orderKeys[y];

            if (left == null)
                return right == null ? 0 : (descending ? 1 : -1);

            if (right == null)
                return descending ? -1 : 1;

            var cmp = ((T)left).CompareTo((T)right);
            return descending ? -cmp : cmp;
        }
    }

    private sealed class GenericObjectPartitionSetComparer(object[] orderKeys, bool descending) : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return CompareValues(orderKeys[x], orderKeys[y], descending);
        }
    }

    private sealed class CompositePartitionSetComparer(object[] orderKeys, bool[] descendingFlags) : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return ((CompositeKeyValue)orderKeys[x]).CompareTo(
                (CompositeKeyValue)orderKeys[y],
                descendingFlags);
        }
    }
}
