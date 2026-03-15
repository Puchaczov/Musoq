using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    // Email regex pattern based on RFC 5322 simplified version
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled);

    private static bool? ValidateNonEmptyString(string? value, Func<string, bool> validator)
    {
        if (value == null)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return validator(value);
    }

    /// <summary>
    ///     Determines whether the string is a valid email address format.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is a valid email format; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidEmail(string? value)
    {
        return ValidateNonEmptyString(value, EmailRegex.IsMatch);
    }

    /// <summary>
    ///     Determines whether the string is a valid URL format.
    ///     Supports http, https, ftp, and ftps schemes.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is a valid URL format; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidUrl(string? value)
    {
        return ValidateNonEmptyString(value, v =>
            Uri.TryCreate(v, UriKind.Absolute, out var uriResult) &&
            (uriResult.Scheme == Uri.UriSchemeHttp ||
             uriResult.Scheme == Uri.UriSchemeHttps ||
             uriResult.Scheme == Uri.UriSchemeFtp ||
             uriResult.Scheme == "ftps"));
    }

    /// <summary>
    ///     Determines whether the string is a valid absolute URI format.
    ///     Supports any URI scheme.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is a valid absolute URI format; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidUri(string? value)
    {
        return ValidateNonEmptyString(value, v => Uri.TryCreate(v, UriKind.Absolute, out _));
    }

    /// <summary>
    ///     Determines whether the string is valid JSON.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is valid JSON; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidJson(string? value)
    {
        return ValidateNonEmptyString(value, v =>
        {
            try
            {
                using var doc = JsonDocument.Parse(v);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        });
    }

    /// <summary>
    ///     Determines whether the string is well-formed XML.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is well-formed XML; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidXml(string? value)
    {
        return ValidateNonEmptyString(value, v =>
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(v);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        });
    }

    /// <summary>
    ///     Determines whether the string represents a valid GUID/UUID.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is a valid GUID; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidGuid(string? value)
    {
        return ValidateNonEmptyString(value, v => Guid.TryParse(v, out _));
    }

    /// <summary>
    ///     Determines whether the string represents a valid integer.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is a valid integer; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidInteger(string? value)
    {
        return ValidateNonEmptyString(value, v => long.TryParse(v, out _));
    }

    /// <summary>
    ///     Determines whether the string represents a valid decimal number.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is a valid decimal; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidDecimal(string? value)
    {
        return ValidateNonEmptyString(value, v => decimal.TryParse(v, out _));
    }

    /// <summary>
    ///     Determines whether the string represents a valid date/time.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is a valid date/time; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidDateTime(string? value)
    {
        return ValidateNonEmptyString(value, v => DateTimeOffset.TryParse(v, out _));
    }

    /// <summary>
    ///     Determines whether the string represents a valid IPv4 address.
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is a valid IPv4 address; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidIPv4(string? value)
    {
        if (value == null)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var parts = value.Split('.');
        if (parts.Length != 4)
            return false;

        foreach (var part in parts)
            if (!byte.TryParse(part, out _))
                return false;

        return true;
    }

    /// <summary>
    ///     Determines whether the string represents a valid boolean value.
    ///     Accepts: true, false, yes, no, 1, 0 (case-insensitive)
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if the string is a valid boolean representation; otherwise false. Returns null if input is null.</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Validation)]
    public bool? IsValidBoolean(string? value)
    {
        if (value == null)
            return null;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim().ToLowerInvariant();
        return trimmed is "true" or "false" or "yes" or "no" or "1" or "0";
    }
}
