using System.Threading;

namespace Musoq.Plugins;

/// <summary>Engine-owned typed partition inputs, resolved frame bounds and destination storage.</summary>
public readonly ref struct WindowBatchContext<TInput, TResult>
{
    /// <summary>Creates a batch view over already allocated concrete buffers.</summary>
    public WindowBatchContext(ReadOnlySpan<TInput> values, ReadOnlySpan<int> indices,
        ReadOnlySpan<int> frameStarts, ReadOnlySpan<int> frameEnds, Span<TResult> results,
        CancellationToken cancellationToken = default)
    {
        Values = values;
        Indices = indices;
        FrameStarts = frameStarts;
        FrameEnds = frameEnds;
        Results = results;
        CancellationToken = cancellationToken;
    }
    /// <summary>Gets values indexed by original row position.</summary>
    public ReadOnlySpan<TInput> Values { get; }
    /// <summary>Gets original row indices in partition order.</summary>
    public ReadOnlySpan<int> Indices { get; }
    /// <summary>Gets frame starts in ordered partition positions.</summary>
    public ReadOnlySpan<int> FrameStarts { get; }
    /// <summary>Gets inclusive frame ends in ordered partition positions.</summary>
    public ReadOnlySpan<int> FrameEnds { get; }
    /// <summary>Gets concrete result storage indexed by original row position.</summary>
    public Span<TResult> Results { get; }
    /// <summary>Gets the query cancellation token for cooperative batch execution.</summary>
    public CancellationToken CancellationToken { get; }
}
