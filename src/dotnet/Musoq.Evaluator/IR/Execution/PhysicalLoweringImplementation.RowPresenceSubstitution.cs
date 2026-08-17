using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionExpression SubstituteRowPresenceAliases(
        ExecutionExpression expression,
        IReadOnlyDictionary<string, bool> presenceByAlias)
    {
        return new RowPresenceSubstitutionRewriter(presenceByAlias).RewriteExpression(expression);
    }

    private static IReadOnlyDictionary<string, bool> CreateAllPresentMap(
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        return CreatePresenceMap(sourceLookup);
    }

    private static IReadOnlyDictionary<string, bool> CreateNullExtendedPresenceMap(
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        string missingAlias)
    {
        var map = CreatePresenceMap(sourceLookup);
        map[missingAlias] = false;
        return map;
    }

    private static Dictionary<string, bool> CreatePresenceMap(
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        foreach (var alias in CollectPresenceAliases(sourceLookup))
            map[alias] = true;

        return map;
    }

    private static IEnumerable<string> CollectPresenceAliases(
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        foreach (var alias in sourceLookup.Keys)
        {
            if (!string.IsNullOrWhiteSpace(alias))
                yield return alias;
        }

        foreach (var sourceShape in sourceLookup.Values)
        {
            if (RowShapeLookup.TryResolveSourceAlias(sourceShape, out var sourceAlias))
                yield return sourceAlias;

            if (sourceShape is not TableRowShape tableRow)
                continue;

            foreach (var context in tableRow.Contexts)
            {
                if (!string.IsNullOrWhiteSpace(context.Name))
                    yield return context.Name;

                if (!string.IsNullOrWhiteSpace(context.QualifiedName))
                    yield return context.QualifiedName;
            }
        }
    }

}
