using System.Threading;

namespace Musoq.Plugins;

/// <summary>Typed partition storage, ordered indices and frame bounds supplied to a library operation.</summary>
public readonly ref struct WindowFrameContext<T>
{
    private readonly ReadOnlySpan<T> _values;
    private readonly ReadOnlySpan<int> _indices;

    /// <summary>Creates a context over existing typed buffers without allocating.</summary>
    public WindowFrameContext(ReadOnlySpan<T> values, ReadOnlySpan<int> indices,
        int position, int frameStart, int frameEnd, CancellationToken cancellationToken = default)
    {
        _values = values;
        _indices = indices;
        Position = position;
        FrameStart = Math.Max(0, frameStart);
        FrameEnd = Math.Min(indices.Length - 1, frameEnd);
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the current ordered partition position.</summary>
    public int Position { get; }
    /// <summary>Gets the first included position, clamped to the partition start.</summary>
    public int FrameStart { get; }
    /// <summary>Gets the last included position, clamped to the partition end.</summary>
    public int FrameEnd { get; }
    /// <summary>Gets the number of rows in the partition.</summary>
    public int Count => _indices.Length;
    /// <summary>Gets whether the requested frame contains any rows.</summary>
    public bool IsEmpty => FrameStart > FrameEnd;
    /// <summary>Gets the query cancellation token for cooperative custom operations.</summary>
    public CancellationToken CancellationToken { get; }
    /// <summary>Returns a value at an ordered partition position.</summary>
    public T Read(int position) => _values[_indices[position]];
}
