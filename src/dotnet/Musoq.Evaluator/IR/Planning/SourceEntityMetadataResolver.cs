using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;

namespace Musoq.Evaluator.IR.Planning;

internal static class SourceEntityMetadataResolver
{
    public static Type? ResolveSourceEntityType(Scope? scope, string alias)
    {
        if (scope == null)
            return null;

        var tableSymbol = FindTableSymbol(scope, alias);
        if (tableSymbol == null || !tableSymbol.ContainsAlias(alias))
            return null;

        var (_, table, _) = tableSymbol.GetTableByAlias(alias);
        var entityType = table.Metadata?.TableEntityType ?? typeof(object);

        return entityType == typeof(object)
            ? null
            : entityType;
    }

    public static bool IsDynamicEntity(Type entityType)
    {
        return DynamicEntityBoundary.IsDynamicEntity(entityType);
    }

    private static TableSymbol? FindTableSymbol(Scope scope, string alias)
    {
        TableSymbol? firstMatch = null;
        TableSymbol? firstTypedMatch = null;

        FindTableSymbol(scope, alias, ref firstMatch, ref firstTypedMatch);

        return firstTypedMatch ?? firstMatch;
    }

    private static void FindTableSymbol(
        Scope scope,
        string alias,
        ref TableSymbol? firstMatch,
        ref TableSymbol? firstTypedMatch)
    {
        if (scope.ScopeSymbolTable.TryGetSymbol(alias, out TableSymbol? tableSymbol) &&
            tableSymbol is not null &&
            tableSymbol.ContainsAlias(alias))
        {
            firstMatch ??= tableSymbol;

            var (_, table, _) = tableSymbol.GetTableByAlias(alias);
            if ((table.Metadata?.TableEntityType ?? typeof(object)) != typeof(object))
                firstTypedMatch ??= tableSymbol;
        }

        foreach (var child in scope.Child)
            FindTableSymbol(child, alias, ref firstMatch, ref firstTypedMatch);
    }
}
