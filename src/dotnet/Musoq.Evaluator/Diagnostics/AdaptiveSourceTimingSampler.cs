namespace Musoq.Evaluator.Diagnostics;

internal struct AdaptiveSourceTimingSampler
{
    private const long InitialExactAttempts = 128;
    private const long SampleWindowSize = 16;
    private const long NormalSampleStride = 512;
    private const long SlowSampleStride = 64;
    private static readonly TimeSpan SlowSampleThreshold = TimeSpan.FromMilliseconds(1);

    private long _remainingWindowSamples;
    private long _nextSampleWindowStart;

    public bool ShouldTime(long nextAttempt)
    {
        if (nextAttempt <= InitialExactAttempts)
            return true;

        if (_nextSampleWindowStart == 0)
            _nextSampleWindowStart = InitialExactAttempts + 1;

        if (_remainingWindowSamples > 0)
        {
            _remainingWindowSamples--;
            return true;
        }

        if (nextAttempt < _nextSampleWindowStart)
            return false;

        _remainingWindowSamples = SampleWindowSize - 1;
        _nextSampleWindowStart = nextAttempt + NormalSampleStride;
        return true;
    }

    public void RecordTimedWait(long attempt, TimeSpan waitTime)
    {
        if (waitTime < SlowSampleThreshold)
            return;

        var slowWindowStart = attempt + SlowSampleStride;
        if (_nextSampleWindowStart == 0 || slowWindowStart < _nextSampleWindowStart)
            _nextSampleWindowStart = slowWindowStart;
    }
}
