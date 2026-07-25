using System.Collections.Generic;

namespace Musoq.Evaluator.Utils.Symbols;

public class AliasesSymbol : Symbol
{
    private readonly HashSet<string> _aliases = [];

    public void AddAlias(string alias)
    {
        _aliases.Add(alias);
    }

    public bool ContainsAlias(string alias)
    {
        return _aliases.Contains(alias);
    }

    internal AliasesSymbol Clone()
    {
        var clone = new AliasesSymbol();
        foreach (var alias in _aliases)
            clone.AddAlias(alias);
        return clone;
    }
}
