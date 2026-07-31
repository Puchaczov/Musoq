using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private static bool CanUseSourceEntityShape(Type entityType)
    {
        return entityType != typeof(object) && CanReferenceType(entityType);
    }

    private Type ResolveEntityType(string alias)
    {
        if (_entityTypesByAlias.TryGetValue(alias, out var entityType))
            return entityType;

        var tableSymbol = FindTableSymbol(alias);
        if (tableSymbol == null || !tableSymbol.ContainsAlias(alias))
            return typeof(object);

        var (_, table, _) = tableSymbol.GetTableByAlias(alias);
        return table.Metadata?.TableEntityType ?? typeof(object);
    }

    private TableSymbol? FindTableSymbol(string alias)
    {
        if (_scope == null)
            return null;

        return FindTableSymbol(_scope, alias);
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

    private static Type ResolveEnumerableElementType(Type resultType)
    {
        if (resultType.IsArray)
            return resultType.GetElementType()!;

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return resultType.GetGenericArguments()[0];

        if (resultType != typeof(string))
        {
            var enumerableInterface = resultType
                .GetInterfaces()
                .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface is not null)
                return enumerableInterface.GetGenericArguments()[0];
        }

        return resultType;
    }

    private static bool IsDynamicEntity(Type entityType)
    {
        return ExecutionSourceCodeGenerationPolicy.IsSupportedDictionary(entityType);
    }

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        return type.IsPrimitive
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(Guid);
    }

    private static bool IsRowSourceType(Type type)
    {
        Type? current = type;
        while (current != null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(RowSource<>))
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static bool CanReferenceType(Type type)
    {
        return ExecutionSourceCodeGenerationPolicy.CanReferenceType(type);
    }
}
