using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Web;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    /// Decodes a JWT token and returns the payload as JSON string.
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The decoded payload as JSON string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? JwtDecode(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return null;

            var payload = parts[1];
            
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the header portion of a JWT token as JSON string.
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The decoded header as JSON string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? JwtGetHeader(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 1)
                return null;

            var header = parts[0];
            header = header.Replace('-', '+').Replace('_', '/');
            switch (header.Length % 4)
            {
                case 2: header += "=="; break;
                case 3: header += "="; break;
            }

            var bytes = Convert.FromBase64String(header);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a specific claim from a JWT token payload.
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <param name="claimName">The name of the claim to retrieve</param>
    /// <returns>The claim value as string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? JwtGetClaim(string? token, string? claimName)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(claimName))
            return null;

        var payload = JwtDecode(token);
        if (payload == null)
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty(claimName, out var claim))
            {
                return claim.ValueKind == JsonValueKind.String 
                    ? claim.GetString() 
                    : claim.GetRawText();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses a query string and returns a specific parameter value.
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
    /// Parses a key-value string with specified delimiters.
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
    /// Formats JSON with indentation for readability.
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
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Minifies JSON by removing whitespace.
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
    /// Formats XML with indentation for readability.
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
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            return doc.ToString();
        }
        catch
        {
            return xml;
        }
    }

    /// <summary>
    /// Minifies XML by removing unnecessary whitespace.
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
            var doc = System.Xml.Linq.XDocument.Parse(xml, System.Xml.Linq.LoadOptions.None);
            return doc.ToString(System.Xml.Linq.SaveOptions.DisableFormatting);
        }
        catch
        {
            return xml;
        }
    }

    /// <summary>
    /// Converts bytes to human readable size (e.g., "1.5 MB").
    /// </summary>
    /// <param name="bytes">The number of bytes</param>
    /// <returns>Human readable size string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? ToHumanReadableSize(long? bytes)
    {
        if (!bytes.HasValue)
            return null;

        string[] suffixes = ["B", "KB", "MB", "GB", "TB", "PB"];
        var size = (double)bytes.Value;
        var suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return suffixIndex == 0 
            ? $"{size:0} {suffixes[suffixIndex]}" 
            : $"{size:0.##} {suffixes[suffixIndex]}";
    }

    /// <summary>
    /// Converts seconds to human readable duration (e.g., "1h 30m 45s").
    /// </summary>
    /// <param name="seconds">The number of seconds</param>
    /// <returns>Human readable duration string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public string? ToHumanReadableDuration(long? seconds)
    {
        if (!seconds.HasValue)
            return null;

        var ts = TimeSpan.FromSeconds(seconds.Value);
        var parts = new System.Collections.Generic.List<string>();

        if (ts.Days > 0) parts.Add($"{ts.Days}d");
        if (ts.Hours > 0) parts.Add($"{ts.Hours}h");
        if (ts.Minutes > 0) parts.Add($"{ts.Minutes}m");
        if (ts.Seconds > 0 || parts.Count == 0) parts.Add($"{ts.Seconds}s");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Calculates the Shannon entropy of a string (measure of randomness).
    /// </summary>
    /// <param name="value">The string to analyze</param>
    /// <returns>The entropy value (higher = more random)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public double? CalculateEntropy(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var frequencies = value.GroupBy(c => c)
            .Select(g => (double)g.Count() / value.Length)
            .ToList();

        return -frequencies.Sum(f => f * Math.Log2(f));
    }

    /// <summary>
    /// Checks if a string appears to be valid Base64.
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string appears to be valid Base64</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public bool? IsBase64(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (value.Length % 4 != 0)
            return false;

        try
        {
            _ = Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a string appears to be a valid hexadecimal string.
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string is valid hex</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public bool? IsHex(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
    }

    /// <summary>
    /// Checks if a string appears to be a valid JWT token.
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if the string appears to be a valid JWT</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public bool? IsJwt(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var parts = value.Split('.');
        if (parts.Length != 3)
            return false;

        return JwtGetHeader(value) != null && JwtDecode(value) != null;
    }
}
