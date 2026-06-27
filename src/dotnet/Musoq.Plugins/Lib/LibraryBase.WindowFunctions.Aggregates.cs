namespace Musoq.Plugins;

public partial class LibraryBase
{
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
}
