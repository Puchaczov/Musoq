using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Utils.Symbols;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticSourceBindingService(SourceBindingState sourceBinding)
{
    public bool HasAlreadyUsedAlias(string queryAlias)
    {
        var scope = sourceBinding.CurrentScope;

        while (scope != null)
        {
            if (scope.ScopeSymbolTable.TryGetSymbol<AliasesSymbol>(MetaAttributes.Aliases, out var symbol) &&
                symbol.ContainsAlias(queryAlias))
            {
                return true;
            }

            scope = scope.Parent;
        }

        return false;
    }
}
