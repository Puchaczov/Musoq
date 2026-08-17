using System.Threading;

namespace Musoq.Evaluator.Diagnostics;

public sealed class OperatorProfileScope : IDisposable
{
    private readonly OperatorProfileRecorder? _recorder;
    private readonly QueryProfileRecorder? _owner;
    private readonly int _frameIndex;
    private readonly int _frameToken;
    private int _disposed;

    internal OperatorProfileScope(
        OperatorProfileRecorder recorder,
        QueryProfileRecorder owner,
        int frameIndex,
        int frameToken)
    {
        _recorder = recorder;
        _owner = owner;
        _frameIndex = frameIndex;
        _frameToken = frameToken;
    }

    private OperatorProfileScope()
    {
    }

    public static OperatorProfileScope None { get; } = new();

    public void RecordException(Exception exception)
    {
        _recorder?.RecordException(exception);
    }

    public void AddInputRows(long count)
    {
        if (count <= 0 || _recorder == null)
            return;

        _owner?.AddOperatorFrameInputRows(_frameIndex, _frameToken, count);
    }

    public void AddOutputRows(long count)
    {
        if (count <= 0 || _recorder == null)
            return;

        _owner?.AddOperatorFrameOutputRows(_frameIndex, _frameToken, count);
    }

    internal void ExcludeElapsed(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
            return;

        _owner?.ExcludeOperatorFrameElapsed(_frameIndex, _frameToken, elapsed.Ticks);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _owner?.CompleteOperatorFrame(_frameIndex, _frameToken);
    }
}
