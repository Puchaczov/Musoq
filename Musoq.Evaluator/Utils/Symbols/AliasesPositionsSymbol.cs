using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Utils.Symbols;

public class AliasesPositionsSymbol : Symbol
{
    public IDictionary<string, int> AliasesPositions { get; } = new Dictionary<string, int>();

    public int GetContextIndexOf(string alias)
    {
        var index = AliasesPositions[alias];

        if (index is 0 or 1) return index;

        if (index % 2 == 0) throw new NotSupportedException($"Alias {alias} is not a context alias.");

        return index - 1;
    }
}
