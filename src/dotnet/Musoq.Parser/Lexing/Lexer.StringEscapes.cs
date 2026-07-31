using Musoq.Parser.Diagnostics;

namespace Musoq.Parser.Lexing;

public sealed partial class Lexer
{
    private static bool IsHexDigit(char value)
    {
        return value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }

    private static bool TryFindInvalidEscapeSequence(ReadOnlySpan<char> value, out string invalidEscape,
        out TextSpan span)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\')
                continue;

            if (i + 1 >= value.Length)
            {
                invalidEscape = "\\";
                span = new TextSpan(i, 1);
                return true;
            }

            var next = value[i + 1];
            if (IsSimpleEscape(next))
            {
                i++;
                continue;
            }

            if (next == 'u')
                return TryValidateFixedLengthEscape(value, i, 4, out invalidEscape, out span);

            if (next == 'x')
                return TryValidateFixedLengthEscape(value, i, 2, out invalidEscape, out span);

            i++;
        }

        invalidEscape = string.Empty;
        span = TextSpan.Empty;
        return false;
    }

    private static bool TryValidateFixedLengthEscape(ReadOnlySpan<char> value, int start, int digitsLength,
        out string invalidEscape, out TextSpan span)
    {
        var availableDigits = Math.Min(digitsLength, value.Length - (start + 2));
        if (availableDigits == 0)
        {
            invalidEscape = string.Empty;
            span = TextSpan.Empty;
            return false;
        }

        if (availableDigits < digitsLength)
        {
            var invalidLength = Math.Min(2 + availableDigits, value.Length - start);
            invalidEscape = value.Slice(start, invalidLength).ToString();
            span = new TextSpan(start, invalidLength);
            return true;
        }

        for (var i = 0; i < digitsLength; i++)
        {
            if (Uri.IsHexDigit(value[start + 2 + i]))
                continue;

            invalidEscape = value.Slice(start, 2 + digitsLength).ToString();
            span = new TextSpan(start, 2 + digitsLength);
            return true;
        }

        invalidEscape = string.Empty;
        span = TextSpan.Empty;
        return false;
    }

    private static bool IsSimpleEscape(char value)
    {
        return value is '\\' or '\'' or '"' or 'n' or 'r' or 't' or 'b' or 'f' or 'e' or '0';
    }
}
