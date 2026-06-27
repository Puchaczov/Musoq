namespace Musoq.Evaluator.Diagnostics;

public readonly struct OperatorProfileHandle
{
    private readonly OperatorProfileRecorder? _recorder;

    internal OperatorProfileHandle(OperatorProfileRecorder recorder)
    {
        _recorder = recorder;
    }

    public static OperatorProfileHandle None { get; } = new();

    public bool IsEnabled => _recorder != null;

    internal OperatorProfileRecorder? Recorder => _recorder;
}
