using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Musoq.Parser.Helpers;

/// <summary>
/// Provides methods for handling string escape sequences.
/// </summary>
public static class EscapeHelpers
{
    private static readonly System.Buffers.SearchValues<char> EscapeCharsValues = System.Buffers.SearchValues.Create("\\\'\"\n\r\t\b\f\u001B\0");
    
    private const char EscapeChar = '\\';
    private const char UnicodePrefix = 'u';
    private const char HexPrefix = 'x';
    
    private const int UnicodeEscapeLength = 4;
    private const int HexEscapeLength = 2;
    
    private static readonly (char escapeChar, char value)[] EscapeMap =
    [
        ('\\', '\\'),
        ('\'', '\''),
        ('"', '"'),
        ('n', '\n'),
        ('r', '\r'),
        ('t', '\t'),
        ('b', '\b'),
        ('f', '\f'),
        ('e', '\u001B'),
        ('0', '\0')
    ];

    /// <summary>
    /// Unescapes a string containing escape sequences.
    /// </summary>
    /// <param name="escaped">The string containing escape sequences.</param>
    /// <returns>The unescaped string.</returns>
    /// <exception cref="ArgumentException">Thrown when encountering malformed Unicode or hex escape sequences if strict mode is enabled.</exception>
    public static string Unescape(this string escaped)
    {
        if (string.IsNullOrEmpty(escaped))
            return escaped;

        if (!escaped.AsSpan().Contains(EscapeChar))
            return escaped;

        return UnescapeInternal(escaped);
    }

    /// <summary>
    /// Escapes a string by adding necessary escape sequences.
    /// </summary>
    /// <param name="unescaped">The string to escape.</param>
    /// <returns>The escaped string.</returns>
    public static string Escape(this string unescaped)
    {
        if (string.IsNullOrEmpty(unescaped))
            return unescaped;

        if (!NeedsEscaping(unescaped))
            return unescaped;

        return EscapeInternal(unescaped);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool NeedsEscaping(string value)
    {
        return value.AsSpan().ContainsAny(EscapeCharsValues);
    }

    private static string UnescapeInternal(string escaped)
    {
        var result = new StringBuilder(Math.Max(escaped.Length * 2 / 3, 16));
        var span = escaped.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] != EscapeChar || i + 1 >= span.Length)
            {
                result.Append(span[i]);
                continue;
            }

            var next = span[i + 1];

            switch (next)
            {
                // Handle Unicode and Hex escapes first
                case UnicodePrefix when TryParseUnicodeEscape(span, i, out var unicodeChar):
                {
                    result.Append(unicodeChar);
                    i += 1 + UnicodeEscapeLength;
                    continue;
                }
                // Invalid unicode sequence - preserve the entire sequence
                case UnicodePrefix:
                {
                    result.Append(EscapeChar).Append(next);
                    var remainingLength = Math.Min(UnicodeEscapeLength, span.Length - (i + 2));
                    if (remainingLength > 0)
                    {
                        result.Append(span.Slice(i + 2, remainingLength));
                        i += 1 + remainingLength;
                    }
                    else
                    {
                        i++;
                    }
                    continue;
                }
                case HexPrefix when TryParseHexEscape(span, i, out var hexChar):
                    result.Append(hexChar);
                    i += 1 + HexEscapeLength;
                    continue;
                // Invalid hex sequence - preserve the entire sequence
                case HexPrefix:
                {
                    result.Append(EscapeChar).Append(next);
                    var remainingLength = Math.Min(HexEscapeLength, span.Length - (i + 2));
                    if (remainingLength > 0)
                    {
                        result.Append(span.Slice(i + 2, remainingLength));
                        i += 1 + remainingLength;
                    }
                    else
                    {
                        i++;
                    }
                    continue;
                }
            }

            // Handle simple escapes
            var found = false;
            foreach (var (escapeChar, value) in EscapeMap)
            {
                if (next != escapeChar) continue;
                
                result.Append(value);
                i++;
                found = true;
                break;
            }

            if (found) continue;
            
            result.Append(EscapeChar).Append(next);
            i++;
        }

        return result.ToString();
    }

    private static string EscapeInternal(string unescaped)
    {
        var result = new StringBuilder(Math.Max(unescaped.Length * 3 / 2, 16));
        var span = unescaped.AsSpan();

        foreach (var current in span)
        {
            var found = false;

            foreach (var (escapeChar, value) in EscapeMap)
            {
                if (current != value) continue;
                
                result.Append(EscapeChar).Append(escapeChar);
                found = true;
                break;
            }

            if (found) continue;
            
            if (char.IsControl(current))
            {
                result.Append(EscapeChar)
                    .Append(UnicodePrefix)
                    .Append(((int)current).ToString("X4"));
            }
            else
            {
                result.Append(current);
            }
        }

        return result.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseUnicodeEscape(ReadOnlySpan<char> value, int startIndex, out char result)
    {
        result = '\0';
    
        if (startIndex + 1 + UnicodeEscapeLength >= value.Length)
            return false;

        var unicodeSpan = value.Slice(startIndex + 2, UnicodeEscapeLength);
        if (!int.TryParse(unicodeSpan, NumberStyles.HexNumber, null, out var unicodeChar) 
            || unicodeChar > char.MaxValue)
            return false;

        if (unicodeChar == 0)
        {
            result = '\0';
            return true;
        }

        result = (char)unicodeChar;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryParseHexEscape(ReadOnlySpan<char> value, int startIndex, out char result)
    {
        result = '\0';
        
        if (startIndex + 1 + HexEscapeLength >= value.Length)
            return false;

        var hexSpan = value.Slice(startIndex + 2, HexEscapeLength);
        return int.TryParse(hexSpan, NumberStyles.HexNumber, null, out var hexChar) 
               && hexChar <= char.MaxValue 
               && (result = (char)hexChar) != '\0';
    }
}