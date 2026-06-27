namespace Musoq.Parser;

public partial class Parser
{
    private bool IsValuesSource()
    {
        if (!IsContextualKeyword("values"))
            return false;

        var position = Current.Span.End;
        while (position < _lexer.Input.Length && char.IsWhiteSpace(_lexer.Input[position]))
            position++;

        return position < _lexer.Input.Length && _lexer.Input[position] == '{';
    }
}
