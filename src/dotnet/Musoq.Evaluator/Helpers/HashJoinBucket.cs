using System.Collections.Generic;

namespace Musoq.Evaluator.Helpers;

public struct HashJoinBucket<T>
{
    private T _first;
    private List<T>? _additional;
    private bool _hasFirst;

    public HashJoinBucket(T first)
    {
        _first = first;
        _additional = null;
        _hasFirst = true;
    }

    public void Add(T row)
    {
        if (!_hasFirst)
        {
            _first = row;
            _hasFirst = true;
            return;
        }

        _additional ??= new List<T>();
        _additional.Add(row);
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(_first, _additional, _hasFirst);
    }

    public struct Enumerator
    {
        private readonly T _first;
        private readonly List<T>? _additional;
        private readonly bool _hasFirst;
        private int _index = -1;

        public Enumerator(T first, List<T>? additional, bool hasFirst)
        {
            _first = first;
            _additional = additional;
            _hasFirst = hasFirst;
            Current = first;
        }

        public T Current { get; private set; }

        public bool MoveNext()
        {
            if (_index < 0)
            {
                if (!_hasFirst)
                    return false;

                Current = _first;
                _index = 0;
                return true;
            }

            if (_additional == null || _index >= _additional.Count)
                return false;

            Current = _additional[_index];
            _index++;
            return true;
        }
    }
}
