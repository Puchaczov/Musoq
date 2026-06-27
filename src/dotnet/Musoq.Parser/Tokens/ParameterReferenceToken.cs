namespace Musoq.Parser.Tokens;

public class ParameterReferenceToken(string name, TextSpan span) : Token(name, TokenType.ParameterReference, span)
{
    public override string ToString()
    {
        return $"${Value}";
    }
}
