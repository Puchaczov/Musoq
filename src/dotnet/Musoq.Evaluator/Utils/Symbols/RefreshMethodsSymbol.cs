using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Utils.Symbols;

public class RefreshMethodsSymbol(IEnumerable<AccessMethodNode> refreshMethods) : Symbol
{
    public IReadOnlyList<AccessMethodNode> RefreshMethods { get; } = refreshMethods.ToArray();
}
