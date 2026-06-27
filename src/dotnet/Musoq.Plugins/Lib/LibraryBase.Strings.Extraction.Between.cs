using System.Collections.Generic;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Extracts text between the first occurrence of the start delimiter and the first occurrence of the end delimiter
    ///     after it.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="startDelimiter">The starting delimiter (character or substring)</param>
    /// <param name="endDelimiter">The ending delimiter (character or substring)</param>
    /// <returns>The extracted text between delimiters, or null if delimiters are not found</returns>
    /// <example>
    ///     ExtractBetween("Hello [World] Test", "[", "]") returns "World"
    ///     ExtractBetween("&lt;tag&gt;content&lt;/tag&gt;", "&lt;tag&gt;", "&lt;/tag&gt;") returns "content"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ExtractBetween(string? value, string? startDelimiter, string? endDelimiter)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(startDelimiter) || string.IsNullOrEmpty(endDelimiter))
            return null;

        var startIndex = value.IndexOf(startDelimiter, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        var contentStart = startIndex + startDelimiter.Length;
        var endIndex = value.IndexOf(endDelimiter, contentStart, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;

        return value.Substring(contentStart, endIndex - contentStart);
    }

    /// <summary>
    ///     Extracts all occurrences of text between the start and end delimiters.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="startDelimiter">The starting delimiter (character or substring)</param>
    /// <param name="endDelimiter">The ending delimiter (character or substring)</param>
    /// <returns>An array of all extracted texts between delimiters</returns>
    /// <example>
    ///     ExtractBetweenAll("a]b] test", "[", "]") returns ["a", "b"]
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[] ExtractBetweenAll(string? value, string? startDelimiter, string? endDelimiter)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(startDelimiter) || string.IsNullOrEmpty(endDelimiter))
            return [];

        var results = new List<string>();
        var currentIndex = 0;

        while (currentIndex < value.Length)
        {
            var startIndex = value.IndexOf(startDelimiter, currentIndex, StringComparison.Ordinal);
            if (startIndex == -1)
                break;

            var contentStart = startIndex + startDelimiter.Length;
            var endIndex = value.IndexOf(endDelimiter, contentStart, StringComparison.Ordinal);
            if (endIndex == -1)
                break;

            results.Add(value.Substring(contentStart, endIndex - contentStart));
            currentIndex = endIndex + endDelimiter.Length;
        }

        return results.ToArray();
    }

    /// <summary>
    ///     Extracts text between the first occurrence of the start delimiter and the first occurrence of the end delimiter,
    ///     including the delimiters themselves in the result.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="startDelimiter">The starting delimiter (character or substring)</param>
    /// <param name="endDelimiter">The ending delimiter (character or substring)</param>
    /// <returns>The extracted text including delimiters, or null if delimiters are not found</returns>
    /// <example>
    ///     ExtractBetweenIncluding("Hello [World] Test", "[", "]") returns "[World]"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ExtractBetweenIncluding(string? value, string? startDelimiter, string? endDelimiter)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(startDelimiter) || string.IsNullOrEmpty(endDelimiter))
            return null;

        var startIndex = value.IndexOf(startDelimiter, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        var endIndex = value.IndexOf(endDelimiter, startIndex + startDelimiter.Length, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;

        return value.Substring(startIndex, endIndex - startIndex + endDelimiter.Length);
    }

    /// <summary>
    ///     Extracts text from the first occurrence of the start delimiter to the end of the string.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="startDelimiter">The starting delimiter (character or substring)</param>
    /// <param name="includeDelimiter">Whether to include the delimiter in the result</param>
    /// <returns>The extracted text from the delimiter to the end, or null if delimiter is not found</returns>
    /// <example>
    ///     ExtractAfter("Hello World Test", "World", false) returns " Test"
    ///     ExtractAfter("Hello World Test", "World", true) returns "World Test"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ExtractAfter(string? value, string? startDelimiter, bool includeDelimiter = false)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(startDelimiter))
            return null;

        var startIndex = value.IndexOf(startDelimiter, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        return includeDelimiter
            ? value.Substring(startIndex)
            : value.Substring(startIndex + startDelimiter.Length);
    }

    /// <summary>
    ///     Extracts text from the beginning of the string up to the first occurrence of the end delimiter.
    /// </summary>
    /// <param name="value">The string to extract from</param>
    /// <param name="endDelimiter">The ending delimiter (character or substring)</param>
    /// <param name="includeDelimiter">Whether to include the delimiter in the result</param>
    /// <returns>The extracted text from the beginning to the delimiter, or null if delimiter is not found</returns>
    /// <example>
    ///     ExtractBefore("Hello World Test", "World", false) returns "Hello "
    ///     ExtractBefore("Hello World Test", "World", true) returns "Hello World"
    /// </example>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ExtractBefore(string? value, string? endDelimiter, bool includeDelimiter = false)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(endDelimiter))
            return null;

        var endIndex = value.IndexOf(endDelimiter, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;

        return includeDelimiter
            ? value.Substring(0, endIndex + endDelimiter.Length)
            : value.Substring(0, endIndex);
    }
}
