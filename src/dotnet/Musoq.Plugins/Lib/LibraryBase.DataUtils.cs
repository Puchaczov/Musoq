using System.Collections.Generic;
using System.Linq;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{

    /// <summary>
    ///     Converts bytes to human readable size (e.g., "1.5 MB").
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
    ///     Converts seconds to human readable duration (e.g., "1h 30m 45s").
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
        var parts = new List<string>();

        if (ts.Days > 0) parts.Add($"{ts.Days}d");
        if (ts.Hours > 0) parts.Add($"{ts.Hours}h");
        if (ts.Minutes > 0) parts.Add($"{ts.Minutes}m");
        if (ts.Seconds > 0 || parts.Count == 0) parts.Add($"{ts.Seconds}s");

        return string.Join(" ", parts);
    }

    /// <summary>
    ///     Calculates the Shannon entropy of a string (measure of randomness).
    /// </summary>
    /// <param name="value">The string to analyze</param>
    /// <returns>The entropy value (higher = more random)</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.DataFormat)]
    public double? CalculateEntropy(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;


        var charCounts = new Dictionary<char, int>();
        foreach (var c in value)
            if (charCounts.TryGetValue(c, out var count))
                charCounts[c] = count + 1;
            else
                charCounts[c] = 1;

        var length = (double)value.Length;
        var entropy = 0.0;
        foreach (var count in charCounts.Values)
        {
            var frequency = count / length;
            entropy -= frequency * Math.Log2(frequency);
        }

        return entropy;
    }

    /// <summary>
    ///     Checks if a string appears to be valid Base64.
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
    ///     Checks if a string appears to be a valid hexadecimal string.
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

}
