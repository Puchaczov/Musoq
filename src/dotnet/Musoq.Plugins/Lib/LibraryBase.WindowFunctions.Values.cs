namespace Musoq.Plugins;

public partial class LibraryBase
{
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
            _n = Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture);
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
}
