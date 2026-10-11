using System.Threading;

namespace Musoq.Plugins;

/// <summary>Typed values, concrete order keys and frame bounds over independently owned partition buffers.</summary>
public readonly ref struct WindowFrameContext<TValue, TOrderKey>
{
    private readonly WindowFrameContext<TValue> _values;
    private readonly ReadOnlySpan<TOrderKey> _orderKeys;
    private readonly ReadOnlySpan<int> _indices;

    /// <summary>Creates a context over existing buffers without allocating or erasing values.</summary>
    public WindowFrameContext(ReadOnlySpan<TValue> values, ReadOnlySpan<TOrderKey> orderKeys,
        ReadOnlySpan<int> indices, int position, int frameStart, int frameEnd,
        CancellationToken cancellationToken = default)
    {
        _values = new(values, indices, position, frameStart, frameEnd, cancellationToken);
        _orderKeys = orderKeys;
        _indices = indices;
    }

    /// <summary>Gets the current ordered position.</summary>
    public int Position => _values.Position;
    /// <summary>Gets the first included frame position.</summary>
    public int FrameStart => _values.FrameStart;
    /// <summary>Gets the last included frame position.</summary>
    public int FrameEnd => _values.FrameEnd;
    /// <summary>Gets the partition size.</summary>
    public int Count => _values.Count;
    /// <summary>Gets whether the frame is empty.</summary>
    public bool IsEmpty => _values.IsEmpty;
    /// <summary>Gets the query cancellation token.</summary>
    public CancellationToken CancellationToken => _values.CancellationToken;
    /// <summary>Reads a value at an ordered partition position.</summary>
    public TValue Read(int position) => _values.Read(position);
    /// <summary>Reads the exact order key, or concrete ValueTuple of keys, at an ordered partition position.</summary>
    public TOrderKey ReadOrderKey(int position) => _orderKeys[_indices[position]];
}
