using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator;

internal sealed class SynchronizedParameterDictionary : IDictionary<string, object?>
{
    private readonly object _gate;
    private readonly Dictionary<string, object?> _values;
    private readonly IDictionary<string, object?>? _compatibilityTarget;
    private readonly Func<bool>? _suppressCompatibilitySynchronization;

    internal SynchronizedParameterDictionary(
        object gate,
        IEnumerable<KeyValuePair<string, object?>>? values = null,
        Func<bool>? suppressCompatibilitySynchronization = null)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _compatibilityTarget = values as IDictionary<string, object?>;
        _suppressCompatibilitySynchronization = suppressCompatibilitySynchronization;
        _values = values is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(values, StringComparer.Ordinal);
    }

    internal IReadOnlyDictionary<string, object?> Snapshot()
    {
        lock (_gate)
        {
            return ParameterSnapshot.CaptureReadOnlyOrEmpty(_values);
        }
    }

    public object? this[string key]
    {
        get
        {
            lock (_gate)
                return _values[key];
        }
        set
        {
            lock (_gate)
            {
                _values[key] = value;
                SynchronizeCompatibilityTargetIfAllowed();
            }
        }
    }

    public ICollection<string> Keys
    {
        get
        {
            lock (_gate)
                return _values.Keys.ToArray();
        }
    }

    public ICollection<object?> Values
    {
        get
        {
            lock (_gate)
                return _values.Values.ToArray();
        }
    }

    public int Count
    {
        get
        {
            lock (_gate)
                return _values.Count;
        }
    }

    public bool IsReadOnly => false;

    public void Add(string key, object? value)
    {
        lock (_gate)
        {
            _values.Add(key, value);
            SynchronizeCompatibilityTargetIfAllowed();
        }
    }

    public bool ContainsKey(string key)
    {
        lock (_gate)
            return _values.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        lock (_gate)
        {
            var removed = _values.Remove(key);
            if (removed)
                SynchronizeCompatibilityTargetIfAllowed();
            return removed;
        }
    }

    public bool TryGetValue(string key, out object? value)
    {
        lock (_gate)
            return _values.TryGetValue(key, out value);
    }

    public void Add(KeyValuePair<string, object?> item)
    {
        lock (_gate)
        {
            ((ICollection<KeyValuePair<string, object?>>)_values).Add(item);
            SynchronizeCompatibilityTargetIfAllowed();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _values.Clear();
            SynchronizeCompatibilityTargetIfAllowed();
        }
    }

    public bool Contains(KeyValuePair<string, object?> item)
    {
        lock (_gate)
            return ((ICollection<KeyValuePair<string, object?>>)_values).Contains(item);
    }

    public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        lock (_gate)
            ((ICollection<KeyValuePair<string, object?>>)_values).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<string, object?> item)
    {
        lock (_gate)
        {
            var removed = ((ICollection<KeyValuePair<string, object?>>)_values).Remove(item);
            if (removed)
                SynchronizeCompatibilityTargetIfAllowed();
            return removed;
        }
    }

    internal void SynchronizeCompatibilityTarget()
    {
        lock (_gate)
            SynchronizeCompatibilityTargetCore();
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Snapshot().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void SynchronizeCompatibilityTargetIfAllowed()
    {
        if (_suppressCompatibilitySynchronization?.Invoke() == true)
            return;

        SynchronizeCompatibilityTargetCore();
    }

    private void SynchronizeCompatibilityTargetCore()
    {
        if (_compatibilityTarget is null)
            return;

        _compatibilityTarget.Clear();
        foreach (var value in _values)
            _compatibilityTarget[value.Key] = value.Value;
    }
}
