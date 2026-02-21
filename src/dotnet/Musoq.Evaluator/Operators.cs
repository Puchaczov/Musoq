using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Musoq.Evaluator;

public partial class Operators
{
    // Caches a compiled matcher (string method or Regex) per LIKE pattern.
    // Using Func<string, bool> instead of Regex lets us avoid the compiled-regex
    // overhead for simple patterns like "%.com", "prefix%", "%middle%", or exact matches.
    private static readonly ConcurrentDictionary<string, Func<string, bool>> LikeMatcherCache = new();
    private static readonly ConcurrentDictionary<string, Regex> RLikePatternCache = new();
    private static readonly Regex EscapePattern = CreateEscapeRegex();

    public bool Like(string content, string searchFor)
    {
        if (content is null || searchFor is null)
            return false;

        var matcher = LikeMatcherCache.GetOrAdd(searchFor, CreateLikeMatcher);
        return matcher(content);
    }

    public bool RLike(string content, string pattern)
    {
        if (content is null || pattern is null)
            return false;

        var regex = RLikePatternCache.GetOrAdd(pattern, p =>
            new Regex(p, RegexOptions.Compiled));

        return regex.IsMatch(content);
    }

    public bool Contains<T>(T value, T[] values)
    {
        if (values is null)
            return false;

        return System.Linq.Enumerable.Contains(values, value);
    }

    /// <summary>
    ///     Builds the fastest possible matcher for a SQL LIKE pattern.
    ///     For simple ASCII patterns the method uses string primitives
    ///     (EndsWith / StartsWith / Contains / Equals) which are 10–20× faster
    ///     than a compiled Regex.  Non-ASCII or complex patterns fall back to the
    ///     compiled Regex path so Unicode case-folding is handled correctly.
    /// </summary>
    private static Func<string, bool> CreateLikeMatcher(string sqlPattern)
    {
        var hasPct = sqlPattern.Contains('%');
        var hasUnderscore = sqlPattern.Contains('_');

        // ── fast paths (ASCII-safe patterns only) ────────────────────────────
        // We restrict fast paths to ASCII literal portions so that OrdinalIgnoreCase
        // gives identical results to the Regex IgnoreCase path for the Latin A–Z range.
        // Patterns with non-ASCII chars fall through to the Regex fallback which
        // uses the runtime's full Unicode case tables.

        if (!hasPct && !hasUnderscore)
        {
            // Exact match (no wildcards at all)
            if (IsAscii(sqlPattern))
            {
                var literal = sqlPattern;
                return content => string.Equals(content, literal, StringComparison.OrdinalIgnoreCase);
            }
        }
        else if (hasPct && !hasUnderscore)
        {
            // %suffix  →  EndsWith
            if (sqlPattern.StartsWith('%'))
            {
                var rest = sqlPattern.AsSpan(1);
                if (!rest.Contains('%'))
                {
                    var suffix = rest.ToString();
                    if (suffix.Length == 0) return _ => true; // bare '%'
                    if (IsAscii(suffix))
                        return content => content.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
                }
            }

            // prefix%  →  StartsWith
            if (sqlPattern.EndsWith('%'))
            {
                var rest = sqlPattern.AsSpan(0, sqlPattern.Length - 1);
                if (!rest.Contains('%'))
                {
                    var prefix = rest.ToString();
                    if (prefix.Length == 0) return _ => true; // bare '%'
                    if (IsAscii(prefix))
                        return content => content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                }
            }

            // %middle%  →  Contains
            if (sqlPattern.StartsWith('%') && sqlPattern.EndsWith('%'))
            {
                var middle = sqlPattern.AsSpan(1, sqlPattern.Length - 2);
                if (middle.Length == 0) return _ => true; // '%%'
                if (!middle.Contains('%') && !middle.Contains('_'))
                {
                    var needle = middle.ToString();
                    if (IsAscii(needle))
                        return content => content.Contains(needle, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        // ── Regex fallback (handles underscores, complex patterns, non-ASCII) ──
        var escaped = EscapePattern.Replace(sqlPattern, match => @"\" + match.Value);
        var regexPattern = escaped.Replace("_", ".").Replace("%", ".*");
        var regex = new Regex(
            @"\A" + regexPattern + @"\z",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        return regex.IsMatch;
    }

    /// <summary>Returns true if every character in <paramref name="s"/> is in the ASCII range.</summary>
    private static bool IsAscii(string s)
    {
        foreach (var c in s)
            if (c > 127)
                return false;
        return true;
    }

    [GeneratedRegex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\", RegexOptions.Compiled)]
    private static partial Regex CreateEscapeRegex();
}
