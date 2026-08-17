namespace Musoq.Evaluator.Diagnostics;

public interface IProfileClock
{
    long GetTimestamp();

    TimeSpan GetElapsedTime(long startTimestamp, long endTimestamp);
}
