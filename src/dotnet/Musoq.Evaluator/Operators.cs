#nullable enable
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator;

public partial class Operators
{
    private static readonly ConcurrentDictionary<string, Func<string, bool>> LikeMatcherCache = new();
    private static readonly ConcurrentDictionary<string, Regex> RLikePatternCache = new();
    private static readonly Regex EscapePattern = CreateEscapeRegex();

    public bool Like(string content, string searchFor)
    {
        if (content is null || searchFor is null)
            return false;

        var matcher = LikeMatcherCache.GetOrAdd(searchFor, static pattern => CreateLikeMatcher(pattern));
        return matcher(content);
    }

    public bool RLike(string content, string pattern)
    {
        if (content is null || pattern is null)
            return false;

        var regex = RLikePatternCache.GetOrAdd(pattern, static p =>
            new Regex(p, RegexOptions.Compiled));

        return regex.IsMatch(content);
    }

    public bool Contains<T>(T value, T[] values)
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
        var sqlPattern = escaped.Replace("_", ".").Replace("%", ".*");
        var regex = new Regex(string.Concat(@"\A", sqlPattern, @"\z"),
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
        if (!pattern.Contains('%'))
            return content => string.Equals(content, pattern, StringComparison.OrdinalIgnoreCase);

        var startsWithPercent = pattern[0] == '%';
        var endsWithPercent = pattern[^1] == '%';

        if (startsWithPercent && endsWithPercent)
        {
            if (pattern.Length <= 2)
                return static _ => true;

            var inner = pattern[1..^1];
            if (!inner.Contains('%'))
                return content => content.Contains(inner, StringComparison.OrdinalIgnoreCase);
        }

        if (startsWithPercent && !endsWithPercent)
        {
            var suffix = pattern[1..];
            if (!suffix.Contains('%'))
                return content => content.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (!startsWithPercent && endsWithPercent)
        {
            var prefix = pattern[..^1];
            if (!prefix.Contains('%'))
                return content => content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }

    [GeneratedRegex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\", RegexOptions.Compiled)]
    private static partial Regex CreateEscapeRegex();
}
