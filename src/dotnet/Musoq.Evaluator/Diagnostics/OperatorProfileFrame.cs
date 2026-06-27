namespace Musoq.Evaluator.Diagnostics;

internal struct OperatorProfileFrame
{
    public OperatorProfileRecorder Recorder;
    public long StartedTimestamp;
    public long ExcludedTicks;
    public long InputRows;
    public long OutputRows;
    public int Token;

    public OperatorProfileFrame(OperatorProfileRecorder recorder, long startedTimestamp, int token)
    {
        Recorder = recorder;
        StartedTimestamp = startedTimestamp;
        Token = token;
    }
}
