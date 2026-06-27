using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    private static readonly ConcurrentDictionary<string, Regex> MatchRegexCache = new();

    /// <summary>
    ///     Determine whether content matches the specified pattern
    /// </summary>
    /// <param name="regex">The regex</param>
    /// <param name="content">The content</param>
    /// <returns>True if matches, otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Utility)]
    public bool? Match(string? regex, string? content)
    {
        if (regex == null || content == null)
            return null;

        var compiledRegex = MatchRegexCache.GetOrAdd(regex, pattern =>
            new Regex(pattern, RegexOptions.Compiled));

        return compiledRegex.IsMatch(content);
    }
}
