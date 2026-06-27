using System.Web;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Encodes the value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Url encoded value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? UrlEncode(string? value)
    {
        if (value == null)
            return null;

        return HttpUtility.UrlEncode(value);
    }

    /// <summary>
    ///     Decodes the value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Url decoded value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? UrlDecode(string? value)
    {
        if (value == null)
            return null;

        return HttpUtility.UrlDecode(value);
    }

    /// <summary>
    ///     Encodes the value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Uri encoded value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? UriEncode(string? value)
    {
        if (value == null)
            return null;

        return Uri.EscapeDataString(value);
    }

    /// <summary>
    ///     Decodes the value
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>Uri decoded value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? UriDecode(string? value)
    {
        if (value == null)
            return null;

        return Uri.UnescapeDataString(value);
    }

    /// <summary>
    ///     Encodes the value for safe use in HTML content.
    ///     Converts special characters like &lt;, &gt;, &amp;, ", ' to their HTML entity equivalents.
    /// </summary>
    /// <param name="value">The value to encode</param>
    /// <returns>HTML encoded value, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? HtmlEncode(string? value)
    {
        if (value == null)
            return null;

        return HttpUtility.HtmlEncode(value);
    }

    /// <summary>
    ///     Decodes HTML entities in the value back to their original characters.
    ///     Converts entities like &amp;lt;, &amp;gt;, &amp;amp;, &amp;quot; back to &lt;, &gt;, &amp;, ".
    /// </summary>
    /// <param name="value">The HTML encoded value to decode</param>
    /// <returns>Decoded value, or null if input is null</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? HtmlDecode(string? value)
    {
        if (value == null)
            return null;

        return HttpUtility.HtmlDecode(value);
    }
}
