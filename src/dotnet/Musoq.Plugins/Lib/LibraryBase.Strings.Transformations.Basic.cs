using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Reverses the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Reversed string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Reverse(string? value)
    {
        if (value == null)
            return null;

        if (value.Length == 0)
            return value;

        if (value.Length == 1)
            return value;

        var length = value.Length;
        if (length <= 256)
        {
            Span<char> buffer = stackalloc char[length];
            for (var i = 0; i < length; i++)
                buffer[i] = value[length - 1 - i];
            return new string(buffer);
        }
        else
        {
            var buffer = new char[length];
            for (var i = 0; i < length; i++)
                buffer[i] = value[length - 1 - i];
            return new string(buffer);
        }
    }

    /// <summary>
    ///     Clones the value n times
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="count">The repeat count</param>
    /// <returns>Cloned value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string Replicate(string value, int count)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < count; ++i)
            builder.Append(value);

        return builder.ToString();
    }

    /// <summary>
    ///     Returns the string from the first argument after the characters specified in the second argument are translated
    ///     into the characters specified in the third argument.
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="characters">The characters</param>
    /// <param name="translations">The translations</param>
    /// <returns>Translated value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Translate(string? value, string? characters, string? translations)
    {
        if (value == null)
            return null;

        if (characters == null || translations == null)
            return null;

        if (characters.Length != translations.Length)
            return null;

        var builder = new StringBuilder();

        foreach (var character in value)
        {
            var index = characters.IndexOf(character, StringComparison.Ordinal);

            builder.Append(index == -1 ? character : translations[index]);
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Replaces the first occurrence of a specified string in this instance with another specified string
    /// </summary>
    /// <param name="text">The text</param>
    /// <param name="lookFor">The lookFor</param>
    /// <param name="changeTo">The changeTo</param>
    /// <returns>Changed value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Replace(string? text, string lookFor, string? changeTo)
    {
        if (text == null)
            return null;

        if (string.IsNullOrEmpty(lookFor))
            return text;

        if (changeTo == null)
            return text;

        return text.Replace(lookFor, changeTo, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Removes all whitespace characters from the string.
    /// </summary>
    /// <param name="value">The string to process</param>
    /// <returns>The string without any whitespace, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RemoveWhitespace(string? value)
    {
        if (value == null)
            return null;

        var result = new StringBuilder(value.Length);

        foreach (var c in value)
            if (!char.IsWhiteSpace(c))
                result.Append(c);

        return result.ToString();
    }

    /// <summary>
    ///     Returns the string repeated the specified number of times with a separator.
    /// </summary>
    /// <param name="value">The string to repeat</param>
    /// <param name="count">The number of times to repeat</param>
    /// <param name="separator">The separator between repetitions</param>
    /// <returns>The repeated string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Repeat(string? value, int count, string separator = "")
    {
        if (value == null)
            return null;

        if (count <= 0)
            return string.Empty;

        if (count == 1)
            return value;

        var result = new StringBuilder();

        for (var i = 0; i < count; i++)
        {
            if (i > 0 && separator != null)
                result.Append(separator);
            result.Append(value);
        }

        return result.ToString();
    }

    /// <summary>
    ///     Wraps the string with the specified prefix and suffix.
    /// </summary>
    /// <param name="value">The string to wrap</param>
    /// <param name="prefix">The prefix to add</param>
    /// <param name="suffix">The suffix to add</param>
    /// <returns>The wrapped string, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Wrap(string? value, string? prefix, string? suffix)
    {
        if (value == null)
            return null;

        return (prefix ?? string.Empty) + value + (suffix ?? string.Empty);
    }
}
