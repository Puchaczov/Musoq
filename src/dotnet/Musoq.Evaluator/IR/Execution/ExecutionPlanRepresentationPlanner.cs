using System;
using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Finalizes planner-owned stored-table representation decisions after the
/// complete execution body, including scope boundaries, is available.
/// </summary>
internal static class ExecutionPlanRepresentationPlanner
{
    public static ExecutionPlan Plan(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var discovered = TypedStoredTableResultResolver.Resolve(plan);
        var structuralConsumers = ExecutionIrAnalysis
            .CollectExpressions<ExecutionCteCollectionInput>(plan.Body)
            .Where(static input => input.Rows is ExecutionStoredTableRows)
            .GroupBy(static input => ((ExecutionStoredTableRows)input.Rows).TableIndex)
            .ToDictionary(static group => group.Key, static group => group.ToArray());
        var representations = discovered.Count == 0 && plan.StoredTableRepresentations.Count > 0
            ? plan.StoredTableRepresentations
            : discovered
                .OrderBy(static pair => pair.Key)
                .Select(pair => new ExecutionStoredTableRepresentationPlan(
                    pair.Key,
                    pair.Value.RowShape,
                    ownership: SelectOwnership(
                        pair.Key,
                        pair.Value.RowShape,
                        plan.Body,
                        structuralConsumers.TryGetValue(pair.Key, out var consumers)
                            ? consumers
                            : [])))
                .ToArray();
        var planned = plan with { StoredTableRepresentations = representations };
        return StoredTableRepresentationAnnotator.Apply(planned, representations);
    }

    private static ExecutionStructuralOwnershipMode SelectOwnership(
        int tableIndex,
        GeneratedRowShape rowShape,
        ExecutionBlock body,
        IReadOnlyList<ExecutionCteCollectionInput> consumers)
    {
        if (consumers.Count == 0)
            return ExecutionStructuralOwnershipMode.ConstructFresh;

        if (consumers.Count == 1 &&
            !ExecutionIrAnalysis.CollectExpressions<ExecutionStoredTable>(body)
                .Any(stored => stored.TableIndex == tableIndex) &&
            CanTransfer(consumers[0], rowShape))
        {
            return ExecutionStructuralOwnershipMode.Transfer;
        }

        // Primitive and collection elements can be copied directly from the
        // planner-owned row list. Record receivers always require fresh
        // construction so mutable provider values are never shared.
        return consumers.All(static consumer => consumer.ConstructionPlan == null)
            ? ExecutionStructuralOwnershipMode.Copy
            : ExecutionStructuralOwnershipMode.ConstructFresh;
    }

    private static bool CanTransfer(
        ExecutionCteCollectionInput input,
        GeneratedRowShape rowShape)
    {
        if (input.ConstructionPlan != null ||
            !string.Equals(input.ElementType.DisplayName, rowShape.TypeName, StringComparison.Ordinal) ||
            input.Fields.Count != rowShape.Fields.Count)
        {
            return false;
        }

        return input.Fields.All(field => rowShape.Fields.Any(rowField =>
            rowField.OutputIndex == field.Index &&
            string.Equals(rowField.Type.StableId, field.Type.StableId, StringComparison.Ordinal)));
    }
}
