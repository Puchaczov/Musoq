using System;

namespace Musoq.Evaluator.Tables;

public class ObjectsRow : Row
{
    private readonly bool _hasLazyContexts;

    // For lazy context resolution - only materialize when accessed
    private readonly object[] _leftContexts;
    private readonly object[] _rightContexts;
    private readonly object[] _values;
    private object[] _cachedContexts;

    public ObjectsRow(object[] values)
    {
        _values = values;
    }

    public ObjectsRow(object[] values, object[] contexts)
    {
        _values = values;
        _cachedContexts = contexts;
    }

    public ObjectsRow(object[] values, object[] leftContexts, object[] rightContexts)
    {
        if (leftContexts == null && rightContexts == null)
            throw new NotSupportedException("Both contexts cannot be null");

        _values = values;

        // Store references for lazy evaluation instead of copying immediately
        _leftContexts = leftContexts;
        _rightContexts = rightContexts;
        _hasLazyContexts = true;
    }

    public override object this[int columnNumber] => _values[columnNumber];

    public override int Count => _values.Length;

    public override object[] Values => _values;

    public override object[] Contexts
    {
        get
        {
            if (!_hasLazyContexts)
                return _cachedContexts;

            // Lazy materialization - only allocate when actually accessed
            if (_cachedContexts != null)
                return _cachedContexts;

            // Materialize the concatenated context array
            if (_leftContexts == null)
            {
                var result = new object[1 + _rightContexts.Length];
                result[0] = null;
                Array.Copy(_rightContexts, 0, result, 1, _rightContexts.Length);
                _cachedContexts = result;
            }
            else if (_rightContexts == null)
            {
                var result = new object[_leftContexts.Length + 1];
                Array.Copy(_leftContexts, 0, result, 0, _leftContexts.Length);
                result[_leftContexts.Length] = null;
                _cachedContexts = result;
            }
            else
            {
                var result = new object[_leftContexts.Length + _rightContexts.Length];
                Array.Copy(_leftContexts, 0, result, 0, _leftContexts.Length);
                Array.Copy(_rightContexts, 0, result, _leftContexts.Length, _rightContexts.Length);
                _cachedContexts = result;
            }

            return _cachedContexts;
        }
    }
}
