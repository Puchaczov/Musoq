namespace Musoq.Evaluator.Utils.Symbols;

public class FieldsNamesSymbol(string[] names) : Symbol
{
    public string[] Names { get; } = names;
}
