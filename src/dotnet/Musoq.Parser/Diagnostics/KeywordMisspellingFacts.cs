using Musoq.Parser.Tokens;

namespace Musoq.Parser.Diagnostics;

internal static class KeywordMisspellingFacts
{
    private static readonly string[] FromKeyword = ["FROM"];

    public static bool IsLikelyMisspelledFromKeyword(Token token, string input)
    {
        if (token.TokenType is not (TokenType.Word or TokenType.Identifier) ||
            string.IsNullOrWhiteSpace(token.Value) ||
            token.Value.Equals("FROM", StringComparison.OrdinalIgnoreCase))
            return false;

        var suggestion = ErrorCatalog.GetDidYouMeanSuggestion(token.Value, FromKeyword, maxDistance: 2);
        return suggestion != null && !IsFollowedByKeyword(input, token.Span, "FROM");
    }

    public static TextSpan GetFromDiagnosticSpan(Token token, string input)
    {
        return IsLikelyMisspelledFromKeyword(token, input) ? token.Span : new TextSpan(token.Span.Start, 0);
    }

    private static bool IsFollowedByKeyword(string input, TextSpan span, string keyword)
    {
        var index = span.End;
        while (index < input.Length)
        {
            while (index < input.Length && char.IsWhiteSpace(input[index]))
                index++;
            if (index + 1 < input.Length && input[index] == '-' && input[index + 1] == '-')
            {
                index += 2;
                while (index < input.Length && input[index] is not '\r' and not '\n')
                    index++;
                continue;
            }
            if (index + 1 < input.Length && input[index] == '/' && input[index + 1] == '*')
            {
                var commentEnd = input.IndexOf("*/", index + 2, StringComparison.Ordinal);
                if (commentEnd < 0)
                    return false;
                index = commentEnd + 2;
                continue;
            }
            break;
        }

        if (index + keyword.Length > input.Length ||
            !input.AsSpan(index, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
            return false;

        var end = index + keyword.Length;
        return end >= input.Length || (!char.IsLetterOrDigit(input[end]) && input[end] != '_');
    }
}
