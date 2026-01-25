using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Checks if an IP address is a private (RFC 1918) address.
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <returns>True if the IP is private</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public bool? IsPrivateIP(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return null;

        if (!IPAddress.TryParse(ipAddress, out var ip))
            return null;

        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
            return false;


        if (bytes[0] == 10)
            return true;


        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return true;


        if (bytes[0] == 192 && bytes[1] == 168)
            return true;


        if (bytes[0] == 127)
            return true;

        return false;
    }

    /// <summary>
    ///     Converts an IPv4 address to its numeric representation.
    /// </summary>
    /// <param name="ipAddress">The IP address</param>
    /// <returns>The numeric representation</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public long? IpToLong(string? ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return null;

        if (!IPAddress.TryParse(ipAddress, out var ip))
            return null;

        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
            return null;

        return ((long)bytes[0] << 24) | ((long)bytes[1] << 16) | ((long)bytes[2] << 8) | bytes[3];
    }

    /// <summary>
    ///     Converts a numeric IP representation back to dotted notation.
    /// </summary>
    /// <param name="ipNumber">The numeric IP representation</param>
    /// <returns>The IP address in dotted notation</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? LongToIp(long? ipNumber)
    {
        if (ipNumber is null or < 0 or > uint.MaxValue)
            return null;

        var num = (uint)ipNumber.Value;
        return $"{(num >> 24) & 0xFF}.{(num >> 16) & 0xFF}.{(num >> 8) & 0xFF}.{num & 0xFF}";
    }

    /// <summary>
    ///     Checks if an IP address is within a CIDR subnet.
    /// </summary>
    /// <param name="ipAddress">The IP address to check</param>
    /// <param name="cidr">The CIDR notation (e.g., "192.168.1.0/24")</param>
    /// <returns>True if the IP is in the subnet</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public bool? IsInSubnet(string? ipAddress, string? cidr)
    {
        if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(cidr))
            return null;

        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                return null;

            if (!IPAddress.TryParse(ipAddress, out var ip) ||
                !IPAddress.TryParse(parts[0], out var subnet) ||
                !int.TryParse(parts[1], out var prefixLength))
                return null;

            if (prefixLength is < 0 or > 32)
                return null;

            var ipLong = IpToLong(ipAddress);
            var subnetLong = IpToLong(parts[0]);

            if (!ipLong.HasValue || !subnetLong.HasValue)
                return null;

            var mask = prefixLength == 0 ? 0 : ~((1L << (32 - prefixLength)) - 1);
            return (ipLong.Value & mask) == (subnetLong.Value & mask);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Formats a MAC address with the specified separator.
    /// </summary>
    /// <param name="mac">The MAC address (can be with or without separators)</param>
    /// <param name="separator">The separator to use (default: ":")</param>
    /// <returns>The formatted MAC address</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? FormatMac(string? mac, string separator = ":")
    {
        if (string.IsNullOrEmpty(mac))
            return null;

        var clean = FormatMacRegex().Replace(mac, "");
        if (clean.Length != 12)
            return null;

        var parts = new string[6];
        for (var i = 0; i < 6; i++) parts[i] = clean.Substring(i * 2, 2).ToUpperInvariant();

        return string.Join(separator, parts);
    }

    /// <summary>
    ///     Generates a new random GUID.
    /// </summary>
    /// <returns>A new GUID as string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string NewGuid()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    ///     Generates a new random GUID without dashes.
    /// </summary>
    /// <returns>A new GUID as string without dashes</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string NewGuidCompact()
    {
        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    ///     Converts a value from one number base to another.
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="fromBase">The source base (2-36)</param>
    /// <param name="toBase">The target base (2-36)</param>
    /// <returns>The converted value</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public string? ConvertBase(string? value, int fromBase, int toBase)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (fromBase < 2 || fromBase > 36 || toBase < 2 || toBase > 36)
            return null;

        try
        {
            var number = Convert.ToInt64(value, fromBase);
            return ConvertToBase(number, toBase);
        }
        catch
        {
            return null;
        }
    }

    private static string ConvertToBase(long number, int toBase)
    {
        const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        if (number == 0) return "0";

        var negative = number < 0;
        number = Math.Abs(number);

        var result = new StringBuilder();
        while (number > 0)
        {
            result.Insert(0, chars[(int)(number % toBase)]);
            number /= toBase;
        }

        return negative ? "-" + result : result.ToString();
    }

    /// <summary>
    ///     Converts a Unix timestamp (seconds since epoch) to DateTime.
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp</param>
    /// <returns>The DateTime in UTC</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public DateTime? UnixToDateTime(long? unixTimestamp)
    {
        if (!unixTimestamp.HasValue)
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp.Value).UtcDateTime;
    }

    /// <summary>
    ///     Converts a Unix timestamp in milliseconds to DateTime.
    /// </summary>
    /// <param name="unixTimestampMs">The Unix timestamp in milliseconds</param>
    /// <returns>The DateTime in UTC</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public DateTime? UnixMillisToDateTime(long? unixTimestampMs)
    {
        if (!unixTimestampMs.HasValue)
            return null;

        return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestampMs.Value).UtcDateTime;
    }

    /// <summary>
    ///     Converts a DateTime to Unix timestamp (seconds since epoch).
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>The Unix timestamp</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public long? DateTimeToUnix(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        return new DateTimeOffset(dateTime.Value).ToUnixTimeSeconds();
    }

    /// <summary>
    ///     Converts a DateTime to Unix timestamp in milliseconds.
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>The Unix timestamp in milliseconds</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.Network)]
    public long? DateTimeToUnixMillis(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return null;

        return new DateTimeOffset(dateTime.Value).ToUnixTimeMilliseconds();
    }

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
        return value?.Replace("'", "''");
    }

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

    [GeneratedRegex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b")]
    private static partial Regex ExtractIpsRegex();

    [GeneratedRegex("-+")]
    private static partial Regex RemoveConsecutiveDashesRegex();

    [GeneratedRegex("[^0-9A-Fa-f]")]
    private static partial Regex FormatMacRegex();

    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase)]
    private static partial Regex ExtractUrlsRegex();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex ExtractEmailsRegex();
}
