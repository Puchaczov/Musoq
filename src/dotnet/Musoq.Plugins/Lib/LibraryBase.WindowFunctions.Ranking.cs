namespace Musoq.Plugins;

public partial class LibraryBase
{
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
                _buckets = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

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
}
