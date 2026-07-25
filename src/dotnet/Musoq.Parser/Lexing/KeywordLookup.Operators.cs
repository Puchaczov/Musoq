using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

public static partial class KeywordLookup
{
    /// <summary>
    ///     Tries to get the token type for an operator.
    /// </summary>
    /// <param name="text">The operator text to look up.</param>
    /// <param name="tokenType">The token type if found.</param>
    /// <returns>True if the text is a recognized operator.</returns>
    public static bool TryGetOperator(string text, out TokenType tokenType)
    {
        return Operators.TryGetValue(text, out tokenType);
    }

    private static bool EqualsKeyword(ReadOnlySpan<char> text, string keyword)
    {
        return text.Equals(keyword.AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Found(TokenType value, out TokenType tokenType)
    {
        tokenType = value;
        return value != TokenType.Word;
    }

}
