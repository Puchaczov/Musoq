namespace Musoq.Parser.Tokens;

public class GenericFunctionToken(string fname, string typeParameter, TextSpan span) : FunctionToken(fname, span)
{
    public string TypeParameter { get; } = typeParameter;

    public override string ToString()
    {
        return $"{Value}<{TypeParameter}>";
    }
}
