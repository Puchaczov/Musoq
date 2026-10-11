using System.Threading;
using System.Collections.Generic;
using System.Text.Json;
using Musoq.Schema;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator;

/// <summary>Stores parameter values in concrete generic slots.</summary>
public sealed class TypedParameterValues
{
    private readonly object _gate;
    private readonly Action? _beforeMutation;
    private readonly Dictionary<string, TypedParameterSlot> _values = new(StringComparer.Ordinal);

    public TypedParameterValues() : this(new object(), null) { }

    internal TypedParameterValues(object gate, Action? beforeMutation)
    {
        _gate = gate;
        _beforeMutation = beforeMutation;
    }

    /// <summary>Gets the supplied slot count.</summary>
    public int Count { get { lock (_gate) return _values.Count; } }

    /// <summary>Sets a scalar value using its exact declared CLR type.</summary>
    public void Set<T>(string name, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateScalarType<T>();

        lock (_gate)
        {
            _beforeMutation?.Invoke();
            _values[name] = new ScalarParameterSlot<T>(value);
        }
    }

    /// <summary>Sets a collection with concretely typed elements.</summary>
    public void SetCollection<T>(string name, IReadOnlyList<T>? values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateScalarType<T>();
        lock (_gate)
        {
            _beforeMutation?.Invoke();
            _values[name] = new CollectionParameterSlot<T>(values);
        }
    }

    /// <summary>Supplies structured JSON for direct decoding into the declared generated carrier.</summary>
    public void SetJson(string name, JsonElement value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        lock (_gate)
        {
            _beforeMutation?.Invoke();
            _values[name] = new ScalarParameterSlot<JsonElement>(value.Clone());
        }
    }

    /// <summary>Replaces the next execution's values with immutable captured slots.</summary>
    public void ReplaceWith(TypedParameterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_gate)
        {
            _beforeMutation?.Invoke();
            _values.Clear();
            snapshot.CopyTo(_values);
        }
    }

    /// <summary>Removes a supplied slot before the next execution.</summary>
    public void Remove(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        lock (_gate)
        {
            _beforeMutation?.Invoke();
            _values.Remove(name);
        }
    }

    /// <summary>Clears supplied slots before the next execution.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _beforeMutation?.Invoke();
            _values.Clear();
        }
    }

    private static void ValidateScalarType<T>()
    {
        if (ConcreteValueShape.FindUnresolvedMember(typeof(T)) is not null ||
            StructuralTypeDescriptor.FromClrType(typeof(T)).Kind != StructuralTypeKind.Scalar)
            throw new ArgumentException($"'{typeof(T)}' is not a concrete scalar parameter type. Use SetCollection or SetJson for structured inputs.");
    }

    /// <summary>Captures independent slots and collection buffers for one execution.</summary>
    public TypedParameterSnapshot Capture(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_values.Count == 0)
                return TypedParameterSnapshot.Empty;
            var values = new Dictionary<string, TypedParameterSlot>(_values.Count, StringComparer.Ordinal);
            foreach (var entry in _values)
                values.Add(entry.Key, entry.Value.Capture(cancellationToken));
            return new TypedParameterSnapshot(values);
        }
    }
}
