using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Extracts all URLs from a string.
    /// </summary>
    /// <param name="value">The string to search</param>
    /// <returns>Comma-separated list of URLs</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? ExtractUrls(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var matches = ExtractUrlsRegex().Matches(value);

        if (matches.Count == 0)
            return string.Empty;

        return string.Join(",", matches.Select(m => m.Value));
    }

    /// <summary>
    ///     Extracts all email addresses from a string.
    /// </summary>
    /// <param name="value">The string to search</param>
    /// <returns>Comma-separated list of emails</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? ExtractEmails(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var matches = ExtractEmailsRegex().Matches(value);

        if (matches.Count == 0)
            return string.Empty;

        return string.Join(",", matches.Select(m => m.Value));
    }

    /// <summary>
    ///     Extracts all IPv4 addresses from a string.
    /// </summary>
    /// <param name="value">The string to search</param>
    /// <returns>Comma-separated list of IP addresses</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? ExtractIPs(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var matches = ExtractIpsRegex().Matches(value);

        return matches.Count == 0 ? string.Empty : string.Join(",", matches.Select(m => m.Value));
    }
}
