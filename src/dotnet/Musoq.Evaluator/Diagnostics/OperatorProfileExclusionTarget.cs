namespace Musoq.Evaluator.Diagnostics;

internal readonly struct OperatorProfileExclusionTarget
{
    private readonly QueryProfileRecorder? _owner;
    private readonly CapturedOperatorProfileFrame[]? _frames;

    public OperatorProfileExclusionTarget(
        QueryProfileRecorder owner,
        CapturedOperatorProfileFrame[] frames)
    {
        _owner = owner;
        _frames = frames;
    }

    public bool IsEnabled => _owner != null && _frames is { Length: > 0 };

    public void ExcludeElapsedTicks(long elapsedTicks)
    {
        if (elapsedTicks <= 0 || _owner == null || _frames == null)
            return;

        _owner.ExcludeOperatorFrameElapsed(_frames, elapsedTicks);
    }
}
