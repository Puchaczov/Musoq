using System.Globalization;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Makes the string uppercase
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Uppercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToUpper(string value)
    {
        return ToUpper(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Makes the string uppercase within specified culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Uppercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToUpper(string value, string culture)
    {
        return ToUpper(value, CultureInfo.GetCultureInfo(culture));
    }

    /// <summary>
    ///     Makes the string uppercase
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Uppercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToUpperInvariant(string value)
    {
        return ToUpper(value, CultureInfo.InvariantCulture);
    }

    private static string? ToUpper(string? value, CultureInfo? culture)
    {
        if (value == null)
            return null;

        if (culture == null)
            return null;

        return value.ToUpper(culture);
    }

    /// <summary>
    ///     Makes the string lowercase
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Lowercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToLower(string value)
    {
        return ToLower(value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Makes the string lowercase within specified culture
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="culture">The culture</param>
    /// <returns>Lowercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToLower(string value, string culture)
    {
        return ToLower(value, CultureInfo.GetCultureInfo(culture));
    }

    /// <summary>
    ///     Makes the string lowercase
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Lowercased string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToLowerInvariant(string value)
    {
        return ToLower(value, CultureInfo.InvariantCulture);
    }

    private static string? ToLower(string? value, CultureInfo? culture)
    {
        if (value == null)
            return null;

        if (culture == null)
            return null;

        return value.ToLower(culture);
    }

    /// <summary>
    ///     Returns a new string that right-aligns the characters in this instance by padding them on the left with a specified
    ///     Unicode character, for a specified total lengt
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="character">The character</param>
    /// <param name="totalWidth">The total width</param>
    /// <returns>Left aligned value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? PadLeft(string? value, string? character, int? totalWidth)
    {
        if (value == null || character == null)
            return null;

        if (totalWidth == null)
            return null;

        return value.PadLeft(totalWidth.Value, character[0]);
    }

    /// <summary>
    ///     Returns a new string that left-aligns the characters in this instance by padding them on the right with a specified
    ///     Unicode character, for a specified total length
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="character">The character</param>
    /// <param name="totalWidth">The total width</param>
    /// <returns>Right aligned value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? PadRight(string? value, string? character, int? totalWidth)
    {
        if (value == null || character == null)
            return null;

        if (totalWidth == null)
            return null;

        return value.PadRight(totalWidth.Value, character[0]);
    }

    /// <summary>
    ///     Capitalizes the first letter of the string
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Capitalized text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToTitleCase(string? value)
    {
        if (value == null)
            return null;

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
    }

    /// <summary>
    ///     Capitalizes the first character of the string.
    /// </summary>
    /// <param name="value">The string to capitalize</param>
    /// <returns>The string with the first character in uppercase</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Capitalize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length == 1)
            return value.ToUpperInvariant();

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    /// <summary>
    ///     Pads a string on the left to the specified total length.
    /// </summary>
    /// <param name="value">The string to pad</param>
    /// <param name="totalWidth">The total desired width</param>
    /// <param name="paddingChar">The character to pad with (default: space)</param>
    /// <returns>The padded string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? PadLeft(string? value, int totalWidth, char paddingChar = ' ')
    {
        if (value == null)
            return null;

        return value.PadLeft(totalWidth, paddingChar);
    }

    /// <summary>
    ///     Pads a string on the right to the specified total length.
    /// </summary>
    /// <param name="value">The string to pad</param>
    /// <param name="totalWidth">The total desired width</param>
    /// <param name="paddingChar">The character to pad with (default: space)</param>
    /// <returns>The padded string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? PadRight(string? value, int totalWidth, char paddingChar = ' ')
    {
        if (value == null)
            return null;

        return value.PadRight(totalWidth, paddingChar);
    }

    /// <summary>
    ///     Removes diacritical marks (accents) from a string.
    /// </summary>
    /// <param name="value">The string to process</param>
    /// <returns>The string with diacritics removed</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? RemoveDiacritics(string? value)
    {
        if (value == null)
            return null;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
