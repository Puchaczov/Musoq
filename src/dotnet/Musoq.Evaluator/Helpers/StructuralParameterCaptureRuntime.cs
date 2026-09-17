using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Musoq.Evaluator.Visitors;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator.Helpers;

/// <summary>Execution-owned state used while a generated parameter snapshot is built.</summary>
public sealed class StructuralParameterCaptureState
{
    private readonly HashSet<object> _ancestors = new(ReferenceEqualityComparer.Instance);
    private readonly StructuralInputLimits _limits;
    private long _nodes;
    private long _stringBytes;
    private int _maxDepth;

    /// <summary>Creates a capture state using the global structural input limits.</summary>
    public StructuralParameterCaptureState(
        CancellationToken cancellationToken,
        StructuralInputLimits? limits = null)
    {
        CancellationToken = cancellationToken;
        _limits = limits ?? StructuralInputLimits.Default;
    }

    /// <summary>Gets the cancellation token used by this capture.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Gets the measured usage accumulated by this capture state.</summary>
    public StructuralInputMetrics Metrics => new(_maxDepth, _nodes, _stringBytes);

    /// <summary>Reserves one logical value node at the supplied one-based depth.</summary>
    public void ReserveNode(int depth)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (depth <= 0 || depth > _limits.MaxDepth)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum depth {_limits.MaxDepth}.");

        _nodes = checked(_nodes + 1);
        if (_nodes > _limits.MaxNodes)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum value nodes {_limits.MaxNodes}.");
        if (depth > _maxDepth)
            _maxDepth = depth;
    }

    /// <summary>Reserves a precomputed constant subtree before its carrier is materialized.</summary>
    public void ReserveMetrics(StructuralInputMetrics metrics, int rootDepth)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (rootDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(rootDepth));
        var maxDepth = checked(rootDepth + Math.Max(0, metrics.MaxDepth) - 1);
        if (maxDepth > _limits.MaxDepth)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum depth {_limits.MaxDepth}.");
        if (metrics.NodeCount < 0 || metrics.StringBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(metrics));
        _nodes = checked(_nodes + metrics.NodeCount);
        _stringBytes = checked(_stringBytes + metrics.StringBytes);
        if (_nodes > _limits.MaxNodes)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum value nodes {_limits.MaxNodes}.");
        if (_stringBytes > _limits.MaxStringBytes)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum string payload {_limits.MaxStringBytes} bytes.");
        if (maxDepth > _maxDepth)
            _maxDepth = maxDepth;
    }

    /// <summary>Checks a known collection lower bound before allocating its result storage.</summary>
    public void EnsureCollectionLowerBound(int count)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (count < 0)
            throw new InvalidOperationException("Structural collection count cannot be negative.");
        if (count > _limits.MaxNodes - _nodes)
            throw new InvalidOperationException("Structural input exceeds maximum value nodes.");
    }

    /// <summary>Charges a string's UTF-16 payload against the global limit.</summary>
    public void ReserveString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        CancellationToken.ThrowIfCancellationRequested();

        try
        {
            _stringBytes = checked(_stringBytes + checked((long)value.Length * sizeof(char)));
        }
        catch (OverflowException exception)
        {
            throw new InvalidOperationException(
                "Structural input string payload overflowed the configured limit.",
                exception);
        }

        if (_stringBytes > _limits.MaxStringBytes)
            throw new InvalidOperationException(
                $"Structural input exceeds maximum string payload {_limits.MaxStringBytes} bytes.");
    }

    /// <summary>Enters a reference for active-ancestor cycle detection.</summary>
    public void Enter(object value, string path)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!_ancestors.Add(value))
            throw new InvalidOperationException(
                $"Structural value '{path}' contains a cyclic host reference.");
    }

    /// <summary>Leaves a reference after its children have been captured.</summary>
    public void Exit(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _ancestors.Remove(value);
    }
}

/// <summary>Allocation-conscious host readers used only during parameter preflight.</summary>
public static class StructuralParameterCaptureRuntime
{
    /// <summary>Reserves a scalar that was already read from an exact typed host collection.</summary>
    public static T ReadTypedScalar<T>(
        T value,
        StructuralParameterCaptureState state,
        int depth)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.ReserveNode(depth);
        return value;
    }

    /// <summary>Reserves a string scalar without routing it through an object slot.</summary>
    public static string? ReadTypedString(
        string? value,
        StructuralParameterCaptureState state,
        int depth)
    {
        ArgumentNullException.ThrowIfNull(state);
        state.ReserveNode(depth);
        if (value != null)
            state.ReserveString(value);
        return value;
    }

    /// <summary>Converts one host scalar using the existing Core conversion policy.</summary>
    public static T ReadScalar<T>(
        object? rawValue,
        string path,
        bool nullable,
        StructuralParameterCaptureState state,
        int depth)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(state);
        state.ReserveNode(depth);

        var scalar = rawValue is StructuralValue { Kind: StructuralTypeKind.Scalar } tree
            ? tree.Scalar
            : rawValue;
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        var conversion = ScriptValueConverter.ConvertValue(
            "Script parameter",
            path,
            targetType.Name,
            targetType,
            scalar);
        if (!conversion.Success)
            throw new InvalidOperationException(conversion.Error);

        if (conversion.Value is string text)
            state.ReserveString(text);

        if (conversion.Value == null)
            return default!;

        if (conversion.Value is T typed)
            return typed;

        // Nullable<T> is boxed as its underlying T. The runtime unbox operation
        // performs the required nullable wrapping without reflection.
        return (T)conversion.Value;
    }

    /// <summary>Gets a collection count through exact typed or covariant indexed access.</summary>
    public static int GetCollectionCount<T>(object rawValue, string path)
    {
        ArgumentNullException.ThrowIfNull(rawValue);
        ArgumentNullException.ThrowIfNull(path);

        if (rawValue is StructuralValue { Kind: StructuralTypeKind.Collection } structural)
            return structural.Elements.Count;

        // Some framework iterators expose IList/IReadOnlyList for optimization
        // while remaining lazy (for example Enumerable.Repeat).  They are not
        // stable host snapshots and must be rejected before indexed access.
        if (rawValue is IEnumerator)
            throw new InvalidOperationException(
                $"Structural collection '{path}' is a lazy-only enumerable and is not supported.");

        if (rawValue is T[] typedArray)
            return typedArray.Length;

        if (rawValue is IReadOnlyList<T> typedList)
            return ValidateCount(typedList.Count, path);

        // IReadOnlyList<T> is covariant for reference elements. This branch
        // covers record dictionaries and nested reference collections without
        // discovering an indexer or boxing each element.
        if (rawValue is IReadOnlyList<object?> referenceList)
            return ValidateCount(referenceList.Count, path);

        if (rawValue is Array array && array.Rank != 1)
            throw new InvalidOperationException(
                $"Structural collection '{path}' must be one-dimensional.");

        throw new InvalidOperationException(
            $"Structural collection '{path}' does not expose the expected indexed element contract '{typeof(T).Name}'.");
    }

    /// <summary>Validates field names and duplicate logical keys for one host record.</summary>
    public static void ValidateRecord(
        object rawValue,
        IReadOnlyList<string> expectedNames,
        string path)
    {
        ArgumentNullException.ThrowIfNull(rawValue);
        ArgumentNullException.ThrowIfNull(expectedNames);
        ArgumentNullException.ThrowIfNull(path);

        var lookup = GetRecordNameLookup(expectedNames);
        var words = Math.Max(1, (expectedNames.Count + 63) / 64);
        Span<ulong> seen = stackalloc ulong[words];

        if (rawValue is StructuralValue { Kind: StructuralTypeKind.Record } structural)
        {
            foreach (var field in structural.Fields)
                ValidateRecordField(field.Key, lookup, ref seen, path);
            return;
        }

        if (rawValue is IReadOnlyDictionary<string, object?> readOnlyDictionary)
        {
            foreach (var field in readOnlyDictionary)
                ValidateRecordField(field.Key, lookup, ref seen, path);
            return;
        }

        if (rawValue is IDictionary<string, object?> dictionary)
        {
            foreach (var field in dictionary)
                ValidateRecordField(field.Key, lookup, ref seen, path);
            return;
        }

        if (rawValue is IDictionary nonGenericDictionary)
        {
            foreach (DictionaryEntry entry in nonGenericDictionary)
            {
                if (entry.Key is not string key)
                    throw new InvalidOperationException(
                        $"Structural record '{path}' requires string dictionary keys.");
                ValidateRecordField(key, lookup, ref seen, path);
            }
            return;
        }

        throw new InvalidOperationException(
            $"Structural value '{path}' must be a Core record or a string-keyed dictionary.");
    }

    private static void ValidateRecordField(
        string name,
        RecordNameLookup lookup,
        ref Span<ulong> seen,
        string path)
    {
        if (!lookup.TryGetValue(name, out var index))
            throw new InvalidOperationException(
                $"Structural value '{path}' contains unexpected field '{name}'.");

        var word = index / 64;
        var bit = 1UL << (index % 64);
        if ((seen[word] & bit) != 0)
            throw new InvalidOperationException(
                $"Structural value '{path}' contains duplicate field '{name}'.");
        seen[word] |= bit;
    }

    /// <summary>Finds one field while preserving the distinction between absent and null.</summary>
    public static bool TryGetRecordField(
        object rawValue,
        string name,
        out object? value)
    {
        ArgumentNullException.ThrowIfNull(rawValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (rawValue is StructuralValue { Kind: StructuralTypeKind.Record } record)
        {
            if (record.Fields.TryGetValue(name, out var structuralValue))
            {
                value = structuralValue;
                return true;
            }

            value = null;
            return false;
        }

        if (rawValue is IReadOnlyDictionary<string, object?> readOnlyDictionary)
        {
            if (TryGetDictionaryValue(readOnlyDictionary, name, out value))
                return true;
            value = null;
            return false;
        }

        if (rawValue is IDictionary<string, object?> dictionary)
        {
            if (TryGetDictionaryValue(dictionary, name, out value))
                return true;
            value = null;
            return false;
        }

        if (rawValue is IDictionary nonGenericDictionary)
        {
            if (nonGenericDictionary.Contains(name))
            {
                value = nonGenericDictionary[name];
                return true;
            }

            foreach (DictionaryEntry entry in nonGenericDictionary)
            {
                if (entry.Key is string key &&
                    string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        throw new InvalidOperationException(
            "Structural value must be a Core record or a string-keyed dictionary.");
    }

    private static bool TryGetDictionaryValue(
        IReadOnlyDictionary<string, object?> dictionary,
        string name,
        out object? value)
    {
        if (dictionary.TryGetValue(name, out value))
            return true;

        foreach (var pair in dictionary)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetDictionaryValue(
        IDictionary<string, object?> dictionary,
        string name,
        out object? value)
    {
        if (dictionary.TryGetValue(name, out value))
            return true;

        foreach (var pair in dictionary)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static int ValidateCount(int count, string path)
    {
        if (count < 0)
            throw new InvalidOperationException($"Structural collection '{path}' reported a negative count.");
        return count;
    }

    private static readonly ConditionalWeakTable<IReadOnlyList<string>, RecordNameLookup> RecordNameLookups = new();

    private static RecordNameLookup GetRecordNameLookup(IReadOnlyList<string> names) =>
        RecordNameLookups.GetValue(names, static values => new RecordNameLookup(values));

    private sealed class RecordNameLookup
    {
        private readonly IReadOnlyDictionary<string, int> _indexes;

        public RecordNameLookup(IReadOnlyList<string> names)
        {
            var indexes = new Dictionary<string, int>(names.Count, StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < names.Count; index++)
                indexes.TryAdd(names[index], index);
            _indexes = indexes;
        }

        public bool TryGetValue(string name, out int index) => _indexes.TryGetValue(name, out index);
    }

}
