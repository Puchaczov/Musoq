namespace Musoq.Parser.Tokens;

public class FunctionToken(string fname, TextSpan span) : Token(fname, TokenType.Function, span)
{
    public const string TokenText = "function";

    public override string ToString()
    {
        return Value;
    }
}
