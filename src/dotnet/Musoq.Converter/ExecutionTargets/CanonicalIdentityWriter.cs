using System.Buffers;
using System.Collections.Generic;
using System.Globalization;

namespace Musoq.Converter.Build;

/// <summary>
/// Owns bounded pooled scratch storage; descriptors always receive their own immutable string.
/// Oversized descriptors append owned chunks instead of repeatedly copying a growing buffer.
/// </summary>
internal ref struct CanonicalIdentityWriter
{
    internal const int InitialCapacity = 4096;
    internal const int MaximumPooledCapacity = 65536;
    private const int IntegerCapacity = 11;
    private const int OwnedSegmentCapacity = 8192;

    private readonly ArrayPool<char> _pool;
    private Storage _storage;
    private List<Segment>? _segments;
    private int _written;
    private int _length;
    private bool _disposed;

    internal CanonicalIdentityWriter(ArrayPool<char> pool)
    {
        ArgumentNullException.ThrowIfNull(pool);
        _pool = pool;
        _storage = Rent(InitialCapacity);
    }

    internal void Append(char value)
    {
        EnsureCapacity(1);
        _storage.Buffer[_written++] = value;
        _length++;
    }

    internal void Append(ReadOnlySpan<char> value)
    {
        EnsureCapacity(value.Length);
        value.CopyTo(_storage.Buffer.AsSpan(_written));
        _written += value.Length;
        _length += value.Length;
    }

    internal void Append(int value)
    {
        EnsureCapacity(IntegerCapacity);
        if (value.TryFormat(_storage.Buffer.AsSpan(_written), out var written))
        {
            _written += written;
            _length += written;
            return;
        }

        Append(value.ToString(CultureInfo.CurrentCulture));
    }

    internal string CreateDescriptor()
    {
        ThrowIfDisposed();
        if (_segments is not { } segments)
            return new string(_storage.Buffer.AsSpan(0, _written));

        return string.Create(_length, (Segments: segments, Current: _storage.Buffer, Written: _written), static (destination, state) =>
        {
            foreach (var segment in state.Segments)
            {
                segment.Storage.Buffer.AsSpan(0, segment.Written).CopyTo(destination);
                destination = destination[segment.Written..];
            }

            state.Current.AsSpan(0, state.Written).CopyTo(destination);
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        var storage = _storage;
        var written = _written;
        var segments = _segments;
        _storage = new Storage([], false);
        _segments = null;
        _written = 0;
        _length = 0;
        if (segments is not null)
        {
            foreach (var segment in segments)
                Return(segment.Storage, segment.Written);
        }

        Return(storage, written);
    }

    private void EnsureCapacity(int additional)
    {
        ThrowIfDisposed();
        _ = checked(_length + additional);
        var required = checked(_written + additional);
        if (required <= _storage.Buffer.Length)
            return;

        if (_segments is not null || required > MaximumPooledCapacity)
        {
            AppendOwnedSegment(additional);
            return;
        }

        var capacity = Math.Min(MaximumPooledCapacity, Math.Max(required, checked(_storage.Buffer.Length * 2)));
        var next = Rent(capacity);
        var previous = _storage;
        previous.Buffer.AsSpan(0, _written).CopyTo(next.Buffer);
        _storage = next;
        Return(previous, _written);
    }

    private void AppendOwnedSegment(int minimumLength)
    {
        var next = new Storage(GC.AllocateUninitializedArray<char>(Math.Max(minimumLength, OwnedSegmentCapacity)), false);
        _segments ??= [];
        _segments.Add(new Segment(_storage, _written));
        _storage = next;
        _written = 0;
    }

    private Storage Rent(int capacity)
    {
        if (capacity > MaximumPooledCapacity)
            return new Storage(GC.AllocateUninitializedArray<char>(capacity), false);

        var buffer = _pool.Rent(capacity);
        if (buffer.Length <= MaximumPooledCapacity)
            return new Storage(buffer, true);

        _pool.Return(buffer);
        return new Storage(GC.AllocateUninitializedArray<char>(capacity), false);
    }

    private void Return(Storage storage, int written)
    {
        if (!storage.IsPooled)
            return;
        storage.Buffer.AsSpan(0, written).Clear();
        _pool.Return(storage.Buffer);
    }

    private readonly void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CanonicalIdentityWriter));
    }

    private readonly record struct Storage(char[] Buffer, bool IsPooled);
    private readonly record struct Segment(Storage Storage, int Written);
}
