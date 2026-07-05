using System.Text.RegularExpressions;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator;

public partial class Operators
{
    private static readonly BoundedRuntimeCache<string, Func<string, bool>> LikeMatcherCache =
        new(RuntimeCacheOptions.PatternCacheSize, StringComparer.Ordinal);
    private static readonly BoundedRuntimeCache<string, Regex> RLikePatternCache =
        new(RuntimeCacheOptions.PatternCacheSize, StringComparer.Ordinal);
    private static readonly Regex EscapePattern = CreateEscapeRegex();

    public bool Like(string? content, string? searchFor)
    {
        if (content is null || searchFor is null)
            return false;

        var matcher = LikeMatcherCache.GetOrAdd(searchFor, static pattern => CreateLikeMatcher(pattern));
        return matcher(content);
    }

    public bool RLike(string? content, string? pattern)
    {
        if (content is null || pattern is null)
            return false;

        var regex = RLikePatternCache.GetOrAdd(pattern, static p =>
            new Regex(p, RegexOptions.Compiled, RuntimeCacheOptions.DefaultRegexTimeout));

        return regex.IsMatch(content);
    }

    public bool Contains<T>(T? value, T?[]? values)
    {
        if (values is null)
            return false;

        return Array.IndexOf(values, value) >= 0;
    }

    private static Func<string, bool> CreateLikeMatcher(string pattern)
    {
        if (!ContainsUnderscoreOrNonAscii(pattern))
        {
            var fastMatcher = TryCreateFastMatcher(pattern);
            if (fastMatcher is not null)
                return fastMatcher;
        }

        var escaped = EscapePattern.Replace(pattern, static match => @"\" + match.Value);
        var sqlPattern = escaped.Replace("_", ".", StringComparison.Ordinal).Replace("%", ".*", StringComparison.Ordinal);
        var regex = new Regex(string.Concat(@"\A", sqlPattern, @"\z"),
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled,
            RuntimeCacheOptions.DefaultRegexTimeout);
        return regex.IsMatch;
    }

    private static bool ContainsUnderscoreOrNonAscii(string pattern)
    {
        foreach (var c in pattern)
        {
            if (c == '_' || c > 127)
                return true;
        }

        return false;
    }

    private static Func<string, bool>? TryCreateFastMatcher(string pattern)
    {
        if (!pattern.Contains('%', StringComparison.Ordinal))
            return content => string.Equals(content, pattern, StringComparison.OrdinalIgnoreCase);

        var startsWithPercent = pattern[0] == '%';
        var endsWithPercent = pattern[^1] == '%';

        if (startsWithPercent && endsWithPercent)
        {
            if (pattern.Length <= 2)
                return static _ => true;

            var inner = pattern[1..^1];
            if (!inner.Contains('%', StringComparison.Ordinal))
                return content => content.Contains(inner, StringComparison.OrdinalIgnoreCase);
        }

        if (startsWithPercent && !endsWithPercent)
        {
            var suffix = pattern[1..];
            if (!suffix.Contains('%', StringComparison.Ordinal))
                return content => content.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (!startsWithPercent && endsWithPercent)
        {
            var prefix = pattern[..^1];
            if (!prefix.Contains('%', StringComparison.Ordinal))
                return content => content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }

    [GeneratedRegex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\", RegexOptions.Compiled)]
    private static partial Regex CreateEscapeRegex();
}
