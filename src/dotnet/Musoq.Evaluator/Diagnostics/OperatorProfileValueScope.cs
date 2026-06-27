using System;

namespace Musoq.Evaluator.Diagnostics;

public struct OperatorProfileValueScope : IDisposable
{
    private readonly QueryProfileRecorder? _owner;
    private readonly int _frameIndex;
    private readonly int _frameToken;
    private bool _disposed;

    internal OperatorProfileValueScope(QueryProfileRecorder owner, int frameIndex, int frameToken)
    {
        _owner = owner;
        _frameIndex = frameIndex;
        _frameToken = frameToken;
    }

    public static OperatorProfileValueScope None => new();

    public bool IsEnabled => _owner != null;

    public void AddInputRows(long count)
    {
        if (count <= 0)
            return;

        _owner?.AddOperatorFrameInputRows(_frameIndex, _frameToken, count);
    }

    public void AddOutputRows(long count)
    {
        if (count <= 0)
            return;

        _owner?.AddOperatorFrameOutputRows(_frameIndex, _frameToken, count);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _owner?.CompleteOperatorFrame(_frameIndex, _frameToken);
    }
}
