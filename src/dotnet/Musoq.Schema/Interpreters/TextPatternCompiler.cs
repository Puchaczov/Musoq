using System.Text.RegularExpressions;

namespace Musoq.Schema.Interpreters;

internal static partial class TextPatternCompiler
{
    private const RegexOptions MatchingOptions = RegexOptions.CultureInvariant | RegexOptions.NonBacktracking;

    internal static string Normalize(string pattern) =>
        pattern.StartsWith(@"\G", StringComparison.Ordinal) ? pattern[2..] : pattern;

    internal static Regex Compile(string anchored, string? validationPattern)
    {
        if (validationPattern is not null)
            _ = new Regex(validationPattern, MatchingOptions);
        return new Regex(anchored, MatchingOptions | RegexOptions.Compiled);
    }
}
