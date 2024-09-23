namespace Musoq.Parser.Tokens;

public class IntegerToken : Token
{
    public const string TokenText = "numeric";

    public IntegerToken(string value, TextSpan span, string abbreviation)
        : base(value, TokenType.Integer, span)
    {
        Abbreviation = abbreviation;
    }
        
    public string Abbreviation { get; }
}