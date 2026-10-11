namespace Musoq.Evaluator.Runtime;

internal static class RegexMatchTimeoutPolicy
{
    internal static TimeSpan Validate(TimeSpan timeout)
    {
        if (timeout < TimeSpan.FromMilliseconds(1) ||
            timeout > TimeSpan.FromMilliseconds(int.MaxValue - 1))
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout,
                "Regex match timeout must be between one millisecond and 2147483646 milliseconds.");

        return timeout;
    }
}
