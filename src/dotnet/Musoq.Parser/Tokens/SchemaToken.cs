namespace Musoq.Parser.Tokens;

/// <summary>
///     A generic token used for schema-specific keywords and types.
///     Allows creating tokens with any TokenType and value.
/// </summary>
public class SchemaToken(string value, TokenType tokenType, TextSpan span) : Token(value, tokenType, span);
