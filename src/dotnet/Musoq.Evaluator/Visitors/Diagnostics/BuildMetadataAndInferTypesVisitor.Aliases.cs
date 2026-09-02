using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private string[] GetVisibleAliases()
    {
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scope = _sourceBinding.CurrentScope;
        while (scope != null)
        {
            if (scope.ScopeSymbolTable.TryGetSymbol<AliasesSymbol>(MetaAttributes.Aliases, out var symbol))
                aliases.UnionWith(symbol.Aliases.Where(static alias => !string.IsNullOrWhiteSpace(alias)));

            scope = scope.Parent;
        }

        return aliases.OrderBy(static alias => alias, StringComparer.Ordinal).ToArray();
    }
}
