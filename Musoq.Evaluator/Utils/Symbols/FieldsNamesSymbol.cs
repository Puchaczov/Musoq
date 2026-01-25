namespace Musoq.Evaluator.Utils.Symbols;

public class FieldsNamesSymbol : Symbol
{
    public FieldsNamesSymbol(string[] names)
    {
        Names = names;
    }

    public string[] Names { get; }
}
