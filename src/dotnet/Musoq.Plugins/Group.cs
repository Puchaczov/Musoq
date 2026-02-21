using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Musoq.Plugins;

/// <summary>
///     Represents a group of rows.
/// </summary>
#if DEBUG
[DebuggerDisplay("{Name}")]
#endif
public sealed class Group
{
    /// <summary>
    ///     Creates a new group.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="fieldNames"></param>
    /// <param name="values"></param>
    public Group(Group? parent, string[] fieldNames, object[] values)
    {
        Parent = parent;
#if DEBUG
        Name = fieldNames.Length == 0 ? "root" : string.Join(",", fieldNames);
#endif
        for (var i = 0; i < fieldNames.Length; i++) Values.Add(fieldNames[i], values[i]);
    }
#if DEBUG
    private string Name { get; }
#endif

    // Concrete Dictionary (not IDictionary) so the JIT can devirtualize TryGetValue/Add/indexer calls.
    private Dictionary<string, object?> Values { get; } = new();

    private Dictionary<string, Func<object?, object?>> Converters { get; } = new();

    /// <summary>
    ///     Gets the parent group.
    /// </summary>
    public Group? Parent { get; }

    /// <summary>
    ///     Gets the number of hits.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    ///     Increments the number of hits.
    /// </summary>
    public void Hit()
    {
        Count += 1;
    }

    /// <summary>
    ///     Gets the value of the group.
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public T? GetValue<T>(string name)
    {
        if (!Values.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Group does not have value {name}.");

        if (Converters.TryGetValue(name, out var converter))
            return (T?)converter(value);

        return (T?)value;
    }

    /// <summary>
    ///     Gets the value of the group.
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public T? GetRawValue<T>(string name)
    {
        if (!Values.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Group does not have value {name}.");

        return (T?)value;
    }

    /// <summary>
    ///     Gets the value of the group or creates a new one.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="defValue"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetOrCreateValue<T>(string name, T? defValue = default)
    {
        if (!Values.TryGetValue(name, out var value))
        {
            Values.Add(name, defValue);
            return defValue;
        }

        return (T?)value;
    }

    /// <summary>
    ///     Gets the value of the group or creates a new one.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="createDefault"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetOrCreateValue<T>(string name, Func<T> createDefault)
    {
        if (!Values.TryGetValue(name, out var value))
        {
            var newValue = createDefault();
            Values.Add(name, newValue);
            return newValue;
        }

        return (T?)value;
    }

    /// <summary>
    ///     Sets the value of the group.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void SetValue<T>(string name, T? value)
    {
        Values[name] = value;
    }

    /// <summary>
    ///     Gets the value of the group or creates a new one.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="converter"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TR"></typeparam>
    /// <returns></returns>
    public TR? GetOrCreateValueWithConverter<T, TR>(string name, T value, Func<object?, object?> converter)
    {
        if (!Values.TryGetValue(name, out var existingValue))
        {
            existingValue = value;
            Values.Add(name, value);
        }

        if (!Converters.TryGetValue(name, out var existingConverter))
        {
            existingConverter = converter;
            Converters.TryAdd(name, converter);
        }

        return (TR?)existingConverter(existingValue);
    }

    /// <summary>
    ///     Adds <paramref name="delta"/> to the decimal stored under <paramref name="name"/>,
    ///     initialising it to <paramref name="delta"/> if the slot does not yet exist.
    ///     Uses a single dictionary lookup (via <see cref="CollectionsMarshal"/>) instead of
    ///     the two lookups required by <see cref="GetOrCreateValue{T}"/> + <see cref="SetValue{T}"/>.
    /// </summary>
    public void AddDecimalValue(string name, decimal delta)
    {
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(Values, name, out var exists);
        slot = exists ? (object?)((decimal)slot! + delta) : (object?)delta;
    }

    /// <summary>
    ///     Increments the integer stored under <paramref name="name"/> by one,
    ///     initialising it to 1 if the slot does not yet exist.
    ///     Uses a single dictionary lookup instead of two.
    /// </summary>
    public void IncrementIntValue(string name)
    {
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(Values, name, out var exists);
        slot = exists ? (object?)((int)slot! + 1) : (object?)1;
    }

    /// <summary>
    ///     Stores <paramref name="candidate"/> under <paramref name="name"/> if it is greater
    ///     than the currently stored value, using <paramref name="defaultIfMissing"/> as the
    ///     initial value when the slot does not yet exist.
    ///     Uses a single dictionary lookup instead of two.
    /// </summary>
    public void UpdateDecimalIfGreater(string name, decimal candidate, decimal defaultIfMissing)
    {
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(Values, name, out var exists);
        if (!exists)
        {
            slot = (object?)defaultIfMissing;
        }

        if ((decimal)slot! < candidate)
            slot = (object?)candidate;
    }

    /// <summary>
    ///     Stores <paramref name="candidate"/> under <paramref name="name"/> if it is less than
    ///     the currently stored value, using <paramref name="defaultIfMissing"/> as the initial
    ///     value when the slot does not yet exist.
    ///     Uses a single dictionary lookup instead of two.
    /// </summary>
    public void UpdateDecimalIfLess(string name, decimal candidate, decimal defaultIfMissing)
    {
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(Values, name, out var exists);
        if (!exists)
        {
            slot = (object?)defaultIfMissing;
        }

        if ((decimal)slot! > candidate)
            slot = (object?)candidate;
    }
}
