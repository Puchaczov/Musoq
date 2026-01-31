namespace Musoq.Parser.Tokens;

/// <summary>
///     Represents an error token produced during lexing when an invalid
///     or unrecognized character is encountered and recovery mode is enabled.
/// </summary>
public sealed class ErrorToken : Token
{
    /// <summary>
    ///     Creates a new error token.
    /// </summary>
    /// <param name="character">The unrecognized character.</param>
    /// <param name="span">The location of the error.</param>
    public ErrorToken(char character, TextSpan span)
        : base(character.ToString(), TokenType.Error, span)
    {
        Character = character;
    }

    /// <summary>
    ///     Creates a new error token with a string value.
    /// </summary>
    /// <param name="value">The invalid token value.</param>
    /// <param name="span">The location of the error.</param>
    public ErrorToken(string value, TextSpan span)
        : base(value, TokenType.Error, span)
    {
        Character = value.Length > 0 ? value[0] : '\0';
    }

    /// <summary>
    ///     Gets the primary unrecognized character.
    /// </summary>
    public char Character { get; }

    /// <summary>
    ///     Gets the error message for this token.
    /// </summary>
    public string ErrorMessage => $"Unrecognized token '{Value}' at position {Span.Start}";

    public override GenericToken<TokenType> Clone()
    {
        return new ErrorToken(Value, Span);
    }
}
