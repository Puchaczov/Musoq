namespace Musoq.Parser.Tokens;

public class PropertyToken(string value, TextSpan span) : Token(value, TokenType.Property, span);
