using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Truncates the string to the specified maximum length, optionally adding an ellipsis.
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">The maximum length of the result</param>
    /// <param name="ellipsis">The ellipsis to append when truncated (default is "...")</param>
    /// <returns>The truncated string, or the original string if shorter than maxLength</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Truncate(string? value, int maxLength, string ellipsis = "...")
    {
        if (value == null)
            return null;

        if (maxLength < 0)
            return string.Empty;

        if (value.Length <= maxLength)
            return value;

        var ellipsisLength = ellipsis?.Length ?? 0;

        if (maxLength <= ellipsisLength)
            return value.Substring(0, maxLength);

        return string.Concat(value.AsSpan(0, maxLength - ellipsisLength), ellipsis.AsSpan());
    }

    /// <summary>
    ///     Removes the specified prefix from the string if it starts with it.
    /// </summary>
    /// <param name="value">The string to process</param>
    /// <param name="prefix">The prefix to remove</param>
    /// <returns>The string without the prefix, or the original string if it doesn't start with the prefix</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RemovePrefix(string? value, string? prefix)
    {
        if (value == null)
            return null;

        if (string.IsNullOrEmpty(prefix))
            return value;

        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value.Substring(prefix.Length)
            : value;
    }

    /// <summary>
    ///     Removes the specified suffix from the string if it ends with it.
    /// </summary>
    /// <param name="value">The string to process</param>
    /// <param name="suffix">The suffix to remove</param>
    /// <returns>The string without the suffix, or the original string if it doesn't end with the suffix</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RemoveSuffix(string? value, string? suffix)
    {
        if (value == null)
            return null;

        if (string.IsNullOrEmpty(suffix))
            return value;

        return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? value.Substring(0, value.Length - suffix.Length)
            : value;
    }

    /// <summary>
    ///     Splits a string and returns the element at the specified index.
    /// </summary>
    /// <param name="value">The string to split</param>
    /// <param name="delimiter">The delimiter</param>
    /// <param name="index">The zero-based index of the element to return</param>
    /// <returns>The element at the specified index, or null if index is out of range</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? SplitAndTake(string? value, string? delimiter, int index)
    {
        if (value == null || delimiter == null)
            return null;

        var parts = value.Split(delimiter);
        if (index >= 0 && index < parts.Length)
            return parts[index];

        return null;
    }
}
