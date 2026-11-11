using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Musoq.Schema.Interpreters;

internal sealed class CompactCompletionSet(ParserCompletionLayout layout) : ISet<string>, IReadOnlySet<string>
{
    private ulong _first;
    private ulong[]? _remaining;
    private HashSet<string>? _extra;
    public int Count { get; private set; }
    public bool IsReadOnly => false;

    internal bool Mark(int index)
    {
        if ((uint)index >= (uint)layout.Names.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        ref var word = ref Word(index);
        var mask = 1UL << (index & 63);
        if ((word & mask) != 0)
            return false;
        word |= mask;
        Count++;
        return true;
    }

    private ref ulong Word(int index)
    {
        if (index < 64)
            return ref _first;
        _remaining ??= new ulong[(layout.Names.Count - 1) / 64];
        return ref _remaining[(index >> 6) - 1];
    }

    public bool Add(string item)
    {
        var index = layout.IndexOf(item);
        if (index >= 0)
            return Mark(index);
        _extra ??= new HashSet<string>(StringComparer.Ordinal);
        if (!_extra.Add(item))
            return false;
        Count++;
        return true;
    }

    void ICollection<string>.Add(string item) => Add(item);

    public bool Contains(string item)
    {
        var index = layout.IndexOf(item);
        if (index < 0)
            return _extra?.Contains(item) ?? false;
        var word = index < 64 ? _first : _remaining?[(index >> 6) - 1] ?? 0;
        return (word & (1UL << (index & 63))) != 0;
    }

    public bool Remove(string item)
    {
        if (!Contains(item))
            return false;
        var index = layout.IndexOf(item);
        if (index >= 0)
            Word(index) &= ~(1UL << (index & 63));
        else
            _extra!.Remove(item);
        Count--;
        return true;
    }

    public void Clear()
    {
        _first = 0;
        if (_remaining is not null)
            Array.Clear(_remaining);
        _extra?.Clear();
        Count = 0;
    }

    public IEnumerator<string> GetEnumerator()
    {
        var words = (layout.Names.Count + 63) / 64;
        for (var index = 0; index < words; index++)
        {
            var word = index == 0 ? _first : _remaining?[index - 1] ?? 0;
            while (word != 0)
            {
                yield return layout.Names[index * 64 + BitOperations.TrailingZeroCount(word)];
                word &= word - 1;
            }
        }
        if (_extra is not null)
            foreach (var name in _extra)
                yield return name;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void CopyTo(string[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (arrayIndex > array.Length || Count > array.Length - arrayIndex)
            throw new ArgumentException("Destination array is too small.", nameof(array));
        foreach (var item in this)
            array[arrayIndex++] = item;
    }

    private HashSet<string> AsSet() => new(this, StringComparer.Ordinal);

    public bool IsProperSubsetOf(IEnumerable<string> other) => AsSet().IsProperSubsetOf(other);

    public bool IsProperSupersetOf(IEnumerable<string> other) => AsSet().IsProperSupersetOf(other);

    public bool IsSubsetOf(IEnumerable<string> other) => AsSet().IsSubsetOf(other);

    public bool IsSupersetOf(IEnumerable<string> other) => AsSet().IsSupersetOf(other);

    public bool Overlaps(IEnumerable<string> other) => AsSet().Overlaps(other);

    public bool SetEquals(IEnumerable<string> other) => AsSet().SetEquals(other);

    public void UnionWith(IEnumerable<string> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        foreach (var item in other)
            Add(item);
    }

    public void ExceptWith(IEnumerable<string> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        foreach (var item in other.ToArray())
            Remove(item);
    }

    public void IntersectWith(IEnumerable<string> other)
    {
        var keep = new HashSet<string>(other, StringComparer.Ordinal);
        foreach (var item in this.ToArray())
            if (!keep.Contains(item))
                Remove(item);
    }

    public void SymmetricExceptWith(IEnumerable<string> other)
    {
        foreach (var item in new HashSet<string>(other, StringComparer.Ordinal))
            if (!Remove(item))
                Add(item);
    }
}
