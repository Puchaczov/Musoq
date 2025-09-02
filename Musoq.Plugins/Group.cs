using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Musoq.Plugins;

/// <summary>
/// Represents a group of rows.
/// </summary>
#if DEBUG
[DebuggerDisplay("{Name}")]
#endif
public sealed class Group
{
#if DEBUG
    private string Name { get; }
#endif

    private IDictionary<string, object?> Values { get; } = new Dictionary<string, object?>();

    private IDictionary<string, Func<object?, object?>> Converters { get; } =
        new Dictionary<string, Func<object?, object?>>();
        
    /// <summary>
    /// Creates a new group.
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="fieldNames"></param>
    /// <param name="values"></param>
    public Group(Group? parent, string[] fieldNames, object[] values)
    {
        Parent = parent;
#if DEBUG
        Name = fieldNames.Length == 0 ? "root" : fieldNames.Aggregate((a, b) => a + ',' + b);
#endif
        for (var i = 0; i < fieldNames.Length; i++) Values.Add(fieldNames[i], values[i]);
    }

    /// <summary>
    /// Gets the parent group.
    /// </summary>
    public Group? Parent { get; }
        
    /// <summary>
    /// Gets the number of hits.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Increments the number of hits.
    /// </summary>
    public void Hit()
    {
        Count += 1;
    }

    /// <summary>
    /// Gets the value of the group.
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
            return (T?) converter(value);

        return (T?) Values[name];
    }

    /// <summary>
    /// Gets the value of the group.
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public T? GetRawValue<T>(string name)
    {
        if (!Values.TryGetValue(name, out var value))
            throw new KeyNotFoundException($"Group does not have value {name}.");

        return (T?) value;
    }

    /// <summary>
    /// Gets the value of the group or creates a new one.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="defValue"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetOrCreateValue<T>(string name, T? defValue = default)
    {
        if (!Values.ContainsKey(name))
            Values.Add(name, defValue);

        return (T?) Values[name];
    }

    /// <summary>
    /// Gets the value of the group or creates a new one.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="createDefault"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T? GetOrCreateValue<T>(string name, Func<T> createDefault)
    {
        if (!Values.ContainsKey(name))
            Values.Add(name, createDefault());

        return (T?)Values[name];
    }

    /// <summary>
    /// Sets the value of the group.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public void SetValue<T>(string name, T? value)
    {
        Values[name] = value;
    }

    /// <summary>
    /// Gets the value of the group or creates a new one.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <param name="converter"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TR"></typeparam>
    /// <returns></returns>
    public TR? GetOrCreateValueWithConverter<T, TR>(string name, T value, Func<object?, object?> converter)
    {
        if (!Values.ContainsKey(name))
            Values.Add(name, value);

        Converters.TryAdd(name, converter);

        return (TR?) Converters[name](Values[name]);
    }
}