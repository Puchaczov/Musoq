using System.Collections.Generic;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator;

/// <summary>An execution's immutable parameter slots with typed value access.</summary>
public sealed class TypedParameterSnapshot
{
    private readonly IReadOnlyDictionary<string, TypedParameterSlot> _values;
    /// <summary>Gets the shared empty snapshot.</summary>
    public static TypedParameterSnapshot Empty { get; } = new(new Dictionary<string, TypedParameterSlot>());

    internal TypedParameterSnapshot(IReadOnlyDictionary<string, TypedParameterSlot> values) => _values = values;

    /// <summary>Gets the supplied names for declaration validation.</summary>
    public IEnumerable<string> Names => _values.Keys;
    /// <summary>Gets the supplied slot count.</summary>
    public int Count => _values.Count;

    internal void CopyTo(Dictionary<string, TypedParameterSlot> destination)
    {
        foreach (var entry in _values) destination.Add(entry.Key, entry.Value);
    }

    /// <summary>Prevents legacy capture from silently bypassing mixed parameter validation during migration.</summary>
    public void ValidateExclusive(IReadOnlyDictionary<string, object?> legacyParameters)
    {
        if (Count != 0 && legacyParameters.Count != 0)
            throw new InvalidOperationException("Use TypedParameters for every supplied parameter in this execution; mixed typed and legacy parameter values are unsupported.");
    }

    /// <summary>Reports whether an explicitly supplied value is present.</summary>
    public bool Contains(string name) => _values.ContainsKey(name);

    /// <summary>Reads a scalar without boxing or reflection invocation.</summary>
    public T GetRequired<T>(string name)
    {
        var slot = RequireSlot(name);
        if (slot is ScalarParameterSlot<T> scalar)
            return scalar.Value;
        if (slot.IsNull && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) is null)
            throw ScriptParameterBindingException.NullNotAllowed(name, typeof(T));
        throw TypeMismatch(name, typeof(T), slot.ValueType);
    }

    /// <summary>Reads an exact nullable scalar or lifts its concrete underlying scalar without boxing.</summary>
    public T? GetRequiredNullable<T>(string name) where T : struct
    {
        var slot = RequireSlot(name);
        if (slot is ScalarParameterSlot<T?> nullable)
            return nullable.Value;
        if (slot is ScalarParameterSlot<T> scalar)
            return scalar.Value;
        throw TypeMismatch(name, typeof(T?), slot.ValueType);
    }

    /// <summary>Reads an owned typed collection.</summary>
    public IReadOnlyList<T> GetRequiredCollection<T>(string name)
    {
        var slot = RequireSlot(name);
        if (slot is CollectionParameterSlot<T> collection)
        {
            if (collection.Values is null)
                throw ScriptParameterBindingException.NullNotAllowed(name, typeof(IReadOnlyList<T>));
            return collection.Values;
        }
        throw TypeMismatch(name, typeof(IReadOnlyList<T>), slot.ValueType);
    }

    private TypedParameterSlot RequireSlot(string name)
    {
        if (!_values.TryGetValue(name, out var slot))
            throw ScriptParameterBindingException.MissingRequired(name);
        return slot;
    }

    private static ScriptParameterBindingException TypeMismatch(string name, Type expected, Type actual)
        => ScriptParameterBindingException.TypeMismatch(name, expected, actual);
}
