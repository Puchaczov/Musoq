using System;
using System.Collections.Generic;

namespace Musoq.Converter.Build;

public partial class BuildItems
{
    private T GetRequired<T>(string key)
    {
        if (!TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Required build item '{key}' was not set.");

        return (T)value;
    }

    private void SetRequired<T>(string key, T value)
        where T : notnull
    {
        this[key] = value;
    }

    private bool GetFlag(string key, bool defaultWhenMissing)
    {
        return TryGetValue(key, out var value) ? (bool)value : defaultWhenMissing;
    }

    private void SetFlag(string key, bool value)
    {
        this[key] = value;
    }

    private IReadOnlyList<T> GetListOrEmpty<T>(string key)
    {
        return TryGetValue(key, out var value)
            ? (IReadOnlyList<T>)value
            : Array.Empty<T>();
    }
}
