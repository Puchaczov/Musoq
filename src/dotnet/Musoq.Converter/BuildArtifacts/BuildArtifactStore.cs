using System.Collections.Generic;

namespace Musoq.Converter.Build;

internal sealed class BuildArtifactStore(IDictionary<string, object> backing)
{
    public bool Contains<T>(BuildArtifactSlot<T> slot)
    {
        return backing.ContainsKey(slot.Key);
    }

    public T GetRequired<T>(BuildArtifactSlot<T> slot)
    {
        if (!backing.TryGetValue(slot.Key, out var value))
            throw new KeyNotFoundException($"Required build item '{slot.Key}' was not set.");

        return (T)value;
    }

    public bool TryGet<T>(BuildArtifactSlot<T> slot, out T value)
    {
        if (backing.TryGetValue(slot.Key, out var stored))
        {
            value = (T)stored;
            return true;
        }

        value = default!;
        return false;
    }

    public T? GetOptional<T>(BuildArtifactSlot<T> slot)
        where T : class
    {
        return backing.TryGetValue(slot.Key, out var value) ? (T)value : null;
    }

    public void SetRequired<T>(BuildArtifactSlot<T> slot, T value)
        where T : notnull
    {
        backing[slot.Key] = value;
    }

    public void SetOptional<T>(BuildArtifactSlot<T> slot, T? value)
        where T : class
    {
        if (value == null)
        {
            backing.Remove(slot.Key);
            return;
        }

        backing[slot.Key] = value;
    }

    public bool GetFlag(BuildArtifactSlot<bool> slot, bool defaultWhenMissing)
    {
        return backing.TryGetValue(slot.Key, out var value) ? (bool)value : defaultWhenMissing;
    }

    public void SetFlag(BuildArtifactSlot<bool> slot, bool value)
    {
        backing[slot.Key] = value;
    }

    public T GetValueOrDefault<T>(BuildArtifactSlot<T> slot, T defaultWhenMissing)
        where T : struct
    {
        return backing.TryGetValue(slot.Key, out var value) ? (T)value : defaultWhenMissing;
    }

    public IReadOnlyList<T> GetListOrEmpty<T>(BuildArtifactSlot<IReadOnlyList<T>> slot)
    {
        return backing.TryGetValue(slot.Key, out var value)
            ? (IReadOnlyList<T>)value
            : [];
    }
}
