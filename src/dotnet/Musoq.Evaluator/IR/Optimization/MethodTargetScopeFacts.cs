using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization;

internal static class MethodTargetScopeFacts
{
    public static string GetLoopPrefix(ExecutionBlock block, string? currentTableTargetNamePrefix)
    {
        var prefix = GetTablePrefix(block);
        if (!string.IsNullOrWhiteSpace(prefix))
            return prefix;

        return ContainsAggregateGroupLookup(block) &&
               !string.IsNullOrWhiteSpace(currentTableTargetNamePrefix)
            ? currentTableTargetNamePrefix
            : string.Empty;
    }

    public static string CreateMaterializationTargetPrefix(string bufferName)
    {
        const string windowRowsSuffix = "WindowRows";

        return bufferName.EndsWith(windowRowsSuffix, StringComparison.Ordinal) &&
               bufferName.Length > windowRowsSuffix.Length
            ? bufferName[..^windowRowsSuffix.Length]
            : bufferName;
    }

    private static string GetTablePrefix(ExecutionBlock block)
    {
        foreach (var node in block.Nodes)
        {
            var prefix = GetTablePrefix(node);
            if (!string.IsNullOrWhiteSpace(prefix))
                return prefix;
        }

        return string.Empty;
    }

    private static string GetTablePrefix(ExecutionNode node)
    {
        return node switch
        {
            ExecutionAppendRow appendRow => appendRow.Table.Name,
            ExecutionAppendExistingRow appendRow => appendRow.Table.Name,
            ExecutionAppendRecord appendRecord => appendRecord.List.Name,
            ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd => CreateAggregateTargetPrefix(getOrAdd.RootGroup.Name),
            ExecutionGetOrAddValueTupleAggregateGroup getOrAdd => CreateAggregateTargetPrefix(getOrAdd.RootGroup.Name),
            ExecutionIf branch => GetTablePrefix(branch.Body),
            ExecutionHashProbe probe => FirstNonEmpty(
                GetTablePrefix(probe.Body),
                probe.NoMatchBody != null ? GetTablePrefix(probe.NoMatchBody) : string.Empty),
            ExecutionKeySetProbe probe => FirstNonEmpty(
                GetTablePrefix(probe.Body),
                probe.NoMatchBody != null ? GetTablePrefix(probe.NoMatchBody) : string.Empty),
            ExecutionAsOfProbe probe => FirstNonEmpty(
                GetTablePrefix(probe.Body),
                probe.NoMatchBody != null ? GetTablePrefix(probe.NoMatchBody) : string.Empty),
            ExecutionRangeProbe probe => GetTablePrefix(probe.Body),
            ExecutionForEach loop => GetTablePrefix(loop.Body),
            ExecutionForEachWithOrdinality loop => GetTablePrefix(loop.Body),
            ExecutionForEachIndexed loop => GetTablePrefix(loop.Body),
            _ => string.Empty
        };
    }

    private static bool ContainsAggregateGroupLookup(ExecutionBlock block)
    {
        return block.Nodes.Any(ContainsAggregateGroupLookup);
    }

    private static bool ContainsAggregateGroupLookup(ExecutionNode node)
    {
        return node switch
        {
            ExecutionGetOrAddSingleKeyAggregateGroup or ExecutionGetOrAddValueTupleAggregateGroup => true,
            ExecutionIf branch => ContainsAggregateGroupLookup(branch.Body),
            ExecutionHashProbe probe => ContainsAggregateGroupLookup(probe.Body) ||
                                        (probe.NoMatchBody != null && ContainsAggregateGroupLookup(probe.NoMatchBody)),
            ExecutionKeySetProbe probe => ContainsAggregateGroupLookup(probe.Body) ||
                                           (probe.NoMatchBody != null && ContainsAggregateGroupLookup(probe.NoMatchBody)),
            ExecutionAsOfProbe probe => ContainsAggregateGroupLookup(probe.Body) ||
                                         (probe.NoMatchBody != null && ContainsAggregateGroupLookup(probe.NoMatchBody)),
            ExecutionRangeProbe probe => ContainsAggregateGroupLookup(probe.Body),
            ExecutionForEach loop => ContainsAggregateGroupLookup(loop.Body),
            ExecutionForEachWithOrdinality loop => ContainsAggregateGroupLookup(loop.Body),
            ExecutionForEachIndexed loop => ContainsAggregateGroupLookup(loop.Body),
            _ => false
        };
    }

    private static string CreateAggregateTargetPrefix(string rootGroupName)
    {
        const string rootGroupSuffix = "RootGroup";

        return rootGroupName.EndsWith(rootGroupSuffix, StringComparison.Ordinal) &&
               rootGroupName.Length > rootGroupSuffix.Length
            ? rootGroupName[..^rootGroupSuffix.Length]
            : string.Empty;
    }

    private static string FirstNonEmpty(string first, string second)
    {
        return string.IsNullOrWhiteSpace(first) ? second : first;
    }
}
