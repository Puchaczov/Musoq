namespace Musoq.Evaluator.Utils.Symbols;

public class FieldsNamesSymbol(string[] names) : Symbol
{
    public string[] Names { get; } = System.Linq.Enumerable.ToArray(names);

    internal FieldsNamesSymbol Clone()
    {
        return new FieldsNamesSymbol(Names);
    }
}
