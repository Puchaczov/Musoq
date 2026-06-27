using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Gets the first N characters of the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="length">The length</param>
    /// <returns>First characters of string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Head(string? value, int? length = 10)
    {
        if (value == null)
            return null;

        if (length == null)
            return null;

        return value.Substring(0, length.Value);
    }

    /// <summary>
    ///     Gets the last N characters of the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="length">The length</param>
    /// <returns>Last characters of string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Tail(string? value, int? length = 10)
    {
        if (value == null)
            return null;

        if (length == null)
            return null;

        return value.Substring(value.Length - length.Value, length.Value);
    }

    /// <summary>
    ///     Gets the character at specified index
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="index">the index</param>
    /// <returns>Character based on index</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public char? GetCharacterOf(string value, int index)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length <= index || index < 0)
            return null;

        return value[index];
    }

    /// <summary>
    ///     Gets the nth word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="wordIndex">The wordIndex</param>
    /// <param name="separator">The separator</param>
    /// <returns>Nth word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetNthWord(string? text, int wordIndex, string? separator)
    {
        if (text == null || separator == null)
            return null;

        var split = text.Split(separator[0]);

        if (wordIndex >= split.Length)
            return null;

        return split[wordIndex];
    }

    /// <summary>
    ///     Gets the first word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="separator">The separator</param>
    /// <returns>First word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetFirstWord(string text, string separator)
    {
        return GetNthWord(text, 0, separator);
    }

    /// <summary>
    ///     Gets the second word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="separator">The separator</param>
    /// <returns>Second word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetSecondWord(string text, string separator)
    {
        return GetNthWord(text, 1, separator);
    }

    /// <summary>
    ///     Gets the third word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="separator">The separator</param>
    /// <returns>Third word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetThirdWord(string text, string separator)
    {
        return GetNthWord(text, 2, separator);
    }

    /// <summary>
    ///     Gets last word of the string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="separator">The separator</param>
    /// <returns>Last word</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? GetLastWord(string? text, string? separator)
    {
        if (text == null || separator == null)
            return null;

        var split = text.Split(separator[0]);

        return split[^1];
    }
}
