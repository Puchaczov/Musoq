using System.Text.Json;
using System.Web;
using System.Xml.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    private static readonly JsonSerializerOptions IndentedJsonSerializerOptions = new() { WriteIndented = true };

    /// <summary>
    ///     Parses a query string and returns a specific parameter value.
    /// </summary>
    /// <param name="queryString">The query string (with or without leading ?)</param>
    /// <param name="paramName">The parameter name to retrieve</param>
    /// <returns>The parameter value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? GetQueryParam(string? queryString, string? paramName)
    {
        if (string.IsNullOrEmpty(queryString) || string.IsNullOrEmpty(paramName))
            return null;

        try
        {
            var query = queryString.TrimStart('?');
            var parsed = HttpUtility.ParseQueryString(query);
            return parsed[paramName];
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Parses a key-value string with specified delimiters.
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <param name="key">The key to find</param>
    /// <param name="pairDelimiter">The delimiter between key-value pairs (default: ampersand)</param>
    /// <param name="kvDelimiter">The delimiter between key and value (default: equals)</param>
    /// <returns>The value for the specified key</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? ParseKeyValue(string? value, string? key, string pairDelimiter = "&", string kvDelimiter = "=")
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key))
            return null;

        var pairs = value.Split(pairDelimiter, StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split(kvDelimiter, 2);
            if (parts.Length == 2 && parts[0].Trim() == key)
                return parts[1].Trim();
        }

        return null;
    }

    /// <summary>
    ///     Formats JSON with indentation for readability.
    /// </summary>
    /// <param name="json">The JSON string to format</param>
    /// <returns>The formatted JSON string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? FormatJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, IndentedJsonSerializerOptions);
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    ///     Minifies JSON by removing whitespace.
    /// </summary>
    /// <param name="json">The JSON string to minify</param>
    /// <returns>The minified JSON string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? MinifyJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement);
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    ///     Formats XML with indentation for readability.
    /// </summary>
    /// <param name="xml">The XML string to format</param>
    /// <returns>The formatted XML string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? FormatXml(string? xml)
    {
        if (string.IsNullOrEmpty(xml))
            return null;

        try
        {
            var doc = XDocument.Parse(xml);
            return doc.ToString();
        }
        catch
        {
            return xml;
        }
    }

    /// <summary>
    ///     Minifies XML by removing unnecessary whitespace.
    /// </summary>
    /// <param name="xml">The XML string to minify</param>
    /// <returns>The minified XML string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? MinifyXml(string? xml)
    {
        if (string.IsNullOrEmpty(xml))
            return null;

        try
        {
            var doc = XDocument.Parse(xml, LoadOptions.None);
            return doc.ToString(SaveOptions.DisableFormatting);
        }
        catch
        {
            return xml;
        }
    }
}
