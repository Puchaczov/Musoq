using System.Collections.Generic;
using System.Linq;
using System.Text;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    /// <summary>
    ///     Converts a string to Unicode escape sequences (e.g., "Hi" -> "\u0048\u0069").
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The Unicode-escaped string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToUnicodeEscape(string? value)
    {
        if (value == null)
            return null;

        var sb = new StringBuilder();
        foreach (var c in value) sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\\u{(int)c:X4}");
        return sb.ToString();
    }

    /// <summary>
    ///     Converts Unicode escape sequences back to regular text (e.g., "\u0048\u0069" -> "Hi").
    /// </summary>
    /// <param name="value">The Unicode-escaped string</param>
    /// <returns>The decoded string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? FromUnicodeEscape(string? value)
    {
        if (value == null)
            return null;

        try
        {
            return UnicodeEscapeRegex().Replace(value, m =>
                ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
        }
        catch
        {
            return value;
        }
    }

    /// <summary>
    ///     Applies ROT13 cipher to a string (rotates letters by 13 positions).
    /// </summary>
    /// <param name="value">The string to transform</param>
    /// <returns>The ROT13-transformed string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Rot13(string? value)
    {
        if (value == null)
            return null;

        var result = new char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c is >= 'a' and <= 'z')
                result[i] = (char)('a' + (c - 'a' + 13) % 26);
            else if (c is >= 'A' and <= 'Z')
                result[i] = (char)('A' + (c - 'A' + 13) % 26);
            else
                result[i] = c;
        }

        return new string(result);
    }

    /// <summary>
    ///     Applies ROT47 cipher to a string (rotates printable ASCII by 47 positions).
    /// </summary>
    /// <param name="value">The string to transform</param>
    /// <returns>The ROT47-transformed string</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? Rot47(string? value)
    {
        if (value == null)
            return null;

        var result = new char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c is >= '!' and <= '~')
                result[i] = (char)('!' + (c - '!' + 47) % 94);
            else
                result[i] = c;
        }

        return new string(result);
    }

    /// <summary>
    ///     Converts text to Morse code.
    /// </summary>
    /// <param name="value">The text to convert</param>
    /// <returns>The Morse code representation</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToMorse(string? value)
    {
        if (value == null)
            return null;

        var result = new List<string>();
        foreach (var c in value.ToUpperInvariant())
            if (MorseCodeMap.TryGetValue(c, out var morse))
                result.Add(morse);
        return string.Join(" ", result);
    }

    /// <summary>
    ///     Converts Morse code to text.
    /// </summary>
    /// <param name="value">The Morse code to convert (space-separated, / for word breaks)</param>
    /// <returns>The decoded text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? FromMorse(string? value)
    {
        if (value == null)
            return null;

        var sb = new StringBuilder();
        var codes = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var code in codes)
            if (ReverseMorseCodeMap.TryGetValue(code, out var c))
                sb.Append(c);
        return sb.ToString();
    }

    /// <summary>
    ///     Converts a string to its binary representation (space-separated bytes).
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>The binary string representation</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? ToBinaryString(string? value)
    {
        if (value == null)
            return null;

        var bytes = Encoding.UTF8.GetBytes(value);
        return string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
    }

    /// <summary>
    ///     Converts a binary string (space-separated bytes) back to text.
    /// </summary>
    /// <param name="value">The binary string to convert</param>
    /// <returns>The decoded text</returns>
    [BindableMethod]
    [MethodCategory(MethodCategories.String)]
    public string? FromBinaryString(string? value)
    {
        if (value == null)
            return null;

        try
        {
            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var bytes = parts.Select(p => Convert.ToByte(p, 2)).ToArray();
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }
}
