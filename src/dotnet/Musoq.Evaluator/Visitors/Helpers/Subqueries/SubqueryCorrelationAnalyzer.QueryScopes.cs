using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed partial class SubqueryCorrelationAnalyzer
{
    private void EnterQuery(IReadOnlySet<string> localAliases)
    {
        _queryScopes.Push(new QueryScopeInfo(localAliases));

        foreach (var scope in _subqueryScopes)
            scope.AddLocalAliases(localAliases);
    }

    private bool IsCurrentLocalAlias(string alias)
    {
        return _queryScopes.Count > 0 && _queryScopes.Peek().Aliases.Contains(alias);
    }

    private bool IsOuterAlias(string alias)
    {
        return _queryScopes.Skip(1).Any(scope => scope.Aliases.Contains(alias));
    }

    private bool IsForbiddenAlias(string alias)
    {
        return _forbiddenAliasScopes.Any(scope => scope.Contains(alias));
    }

    private HashSet<string> GetVisibleAliases()
    {
        var aliases = CreateAliasSet();

        foreach (var scope in _queryScopes)
        foreach (var alias in scope.Aliases)
            aliases.Add(alias);

        return aliases;
    }

    private QueryScopeInfo[] ClearQueryScopes()
    {
        var savedScopes = _queryScopes.ToArray();
        _queryScopes.Clear();
        return savedScopes;
    }

    private void RestoreQueryScopes(QueryScopeInfo[] savedScopes)
    {
        for (var i = savedScopes.Length - 1; i >= 0; i -= 1)
            _queryScopes.Push(savedScopes[i]);
    }
}
