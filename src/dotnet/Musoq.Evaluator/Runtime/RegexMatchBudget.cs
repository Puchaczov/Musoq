namespace Musoq.Evaluator.Runtime;

/// <summary>A finite match budget validated before execution-local matcher construction.</summary>
internal sealed class RegexMatchBudget
{
    private RegexMatchBudget(TimeSpan timeout) => Timeout = timeout;

    internal TimeSpan Timeout { get; }

    internal static RegexMatchBudget Default { get; } =
        new(RegexMatchTimeoutPolicy.Validate(RuntimeCacheOptions.DefaultRegexTimeout));

    internal static RegexMatchBudget Create(TimeSpan timeout)
    {
        var validated = RegexMatchTimeoutPolicy.Validate(timeout);
        if (validated == Default.Timeout)
            return Default;
        return new RegexMatchBudget(validated);
    }
}
