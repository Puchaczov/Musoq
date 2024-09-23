using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Utils.Symbols;

public class RefreshMethodsSymbol : Symbol
{
    public RefreshMethodsSymbol(IEnumerable<AccessMethodNode> refreshMethods)
    {
        RefreshMethods = refreshMethods.ToArray();
    }

    public IReadOnlyList<AccessMethodNode> RefreshMethods { get; }
}