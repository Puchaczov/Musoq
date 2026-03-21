using System;
using System.Collections.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Creates a window function that assigns a sequential number to each row within a partition.
    /// </summary>
    [WindowFunction(Name = "RowNumber")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, long> WindowRowNumber()
    {
        return new RowNumberWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that assigns a rank to each row, with gaps for ties.
    ///     Receives the ORDER BY key via accumulate to detect ties.
    /// </summary>
    [WindowFunction(Name = "Rank")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, long> WindowRank()
    {
        return new RankWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that assigns a dense rank to each row, without gaps for ties.
    ///     Receives the ORDER BY key via accumulate to detect ties.
    /// </summary>
    [WindowFunction(Name = "DenseRank")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, long> WindowDenseRank()
    {
        return new DenseRankWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that distributes rows into a specified number of roughly equal groups.
    ///     Receives the bucket count via accumulate (from the SQL argument).
    /// </summary>
    [WindowFunction(Name = "Ntile")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, long> WindowNtile()
    {
        return new NtileWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition sum.
    /// </summary>
    [WindowFunction(Name = "Sum")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, decimal> WindowSum()
    {
        return new SumWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition count of non-null values.
    /// </summary>
    [WindowFunction(Name = "Count")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, int> WindowCount()
    {
        return new CountWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition average.
    /// </summary>
    [WindowFunction(Name = "Avg")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, decimal> WindowAvg()
    {
        return new AvgWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition minimum.
    /// </summary>
    [WindowFunction(Name = "Min")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowMin()
    {
        return new MinWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that computes a running or whole-partition maximum.
    /// </summary>
    [WindowFunction(Name = "Max")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowMax()
    {
        return new MaxWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that returns the first value in each partition.
    /// </summary>
    [WindowFunction(Name = "FirstValue")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowFirstValue()
    {
        return new FirstValueWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that returns the last value seen so far (running)
    ///     or the last value in the partition (unordered).
    /// </summary>
    [WindowFunction(Name = "LastValue")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowLastValue()
    {
        return new LastValueWindowFunction();
    }

    /// <summary>
    ///     Creates a window function that returns the nth value in each partition.
    ///     The position argument is passed via <see cref="IWindowFunction.SetArguments"/>.
    /// </summary>
    [WindowFunction(Name = "NthValue")]
    [MethodCategory(MethodCategories.Window)]
    public IWindowFunction<object?, object?> WindowNthValue()
    {
        return new NthValueWindowFunction();
    }

    private sealed class SumWindowFunction : IWindowFunction<object?, decimal>
    {
        private decimal _sum;

        public void PartitionStart() => _sum = 0;

        public void Accumulate(object? value)
        {
            if (value is not null)
                _sum += ToDecimalFast(value);
        }

        public decimal GetValue() => _sum;
    }

    private sealed class CountWindowFunction : IWindowFunction<object?, int>
    {
        private int _count;

        public void PartitionStart() => _count = 0;

        public void Accumulate(object? value)
        {
            if (value is not null)
                _count++;
        }

        public int GetValue() => _count;
    }

    private sealed class AvgWindowFunction : IWindowFunction<object?, decimal>
    {
        private decimal _sum;
        private int _count;

        public void PartitionStart()
        {
            _sum = 0;
            _count = 0;
        }

        public void Accumulate(object? value)
        {
            if (value is not null)
            {
                _sum += ToDecimalFast(value);
                _count++;
            }
        }

        public decimal GetValue() => _count > 0 ? _sum / _count : 0m;
    }

    private sealed class MinWindowFunction : IWindowFunction<object?, object?>
    {
        private IComparable? _current;

        public void PartitionStart() => _current = null;

        public void Accumulate(object? value)
        {
            if (value is IComparable comparable)
            {
                if (_current is null || comparable.CompareTo(_current) < 0)
                    _current = comparable;
            }
        }

        public object? GetValue() => _current;
    }

    private sealed class MaxWindowFunction : IWindowFunction<object?, object?>
    {
        private IComparable? _current;

        public void PartitionStart() => _current = null;

        public void Accumulate(object? value)
        {
            if (value is IComparable comparable)
            {
                if (_current is null || comparable.CompareTo(_current) > 0)
                    _current = comparable;
            }
        }

        public object? GetValue() => _current;
    }

    private static decimal ToDecimalFast(object value)
    {
        return value switch
        {
            int i => i,
            long l => l,
            decimal d => d,
            double dbl => (decimal)dbl,
            float f => (decimal)f,
            short s => s,
            byte b => b,
            _ => Convert.ToDecimal(value)
        };
    }

    private sealed class RowNumberWindowFunction : IWindowFunction<object?, long>
    {
        private long _counter;

        public void PartitionStart() => _counter = 0;

        public void Accumulate(object? value) => _counter++;

        public long GetValue() => _counter;
    }

    private sealed class RankWindowFunction : IWindowFunction<object?, long>
    {
        private long _position;
        private long _rank;
        private object? _lastKey;
        private bool _isFirst;

        public void PartitionStart()
        {
            _position = 0;
            _rank = 0;
            _lastKey = null;
            _isFirst = true;
        }

        public void Accumulate(object? value)
        {
            _position++;

            if (_isFirst || !EqualKeys(_lastKey, value))
            {
                _rank = _position;
                _lastKey = value;
                _isFirst = false;
            }
        }

        public long GetValue() => _rank;
    }

    private sealed class DenseRankWindowFunction : IWindowFunction<object?, long>
    {
        private long _rank;
        private object? _lastKey;
        private bool _isFirst;

        public void PartitionStart()
        {
            _rank = 0;
            _lastKey = null;
            _isFirst = true;
        }

        public void Accumulate(object? value)
        {
            if (_isFirst || !EqualKeys(_lastKey, value))
            {
                _rank++;
                _lastKey = value;
                _isFirst = false;
            }
        }

        public long GetValue() => _rank;
    }

    private sealed class NtileWindowFunction : IWindowFunction<object?, long>
    {
        private int _partitionSize;
        private int _buckets;
        private int _position;

        public void SetPartitionSize(int size) => _partitionSize = size;

        public void PartitionStart()
        {
            _buckets = 0;
            _position = 0;
        }

        public void Accumulate(object? value)
        {
            if (_buckets == 0 && value is not null)
                _buckets = Convert.ToInt32(value);

            _position++;
        }

        public long GetValue()
        {
            if (_buckets <= 0)
                return 1;

            var rowsPerBucket = _partitionSize / _buckets;
            var extra = _partitionSize % _buckets;
            var largeGroupBoundary = extra * (rowsPerBucket + 1);

            if (_position <= largeGroupBoundary)
                return ((_position - 1) / (rowsPerBucket + 1)) + 1;

            return ((_position - 1 - largeGroupBoundary) / rowsPerBucket) + extra + 1;
        }
    }

    private sealed class FirstValueWindowFunction : IWindowFunction<object?, object?>
    {
        private object? _firstValue;
        private bool _hasValue;

        public void PartitionStart()
        {
            _firstValue = null;
            _hasValue = false;
        }

        public void Accumulate(object? value)
        {
            if (!_hasValue)
            {
                _firstValue = value;
                _hasValue = true;
            }
        }

        public object? GetValue() => _firstValue;
    }

    private sealed class LastValueWindowFunction : IWindowFunction<object?, object?>
    {
        private object? _lastValue;

        public void PartitionStart() => _lastValue = null;

        public void Accumulate(object? value) => _lastValue = value;

        public object? GetValue() => _lastValue;
    }

    private sealed class NthValueWindowFunction : IWindowFunction<object?, object?>
    {
        private int _n;
        private int _position;
        private object? _nthValue;

        public void SetArguments(object?[] args)
        {
            _n = Convert.ToInt32(args[0]);
        }

        public void PartitionStart()
        {
            _position = 0;
            _nthValue = null;
        }

        public void Accumulate(object? value)
        {
            _position++;

            if (_position == _n)
                _nthValue = value;
        }

        public object? GetValue() => _position >= _n ? _nthValue : null;
    }

    private static bool EqualKeys(object? a, object? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        if (a is IComparable ca)
            return ca.CompareTo(b) == 0;

        return Equals(a, b);
    }
}
