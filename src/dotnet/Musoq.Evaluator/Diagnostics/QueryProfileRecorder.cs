using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Diagnostics;

public sealed class QueryProfileRecorder
{
    private readonly object _gate = new();
    private readonly IProfileClock _clock;
    private readonly string? _queryId;
    private readonly long _startedTimestamp;
    private readonly List<SourceProfileRecorder> _sources = [];
    private readonly List<OperatorProfileSnapshot> _operators = [];
    private readonly Dictionary<string, OperatorProfileRecorder> _operatorRecorders = new(StringComparer.Ordinal);
    private readonly List<string> _operatorOrder = [];
    private readonly ThreadLocal<List<OperatorProfileFrame>> _operatorFrames =
        new(static () => []);
    private int _nextOperatorFrameToken;

    public QueryProfileRecorder(IProfileClock? clock = null, string? queryId = null)
    {
        _clock = clock ?? StopwatchProfileClock.Instance;
        _queryId = string.IsNullOrWhiteSpace(queryId) ? null : queryId;
        _startedTimestamp = _clock.GetTimestamp();
    }

    public IProfileClock Clock => _clock;

    public SourceProfileRecorder CreateSourceRecorder(string name)
    {
        return CreateSourceRecorder(name, SourceProfileTimingMode.Exact);
    }

    public SourceProfileRecorder CreateAdaptiveSourceRecorder(string name)
    {
        return CreateSourceRecorder(name, SourceProfileTimingMode.Adaptive);
    }

    private SourceProfileRecorder CreateSourceRecorder(string name, SourceProfileTimingMode timingMode)
    {
        var sourceRecorder = new SourceProfileRecorder(name, _clock, this);
        if (timingMode == SourceProfileTimingMode.Adaptive)
            sourceRecorder.EnableAdaptiveTiming();

        lock (_gate)
        {
            _sources.Add(sourceRecorder);
        }

        return sourceRecorder;
    }

    public IEnumerable<T> ProfileSource<T>(string name, IEnumerable<T> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        return ProfiledEnumerable<T>.Create(rows, CreateSourceRecorder(name));
    }

    public void AddOperator(OperatorProfileSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_gate)
        {
            _operators.Add(snapshot);
        }
    }

    public void RegisterOperators(IEnumerable<ExecutionPlanOperatorDescriptor> operators)
    {
        ArgumentNullException.ThrowIfNull(operators);

        lock (_gate)
        {
            foreach (var descriptor in operators)
                GetOrCreateOperatorRecorder(descriptor.Id, descriptor.NodeKind);
        }
    }

    public OperatorProfileScope BeginOperator(string id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        OperatorProfileRecorder recorder;
        lock (_gate)
        {
            recorder = GetOrCreateOperatorRecorder(id, name);
        }

        return recorder.Begin(name, this);
    }

    public OperatorProfileHandle GetOperatorHandle(string id, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        lock (_gate)
        {
            return new OperatorProfileHandle(GetOrCreateOperatorRecorder(id, name));
        }
    }

    public OperatorProfileScope BeginOperator(OperatorProfileHandle handle)
    {
        var recorder = handle.Recorder;
        if (recorder == null)
            return OperatorProfileScope.None;

        return recorder.Begin(this);
    }

    public OperatorProfileValueScope BeginOperatorValue(OperatorProfileHandle handle)
    {
        var recorder = handle.Recorder;
        return recorder?.BeginValue(this) ?? OperatorProfileValueScope.None;
    }

    public void AddOperatorInputRows(string id, long count)
    {
        if (count <= 0)
            return;

        OperatorProfileRecorder? recorder;
        lock (_gate)
        {
            recorder = _operatorRecorders.GetValueOrDefault(id);
        }

        recorder?.AddInputRows(count);
    }

    public void AddOperatorInputRows(OperatorProfileHandle handle, long count)
    {
        if (count <= 0)
            return;

        handle.Recorder?.AddInputRows(count);
    }

    public void AddOperatorOutputRows(string id, long count)
    {
        if (count <= 0)
            return;

        OperatorProfileRecorder? recorder;
        lock (_gate)
        {
            recorder = _operatorRecorders.GetValueOrDefault(id);
        }

        recorder?.AddOutputRows(count);
    }

    public void AddOperatorOutputRows(OperatorProfileHandle handle, long count)
    {
        if (count <= 0)
            return;

        handle.Recorder?.AddOutputRows(count);
    }

    public void RecordOperatorException(string id, Exception exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(exception);

        OperatorProfileRecorder? recorder;
        lock (_gate)
        {
            recorder = _operatorRecorders.GetValueOrDefault(id);
        }

        recorder?.RecordException(exception);
    }

    public int GetCurrentOperatorScopeDepth() => _operatorFrames.Value?.Count ?? 0;

    public bool RecordActiveOperatorException(Exception exception, int startDepth)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var frames = _operatorFrames.Value;
        if (frames is null || frames.Count <= startDepth)
            return true;

        var targetDepth = Math.Max(0, startDepth);
        for (var index = targetDepth; index < frames.Count; index++)
            frames[index].Recorder.RecordException(exception);

        return true;
    }

    public void DisposeActiveOperatorScopes(int startDepth)
    {
        var frames = _operatorFrames.Value;
        if (frames is null)
            return;

        var targetDepth = Math.Max(0, startDepth);
        while (frames.Count > targetDepth)
            CompleteOperatorFrame(frames.Count - 1, frames[^1].Token);
    }

    public QueryProfileSnapshot CreateSnapshot()
    {
        SourceProfileRecorder[] sources;
        OperatorProfileSnapshot[] operators;
        OperatorProfileRecorder[] operatorRecorders;

        lock (_gate)
        {
            sources = _sources.ToArray();
            operators = _operators.ToArray();
            operatorRecorders = _operatorOrder
                .Select(id => _operatorRecorders[id])
                .ToArray();
        }

        var snapshotSources = sources
            .Select(source => source.CreateSnapshot())
            .ToArray();

        return new QueryProfileSnapshot(
            _clock.GetElapsedTime(_startedTimestamp, _clock.GetTimestamp()),
            snapshotSources,
            operatorRecorders
                .Select(static recorder => recorder.CreateSnapshot())
                .Concat(operators)
                .ToArray())
        {
            QueryId = _queryId
        };
    }

    internal void ExcludeCurrentOperatorElapsed(long startTimestamp, long endTimestamp)
    {
        var elapsed = _clock.GetElapsedTime(startTimestamp, endTimestamp);
        if (elapsed <= TimeSpan.Zero)
            return;

        var frames = _operatorFrames.Value;
        if (frames is null || frames.Count == 0)
            return;

        var elapsedTicks = elapsed.Ticks;
        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames[index];
            frame.ExcludedTicks += elapsedTicks;
            frames[index] = frame;
        }
    }

    internal OperatorProfileExclusionTarget CaptureCurrentOperatorExclusionTarget()
    {
        var frames = _operatorFrames.Value;
        if (frames is null || frames.Count == 0)
            return default;

        var capturedFrames = new CapturedOperatorProfileFrame[frames.Count];
        for (var index = 0; index < frames.Count; index++)
            capturedFrames[index] = new CapturedOperatorProfileFrame(index, frames[index].Token);

        return new OperatorProfileExclusionTarget(this, capturedFrames);
    }

    internal OperatorProfileScope BeginOperatorScope(OperatorProfileRecorder recorder, long startedTimestamp)
    {
        var frames = _operatorFrames.Value!;
        var token = Interlocked.Increment(ref _nextOperatorFrameToken);
        var index = frames.Count;
        frames.Add(new OperatorProfileFrame(recorder, startedTimestamp, token));
        return new OperatorProfileScope(recorder, this, index, token);
    }

    internal OperatorProfileValueScope BeginOperatorValueScope(OperatorProfileRecorder recorder, long startedTimestamp)
    {
        var frames = _operatorFrames.Value!;
        var token = Interlocked.Increment(ref _nextOperatorFrameToken);
        var index = frames.Count;
        frames.Add(new OperatorProfileFrame(recorder, startedTimestamp, token));
        return new OperatorProfileValueScope(this, index, token);
    }

    internal void AddOperatorFrameInputRows(int frameIndex, int frameToken, long count)
    {
        if (count <= 0)
            return;

        UpdateOperatorFrame(frameIndex, frameToken, frame =>
        {
            frame.InputRows += count;
            return frame;
        });
    }

    internal void AddOperatorFrameOutputRows(int frameIndex, int frameToken, long count)
    {
        if (count <= 0)
            return;

        UpdateOperatorFrame(frameIndex, frameToken, frame =>
        {
            frame.OutputRows += count;
            return frame;
        });
    }

    internal void ExcludeOperatorFrameElapsed(int frameIndex, int frameToken, long elapsedTicks)
    {
        if (elapsedTicks <= 0)
            return;

        UpdateOperatorFrame(frameIndex, frameToken, frame =>
        {
            frame.ExcludedTicks += elapsedTicks;
            return frame;
        });
    }

    internal void ExcludeOperatorFrameElapsed(
        IReadOnlyList<CapturedOperatorProfileFrame> capturedFrames,
        long elapsedTicks)
    {
        if (elapsedTicks <= 0)
            return;

        foreach (var frame in capturedFrames)
            ExcludeOperatorFrameElapsed(frame.Index, frame.Token, elapsedTicks);
    }

    internal void CompleteOperatorFrame(int frameIndex, int frameToken)
    {
        var frames = _operatorFrames.Value;
        if (frames is null || frames.Count == 0)
            return;

        var actualIndex = FindOperatorFrameIndex(frames, frameIndex, frameToken);
        if (actualIndex < 0)
            return;

        var frame = frames[actualIndex];
        frames.RemoveAt(actualIndex);
        frame.Recorder.Complete(
            frame.StartedTimestamp,
            frame.ExcludedTicks,
            frame.InputRows,
            frame.OutputRows);
    }

    private void UpdateOperatorFrame(int frameIndex, int frameToken, Func<OperatorProfileFrame, OperatorProfileFrame> update)
    {
        var frames = _operatorFrames.Value;
        if (frames is null || frames.Count == 0)
            return;

        var actualIndex = FindOperatorFrameIndex(frames, frameIndex, frameToken);
        if (actualIndex < 0)
            return;

        frames[actualIndex] = update(frames[actualIndex]);
    }

    private static int FindOperatorFrameIndex(List<OperatorProfileFrame> frames, int frameIndex, int frameToken)
    {
        if (frameIndex >= 0 && frameIndex < frames.Count && frames[frameIndex].Token == frameToken)
            return frameIndex;

        for (var index = frames.Count - 1; index >= 0; index--)
        {
            if (frames[index].Token == frameToken)
                return index;
        }

        return -1;
    }

    private OperatorProfileRecorder GetOrCreateOperatorRecorder(string id, string name)
    {
        if (_operatorRecorders.TryGetValue(id, out var recorder))
            return recorder;

        recorder = new OperatorProfileRecorder(id, name, _clock);
        _operatorRecorders.Add(id, recorder);
        _operatorOrder.Add(id);
        return recorder;
    }
}
