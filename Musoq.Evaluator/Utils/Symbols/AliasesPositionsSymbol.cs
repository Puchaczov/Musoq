using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Evaluator.Utils.Symbols
{
    public class AliasesPositionsSymbol : Symbol
    {
        public IDictionary<string, int> AliasesPositions { get; } = new Dictionary<string, int>();
    }
}
