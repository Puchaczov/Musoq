using System.Threading;

namespace Musoq.Benchmarks;

public sealed class OptimizationBenchmarkRecorder
{
    private int _payloadOpens;

    public int PayloadOpens => _payloadOpens;

    public void RecordPayloadOpen()
    {
        Interlocked.Increment(ref _payloadOpens);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _payloadOpens, 0);
    }
}
