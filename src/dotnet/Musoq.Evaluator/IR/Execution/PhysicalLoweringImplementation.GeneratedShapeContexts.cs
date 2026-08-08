using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static List<FieldBinding> CreateContextBindings(
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyList<string>? nullableAliases = null)
    {
        var contexts = new List<FieldBinding>();

        foreach (var sourceShape in sourceLookup.Values)
            contexts.AddRange(CreateContextBindings(sourceShape, contexts.Count, nullableAliases));

        return contexts;
    }

    private static IEnumerable<FieldBinding> CreateContextBindings(
        RowShape sourceShape,
        int startIndex,
        IReadOnlyList<string>? nullableAliases)
    {
        if (sourceShape is TableRowShape tableRow)
        {
            if (tableRow.Contexts.Count == 0)
            {
                var tableAlias = RowShapeLookup.ResolveSourceAlias(tableRow);
                yield return new FieldBinding(
                    tableAlias,
                    tableAlias,
                    startIndex,
                    typeof(object),
                    IsNullableContext(tableAlias, nullableAliases)
                        ? FieldNullability.Nullable
                        : FieldNullability.Unknown,
                    new ContextAccess(startIndex));

                yield break;
            }

            for (var index = 0; index < tableRow.Contexts.Count; index++)
            {
                var context = tableRow.Contexts[index];
                yield return context with
                {
                    AccessStrategy = new ContextAccess(startIndex + index),
                    Nullability = IsNullableContext(context, nullableAliases)
                        ? FieldNullability.Nullable
                        : context.Nullability
                };
            }

            yield break;
        }

        var alias = RowShapeLookup.ResolveSourceAlias(sourceShape);
        var generatedContextBinding = new FieldBinding(
            alias,
            alias,
            startIndex,
            RowShapeLookup.ResolveSourceRuntimeType(sourceShape),
            IsNullableContext(alias, nullableAliases)
                ? FieldNullability.Nullable
                : FieldNullability.Unknown,
            new ContextAccess(startIndex));

        if (sourceShape is SourceEntityShape { GeneratedTypeName: { } generatedTypeName })
        {
            var generatedMemberTypeNames = sourceShape.Fields
                .Where(static field => field.GeneratedTypeName is { Length: > 0 })
                .GroupBy(static field => field.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.First().GeneratedTypeName!,
                    StringComparer.OrdinalIgnoreCase);
            generatedContextBinding = generatedContextBinding with
            {
                GeneratedTypeName = generatedTypeName,
                GeneratedMemberTypeNames = generatedMemberTypeNames
            };
        }

        yield return generatedContextBinding;
    }

    private static bool IsNullableContext(
        FieldBinding context,
        IReadOnlyList<string>? nullableAliases)
    {
        return nullableAliases != null && nullableAliases.Any(alias =>
            string.Equals(context.Name, alias, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(context.QualifiedName, alias, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNullableContext(
        string alias,
        IReadOnlyList<string>? nullableAliases)
    {
        return nullableAliases != null && nullableAliases.Any(nullableAlias =>
            string.Equals(alias, nullableAlias, StringComparison.OrdinalIgnoreCase));
    }
}
