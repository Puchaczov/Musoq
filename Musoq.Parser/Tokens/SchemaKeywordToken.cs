namespace Musoq.Parser.Tokens;

/// <summary>
///     A generic token class for interpretation schema keywords.
///     This is used for binary/text schema syntax tokens like:
///     binary, text, le, be, byte, short, int, etc.
/// </summary>
public class SchemaKeywordToken : Token
{
    public SchemaKeywordToken(string value, TokenType type, TextSpan span)
        : base(value, type, span)
    {
    }

    public override GenericToken<TokenType> Clone()
    {
        return new SchemaKeywordToken(Value, TokenType, Span);
    }
}
