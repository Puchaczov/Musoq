using System.Collections.Generic;
using System.Linq;
using GeneratedRowContextConstructor = Musoq.Targets.CSharpClr.ExecutionCSharpRenderer.GeneratedRowContextConstructor;

namespace Musoq.Targets.CSharpClr;

internal static class GeneratedRowCarrierUsage
{
    public static IReadOnlySet<string> CollectTypesRequiringRowBase(
        ExecutionBlock block,
        IReadOnlyDictionary<string, IReadOnlySet<GeneratedRowContextConstructor>> constructorUsages)
    {
        var aliases = CollectGeneratedRowAliases(block);
        var types = new HashSet<string>(StringComparer.Ordinal);

        foreach (var read in ExecutionIrAnalysis.CollectExpressions<ExecutionFieldRead>(block))
        {
            if (!RequiresRowBase(read, constructorUsages))
                continue;

            if (read.AccessStrategy is GeneratedRowContextAccess context)
                types.Add(context.TypeName);

            if (string.IsNullOrWhiteSpace(read.Alias) ||
                !aliases.TryGetValue(read.Alias, out var typeNames))
            {
                continue;
            }

            foreach (var typeName in typeNames)
                types.Add(typeName);
        }

        return types;
    }

    private static Dictionary<string, HashSet<string>> CollectGeneratedRowAliases(ExecutionBlock block)
    {
        var aliases = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var node in ExecutionIrAnalysis.FlattenNodes(block))
            AddNodeVariables(node, aliases);

        foreach (var variableRead in ExecutionIrAnalysis.CollectExpressions<ExecutionVariableRead>(block))
            AddVariable(variableRead.Variable, aliases);

        return aliases;
    }

    private static void AddNodeVariables(ExecutionNode node, Dictionary<string, HashSet<string>> aliases)
    {
        switch (node)
        {
            case ExecutionForEach loop:
                AddVariable(loop.Item, aliases);
                break;
            case ExecutionForEachWithOrdinality loop:
                AddVariable(loop.Item, aliases);
                break;
            case ExecutionForEachIndexed loop:
                AddVariable(loop.Item, aliases);
                break;
            case ExecutionParallelFilterProjectLoop loop:
                AddVariable(loop.Source, aliases);
                break;
            case ExecutionParallelSingleKeyAggregateLoop loop:
                AddVariable(loop.Source, aliases);
                break;
            case ExecutionMaterializeFilteredList materialize:
                AddVariable(materialize.Item, aliases);
                break;
            case ExecutionCreateGeneratedRow createRow:
                AddVariable(createRow.Row, aliases);
                break;
            case ExecutionAppendExistingRow appendRow:
                AddVariable(appendRow.Row, aliases);
                break;
            case ExecutionHashAdd hashAdd:
                AddVariable(hashAdd.Row, aliases);
                break;
            case ExecutionCreateAsOfIndex asOfIndex:
                AddVariable(asOfIndex.Candidate, aliases);
                break;
            case ExecutionAsOfProbe asOfProbe:
                AddVariable(asOfProbe.Match, aliases);
                AddVariable(asOfProbe.Candidate, aliases);
                break;
            case ExecutionCreateRangeIndex rangeIndex:
                AddVariable(rangeIndex.Candidate, aliases);
                break;
            case ExecutionRangeProbe rangeProbe:
                AddVariable(rangeProbe.Match, aliases);
                break;
        }
    }

    private static void AddVariable(ExecutionVariable variable, Dictionary<string, HashSet<string>> aliases)
    {
        if (string.IsNullOrWhiteSpace(variable.GeneratedRowTypeName))
            return;

        if (!aliases.TryGetValue(variable.Name, out var typeNames))
        {
            typeNames = [];
            aliases.Add(variable.Name, typeNames);
        }

        typeNames.Add(variable.GeneratedRowTypeName);
    }

    private static bool RequiresRowBase(
        ExecutionFieldRead read,
        IReadOnlyDictionary<string, IReadOnlySet<GeneratedRowContextConstructor>> constructorUsages)
    {
        return read.AccessStrategy is PositionalAccess or NestedPositionalAccess or ContextAccess ||
               read.AccessStrategy is GeneratedRowContextAccess context &&
               !CanReadGeneratedContextStorage(context, constructorUsages);
    }

    private static bool CanReadGeneratedContextStorage(
        GeneratedRowContextAccess context,
        IReadOnlyDictionary<string, IReadOnlySet<GeneratedRowContextConstructor>> constructorUsages)
    {
        if (!constructorUsages.TryGetValue(context.TypeName, out var usages))
            return false;

        var constructors = usages
            .Where(static usage => usage != GeneratedRowContextConstructor.NoContext)
            .ToArray();

        if (constructors.Length > 1)
            return context.Index >= 0;

        return constructors.Length == 1 && constructors[0] switch
        {
            GeneratedRowContextConstructor.ContextArray => context.Index >= 0,
            GeneratedRowContextConstructor.SingleContext => context.Index == 0,
            GeneratedRowContextConstructor.SingleContexts => context.Index >= 0,
            GeneratedRowContextConstructor.TwoSingleContexts => context.Index is 0 or 1,
            _ => false
        };
    }
}
