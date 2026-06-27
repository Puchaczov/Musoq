using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static string? GetUnsupportedCombinationReason(ExecutionBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);

        return null;
    }

    private static string? GetUnsupportedVariableReuseReason(ExecutionBlock block)
    {
        foreach (var parallel in block.Nodes.OfType<ExecutionParallelBlock>())
        {
            foreach (var task in parallel.Tasks)
            {
                var taskReason = GetUnsupportedVariableReuseReason(task.Body);
                if (taskReason != null)
                    return taskReason;
            }

            var mergeReason = GetUnsupportedVariableReuseReason(parallel.Merge.Body);
            if (mergeReason != null)
                return mergeReason;
        }

        var sourceRowNames = CollectSourceRowNames(block).ToArray();
        var duplicateSourceRowName = sourceRowNames
            .GroupBy(static name => name, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1)
            ?.Key;

        if (duplicateSourceRowName != null)
            return $"Execution IR C# backend cannot render repeated source row variable {duplicateSourceRowName}.";

        var duplicateAggregateVariableName = GetDuplicateAggregateVariableName(block);
        if (duplicateAggregateVariableName != null)
            return $"Execution IR C# backend cannot render repeated aggregate variable {duplicateAggregateVariableName}.";

        var tableNames = CollectCreatedTableNames(block).ToHashSet(StringComparer.Ordinal);
        var conflictingLoopName = CollectLoopItemNames(block).FirstOrDefault(tableNames.Contains);
        if (conflictingLoopName != null)
            return $"Execution IR C# backend cannot render loop variable {conflictingLoopName} because it conflicts with a table variable.";

        return null;
    }

    private static string? GetDuplicateAggregateVariableName(ExecutionBlock block)
    {
        var duplicateName = CollectAggregateDeclarationNames(block)
            .GroupBy(static name => name, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1)
            ?.Key;

        if (duplicateName != null)
            return duplicateName;

        foreach (var node in block.Nodes)
        {
            foreach (var nestedBlock in GetNestedBlocks(node))
            {
                duplicateName = GetDuplicateAggregateVariableName(nestedBlock);
                if (duplicateName != null)
                    return duplicateName;
            }
        }

        return null;
    }

}
