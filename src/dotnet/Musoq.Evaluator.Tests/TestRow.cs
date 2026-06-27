using System;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

public sealed class TestRow : Row
{
    private readonly LazyContextKind _lazyContextKind;
    private readonly object?[]? _leftContexts;
    private readonly object?[]? _rightContexts;
    private readonly object? _leftContext;
    private readonly object? _rightContext;
    private readonly object[] _values;
    private object?[]? _cachedContexts;

    public TestRow(object[] values)
    {
        _values = values;
    }

    public TestRow(object[] values, object?[]? contexts)
    {
        _values = values;
        _cachedContexts = contexts;
    }

    public TestRow(object[] values, object? context)
    {
        _values = values;
        _leftContext = context;
        _lazyContextKind = LazyContextKind.Single;
    }

    public TestRow(object[] values, object? leftContext, object? rightContext)
    {
        _values = values;
        _leftContext = leftContext;
        _rightContext = rightContext;
        _lazyContextKind = LazyContextKind.SinglePair;
    }

    public TestRow(object[] values, object?[]? leftContexts, object?[]? rightContexts)
    {
        if (leftContexts == null && rightContexts == null)
            throw new NotSupportedException("Both contexts cannot be null");

        _values = values;
        _leftContexts = leftContexts;
        _rightContexts = rightContexts;
        _lazyContextKind = LazyContextKind.ArrayPair;
    }

    public TestRow(object[] values, object?[]? leftContexts, object? rightContext)
    {
        _values = values;
        _leftContexts = leftContexts;
        _rightContext = rightContext;
        _lazyContextKind = LazyContextKind.ArrayAndSingle;
    }

    public TestRow(object[] values, object? leftContext, object?[]? rightContexts)
    {
        _values = values;
        _leftContext = leftContext;
        _rightContexts = rightContexts;
        _lazyContextKind = LazyContextKind.SingleAndArray;
    }

    public override object this[int columnNumber] => _values[columnNumber];

    public override object this[string name]
    {
        get
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (name.StartsWith("Value", StringComparison.Ordinal) &&
                int.TryParse(name.AsSpan("Value".Length), out var valueIndex) &&
                valueIndex >= 0 &&
                valueIndex < _values.Length)
            {
                return _values[valueIndex];
            }

            throw new NotSupportedException(
                $"String-keyed access for column '{name}' is not directly supported on TestRow. " +
                "Use integer indexing or the Value{n} fallback.");
        }
    }

    public override int Count => _values.Length;

    public override object[] Values => _values;

    public override object?[]? Contexts
    {
        get
        {
            if (_lazyContextKind == LazyContextKind.None)
                return _cachedContexts;

            if (_cachedContexts != null)
                return _cachedContexts;

            _cachedContexts = _lazyContextKind switch
            {
                LazyContextKind.Single => [_leftContext],
                LazyContextKind.SinglePair => [_leftContext, _rightContext],
                LazyContextKind.ArrayPair => MaterializeArrayPairContexts(),
                LazyContextKind.ArrayAndSingle => MaterializeArrayAndSingleContexts(),
                LazyContextKind.SingleAndArray => MaterializeSingleAndArrayContexts(),
                _ => _cachedContexts
            };

            return _cachedContexts;
        }
    }

    private object?[] MaterializeArrayPairContexts()
    {
        return ContextMaterializer.MergePreservingNullSegments(_leftContexts, _rightContexts);
    }

    private object?[] MaterializeArrayAndSingleContexts()
    {
        return ContextMaterializer.AppendPreservingNullSegment(_leftContexts, _rightContext);
    }

    private object?[] MaterializeSingleAndArrayContexts()
    {
        return ContextMaterializer.PrependPreservingNullSegment(_leftContext, _rightContexts);
    }

    private enum LazyContextKind
    {
        None,
        Single,
        SinglePair,
        ArrayPair,
        ArrayAndSingle,
        SingleAndArray
    }
}
