namespace Musoq.Parser.Tokens;

/// <summary>
///     Token representing the not-equal operator.
///     The standard SQL syntax is '<>' but '!=' is also tokenized
///     so the parser can emit a clear error message directing users to use '<>'.
/// </summary>
public class DiffToken : Token
{
    public const string TokenText = "<>";

    public DiffToken(TextSpan span)
        : base(TokenText, TokenType.Diff, span)
    {
    }

    /// <summary>
    ///     Constructor for non-standard operators like '!=' that need custom token text
    ///     for error message purposes.
    /// </summary>
    public DiffToken(string tokenText, TextSpan span)
        : base(tokenText, TokenType.Diff, span)
    {
    }
}
