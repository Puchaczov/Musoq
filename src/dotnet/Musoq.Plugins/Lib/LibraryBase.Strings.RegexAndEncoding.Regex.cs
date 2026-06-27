using System.Collections.Generic;
using System.Text.RegularExpressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Replace the specified value part that matches the pattern with the replacement
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="pattern">The pattern</param>
    /// <param name="replacement">The replacement</param>
    /// <returns>Replaced value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RegexReplace(string? value, string? pattern, string? replacement)
    {
        if (value == null || pattern == null || replacement == null)
            return null;

        var compiledRegex = StringRegexCache.GetOrAdd(pattern, p =>
            new Regex(p, RegexOptions.Compiled));

        return compiledRegex.Replace(value, replacement);
    }

    /// <summary>
    ///     Returns all matching strings based on regular expression pattern
    /// </summary>
    /// <param name="regex">The regular expression pattern</param>
    /// <param name="content">The content to search in</param>
    /// <returns>Array of matching strings, or null if either parameter is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[]? RegexMatches(string? regex, string? content)
    {
        if (regex == null || content == null)
            return null;

        var compiledRegex = StringRegexCache.GetOrAdd(regex, p =>
            new Regex(p, RegexOptions.Compiled));

        var matches = compiledRegex.Matches(content);
        var result = new string[matches.Count];
        for (var i = 0; i < matches.Count; i++)
            result[i] = matches[i].Value;
        return result;
    }

    /// <summary>
    ///     Extracts the first match of a regex capture group from the string.
    /// </summary>
    /// <param name="value">The string to search in</param>
    /// <param name="pattern">The regex pattern with capture groups</param>
    /// <param name="groupIndex">The capture group index (0 = whole match, 1+ = capture groups)</param>
    /// <returns>The matched group text, or null if no match</returns>
    /// <example>
    ///     RegexExtract("Hello 123 World", @"(\d+)", 1) returns "123"
    ///     RegexExtract("test@example.com", @"(\w+)@(\w+)\.(\w+)", 2) returns "example"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RegexExtract(string? value, string? pattern, int groupIndex = 0)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
            return null;

        try
        {
            var regex = StringRegexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled));
            var match = regex.Match(value);

            if (!match.Success)
                return null;

            if (groupIndex < 0 || groupIndex >= match.Groups.Count)
                return null;

            return match.Groups[groupIndex].Value;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Extracts all matches of a regex capture group from the string.
    /// </summary>
    /// <param name="value">The string to search in</param>
    /// <param name="pattern">The regex pattern with capture groups</param>
    /// <param name="groupIndex">The capture group index (0 = whole match, 1+ = capture groups)</param>
    /// <returns>Array of matched group texts</returns>
    /// <example>
    ///     RegexExtractAll("a1b2c3", @"(\d)", 1) returns ["1", "2", "3"]
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[] RegexExtractAll(string? value, string? pattern, int groupIndex = 0)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
            return [];

        try
        {
            var regex = StringRegexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled));
            var matches = regex.Matches(value);
            var results = new List<string>();

            foreach (Match match in matches)
                if (groupIndex >= 0 && groupIndex < match.Groups.Count)
                    results.Add(match.Groups[groupIndex].Value);

            return results.ToArray();
        }
        catch (ArgumentException)
        {
            return [];
        }
    }

    /// <summary>
    ///     Checks if the string matches the specified regex pattern.
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <param name="pattern">The regex pattern</param>
    /// <returns>True if the string matches the pattern; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool? IsMatch(string? value, string? pattern)
    {
        if (value == null || pattern == null)
            return null;

        try
        {
            var regex = StringRegexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.Compiled));
            return regex.IsMatch(value);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
