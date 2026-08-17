namespace Musoq.Parser.Diagnostics;

internal readonly record struct OrdinaryStringEscapeRisk(
    string EscapeText,
    TextSpan Span,
    bool IsRootedPath,
    bool HasNonEscapeContent);

internal static class OrdinaryStringEscapeRiskDetector
{
    public static OrdinaryStringEscapeRisk? Find(ReadOnlySpan<char> innerText, int sourceStart)
    {
        var isRootedPath = IsRootedPath(innerText);
        var hasNonEscapeContent = HasNonEscapeContent(innerText);

        if (IsUnderEscapedNetworkPrefix(innerText))
            return new OrdinaryStringEscapeRisk(
                innerText[..2].ToString(),
                new TextSpan(sourceStart, 2),
                true,
                hasNonEscapeContent);

        for (var index = 0; index < innerText.Length; index++)
        {
            if (innerText[index] != '\\' || index + 1 >= innerText.Length)
                continue;

            var next = innerText[index + 1];
            if (next is '\\' or '\'')
            {
                index++;
                continue;
            }

            if (IsSimpleValueChangingEscape(next))
                return CreateRisk(innerText, sourceStart, index, 2, isRootedPath, hasNonEscapeContent);

            if (next is 'u' or 'x')
            {
                var digitLength = next == 'u' ? 4 : 2;
                if (HasValidFixedLengthEscape(innerText, index, digitLength))
                    return CreateRisk(innerText, sourceStart, index, 2 + digitLength, isRootedPath,
                        hasNonEscapeContent);

                index++;
            }
        }

        return null;
    }

    private static OrdinaryStringEscapeRisk CreateRisk(
        ReadOnlySpan<char> innerText,
        int sourceStart,
        int escapeStart,
        int escapeLength,
        bool isRootedPath,
        bool hasNonEscapeContent)
    {
        return new OrdinaryStringEscapeRisk(
            innerText.Slice(escapeStart, escapeLength).ToString(),
            new TextSpan(sourceStart + escapeStart, escapeLength),
            isRootedPath,
            hasNonEscapeContent);
    }

    private static bool IsSimpleValueChangingEscape(char value)
    {
        return value is '"' or 'n' or 'r' or 't' or 'b' or 'f' or 'e' or '0';
    }

    private static bool HasValidFixedLengthEscape(ReadOnlySpan<char> value, int start, int digitsLength)
    {
        if (value.Length - start - 2 < digitsLength)
            return false;

        for (var index = 0; index < digitsLength; index++)
            if (!IsHexDigit(value[start + 2 + index]))
                return false;

        return true;
    }

    private static bool HasNonEscapeContent(ReadOnlySpan<char> value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '\\' || index + 1 >= value.Length)
                return true;

            var next = value[index + 1];
            if (next is 'u' or 'x')
            {
                var digitLength = next == 'u' ? 4 : 2;
                if (HasValidFixedLengthEscape(value, index, digitLength))
                {
                    index += 1 + digitLength;
                    continue;
                }
            }

            if (IsKnownEscape(next))
            {
                index++;
                continue;
            }

            return true;
        }

        return false;
    }

    private static bool IsKnownEscape(char value)
    {
        return value is '\\' or '\'' or '"' or 'n' or 'r' or 't' or 'b' or 'f' or 'e' or '0';
    }

    private static bool IsRootedPath(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
            return false;

        if (value[0] is '\\' or '/')
            return true;

        if (value.Length >= 3 && IsAsciiLetter(value[0]) && value[1] == ':' && IsPathSeparator(value[2]))
            return true;

        return value.Length >= 2 && value[0] == '.' && IsPathSeparator(value[1]) ||
               value.Length >= 3 && value[0] == '.' && value[1] == '.' && IsPathSeparator(value[2]);
    }

    private static bool IsUnderEscapedNetworkPrefix(ReadOnlySpan<char> value)
    {
        return value.Length >= 2 && value[0] == '\\' && value[1] == '\\' &&
               (value.Length == 2 || value[2] != '\\');
    }

    private static bool IsPathSeparator(char value)
    {
        return value is '\\' or '/';
    }

    private static bool IsAsciiLetter(char value)
    {
        return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static bool IsHexDigit(char value)
    {
        return value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
    }
}
