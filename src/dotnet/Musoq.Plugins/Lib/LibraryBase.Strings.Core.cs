using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Gets the new identifier
    /// </summary>
    /// <returns>New identifier</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    [NonDeterministic]
    public string NewId()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    ///     Removes leading and trailing whitespace from a string.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Trim(string? value)
    {
        return value?.Trim();
    }

    /// <summary>
    ///     Removes leading whitespace from a string.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? TrimStart(string? value)
    {
        return value?.TrimStart();
    }

    /// <summary>
    ///     Removes trailing whitespace from a string.
    /// </summary>
    /// <param name="value">The string to trim.</param>
    /// <returns>The trimmed string.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? TrimEnd(string? value)
    {
        return value?.TrimEnd();
    }

    /// <summary>
    ///     Gets the substring from the string.
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="index">The index</param>
    /// <param name="length">The length</param>
    /// <returns>Substring of a string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Substring(string? value, int? index, int? length)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (length < 1)
            return string.Empty;

        if (index == null || length == null)
            return null;

        var valueLastIndex = value.Length - 1;
        var computedLastIndex = index + (length - 1);

        if (valueLastIndex < computedLastIndex)
            length = value.Length - 1 - index + 1;

        return length is null ? null : value.Substring(index.Value, length.Value);
    }

    /// <summary>
    ///     Gets the substring from the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="length">The length</param>
    /// <returns>Substring of a string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Substring(string? value, int? length)
    {
        return Substring(value, 0, length);
    }

    /// <summary>
    ///     Concatenates the specified values
    /// </summary>
    /// <param name="strings">The strings</param>
    /// <returns>Concatenated values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat(params string?[]? strings)
    {
        return strings == null ? null : ConcatCore(strings);
    }

    /// <summary>
    ///     Concatenates the specified characters
    /// </summary>
    /// <param name="characters">The characters</param>
    /// <returns>Concatenated characters</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat(params char[]? characters)
    {
        return characters == null ? null : ConcatCore(characters);
    }

    /// <summary>
    ///     Concatenates specified string first characters
    /// </summary>
    /// <param name="firstString">The string</param>
    /// <param name="chars">The characters</param>
    /// <returns>Concatenated string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat(string? firstString, params char[]? chars)
    {
        if (firstString == null || chars == null)
            return null;

        var sb = new StringBuilder();
        sb.Append(firstString);

        foreach (var value in chars)
            sb.Append(value);

        return sb.ToString();
    }

    /// <summary>
    ///     Concatenate specific character with strings
    /// </summary>
    /// <param name="firstChar">The character</param>
    /// <param name="strings">The strings</param>
    /// <returns>Concatenated string</returns>
    public string? Concat(char? firstChar, params string[]? strings)
    {
        if (firstChar == null || strings == null)
            return null;

        var sb = new StringBuilder();
        sb.Append(firstChar);

        foreach (var value in strings)
            sb.Append(value);

        return sb.ToString();
    }

    /// <summary>
    ///     Concatenates the specified strings
    /// </summary>
    /// <param name="objects">The objects</param>
    /// <returns>Concatenated string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat(params object?[]? objects)
    {
        return objects == null ? null : ConcatCore(objects);
    }

    /// <summary>
    ///     Concatenates the specified strings
    /// </summary>
    /// <param name="objects">The objects</param>
    /// <returns>Concatenated string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Concat<T>(params T?[]? objects)
    {
        return objects == null ? null : ConcatCore(objects);
    }

    private static string ConcatCore<T>(T[] values)
    {
        var sb = new StringBuilder();

        foreach (var value in values)
            sb.Append(value);

        return sb.ToString();
    }

    /// <summary>
    ///     Splits the string into an array of substrings based on the specified separators
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="separators">The separators</param>
    /// <returns>Separated values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string[] Split(string? value, params string[] separators)
    {
        if (value == null)
            return [];

        return value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    ///     Splits the string into an array of characters
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Array of characters</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public char[] ToCharArray(string? value)
    {
        if (value == null)
            return [];

        return value.ToCharArray();
    }

    /// <summary>
    ///     Determines whether the string is null or empty
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>True if null or empty; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool IsNullOrEmpty(string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    ///     Determines whether the string is null or whitespace
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>True if null or whitespace; otherwise false</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public bool IsNullOrWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    ///     Joins the specified values with the separator
    /// </summary>
    /// <param name="separator">The separator</param>
    /// <param name="values">The values</param>
    /// <returns>Joined values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? StringsJoin(string? separator, params string?[]? values)
    {
        if (separator is null)
            return null;

        if (values is null)
            return null;

        return string.Join(separator, values.Where(str => str != null));
    }

    /// <summary>
    ///     Joins the specified values with the separator
    /// </summary>
    /// <param name="separator">The separator</param>
    /// <param name="values">The values</param>
    /// <returns>Joined values</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? StringsJoin(string? separator, IEnumerable<string?>? values)
    {
        if (separator is null)
            return null;

        if (values is null)
            return null;

        return string.Join(separator, values.Where(str => str != null));
    }
}
