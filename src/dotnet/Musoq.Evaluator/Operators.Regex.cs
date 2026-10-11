using System.Text.RegularExpressions;
using System.Globalization;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator;

public partial class Operators
{
    private static readonly BoundedRuntimeCache<RLikeMatcherCacheKey, RLikeSimpleMatcher?> RLikeSimplePatternCache =
        new(RuntimeCacheOptions.PatternCacheSize);

    internal static int SimpleRLikeMatcherCacheCount => RLikeSimplePatternCache.Count;

    internal static RLikeSimpleMatcher? GetOrCreateSimpleRLikeMatcher(string pattern, TimeSpan timeout)
    {
        var key = new RLikeMatcherCacheKey(pattern, CultureInfo.CurrentCulture.Name, timeout.Ticks);
        return RLikeSimplePatternCache.GetOrAdd(key, static value =>
            RLikeSimpleMatcher.TryCreate(value.Pattern, TimeSpan.FromTicks(value.TimeoutTicks)));
    }

    internal static Regex CreateRLikeRegex(string pattern, TimeSpan timeout) =>
        CreateRLikeRegex(pattern, timeout, static (value, options, budget) => new Regex(value, options, budget));

    internal static Regex CreateRLikeRegex(
        string pattern,
        TimeSpan timeout,
        Func<string, RegexOptions, TimeSpan, Regex> factory)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(factory);
        RegexMatchTimeoutPolicy.Validate(timeout);

        try
        {
            return factory(pattern, RegexOptions.NonBacktracking, timeout);
        }
        catch (NotSupportedException)
        {
            return factory(pattern, RegexOptions.Compiled, timeout);
        }
    }
}
