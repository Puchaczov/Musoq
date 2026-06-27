using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts a string to a URL-friendly slug.
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The slugified string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? ToSlug(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
            else if (c is ' ' or '-' or '_')
                sb.Append('-');
        }


        var result = RemoveConsecutiveDashesRegex().Replace(sb.ToString(), "-").Trim('-');
        return result;
    }

    /// <summary>
    ///     Escapes a string for use in a regular expression.
    /// </summary>
    /// <param name="value">The string to escape</param>
    /// <returns>The escaped string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? EscapeRegex(string? value)
    {
        return value == null ? null : Regex.Escape(value);
    }

    /// <summary>
    ///     Escapes single quotes for SQL (doubles them).
    /// </summary>
    /// <param name="value">The string to escape</param>
    /// <returns>The escaped string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? EscapeSql(string? value)
    {
        return value?.Replace("'", "''", StringComparison.Ordinal);
    }
}
