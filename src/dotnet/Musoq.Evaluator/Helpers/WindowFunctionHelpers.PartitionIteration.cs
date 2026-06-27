using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public static partial class WindowFunctionHelpers
{
    private readonly struct WindowPartition
    {
        private readonly List<int>? _list;
        private readonly int[]? _indices;
        private readonly int _start;

        public WindowPartition(List<int> list)
        {
            _list = list;
            _indices = null;
            _start = 0;
            Count = list.Count;
        }

        public WindowPartition(int[] indices, int start, int count)
        {
            _list = null;
            _indices = indices;
            _start = start;
            Count = count;
        }

        public int Count { get; }

        public int this[int index] => _list != null
            ? _list[index]
            : _indices?[_start + index] ?? throw new InvalidOperationException("Partition indices are not initialized.");
    }

    private readonly struct WindowPartitionEnumerable
    {
        private readonly List<List<int>>? _lists;
        private readonly WindowPartitionSet? _set;

        public WindowPartitionEnumerable(List<List<int>> lists)
        {
            _lists = lists;
            _set = null;
        }

        public WindowPartitionEnumerable(WindowPartitionSet set)
        {
            _lists = null;
            _set = set;
        }

        public WindowPartitionEnumerator GetEnumerator()
        {
            return new WindowPartitionEnumerator(_lists, _set);
        }
    }

    private struct WindowPartitionEnumerator(List<List<int>>? lists, WindowPartitionSet? set)
    {
        private int _index = -1;

        public WindowPartition Current { get; private set; } = default;

        public bool MoveNext()
        {
            _index++;

            if (lists != null)
            {
                if (_index >= lists.Count)
                    return false;

                Current = new WindowPartition(lists[_index]);
                return true;
            }

            if (set == null || _index >= set.PartitionCount)
                return false;

            Current = new WindowPartition(set.Indices, set.GetStart(_index), set.GetLength(_index));
            return true;
        }
    }

    private static WindowPartitionEnumerable EnumeratePartitions(List<List<int>> partitions)
    {
        return new WindowPartitionEnumerable(partitions);
    }

    private static WindowPartitionEnumerable EnumeratePartitions(WindowPartitionSet partitions)
    {
        return new WindowPartitionEnumerable(partitions);
    }
}
