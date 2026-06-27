using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record CteSidecarStorageDecision(
        bool StoreRows,
        bool KeepPayloadRows);

    private CteSidecarStorageDecision CreateCteSidecarStorageDecision(
        string definitionName,
        IReadOnlyList<CteSidecarIndexSpec> sidecarSpecs,
        IReadOnlyDictionary<string, CteReferenceClassification> classifications,
        TableBuildResult result)
    {
        if (!_compilationOptions.UseCteSidecarIndexes ||
            sidecarSpecs.Count == 0 ||
            !result.Supported ||
            !classifications.TryGetValue(definitionName, out var classification) ||
            classification.ReferenceCount != sidecarSpecs.Count ||
            !ContainsAllStoredCteIndexes(result.Nodes, sidecarSpecs) ||
            ContainsSideEffectSensitiveSiblingExpression(result.Nodes))
        {
            return new CteSidecarStorageDecision(StoreRows: true, KeepPayloadRows: true);
        }

        return new CteSidecarStorageDecision(
            StoreRows: false,
            KeepPayloadRows: ContainsHashSidecarRowPayload(result.Nodes, result.RowShape.TypeName));
    }

    private static bool ContainsHashSidecarRowPayload(
        IReadOnlyList<ExecutionNode> nodes,
        string rowTypeName)
    {
        return ExecutionIrAnalysis
            .FlattenNodes(new ExecutionBlock(nodes))
            .OfType<ExecutionHashAdd>()
            .Any(add => string.Equals(add.GeneratedRowTypeName, rowTypeName, StringComparison.Ordinal));
    }

    private TableBuildResult ApplyCteSidecarOptimizations(
        string definitionName,
        IReadOnlyList<CteSidecarIndexSpec> sidecarSpecs,
        IReadOnlyDictionary<string, CteReferenceClassification> cteReferenceClassifications,
        CteDefinitionPruningPlan pruningPlan,
        TableBuildResult result,
        out CteSidecarStorageDecision storage)
    {
        result = ApplyCteSidecarIndexes(result, sidecarSpecs);
        result = ApplyCteContextPruning(result, definitionName, pruningPlan);
        storage = CreateCteSidecarStorageDecision(
            definitionName,
            sidecarSpecs,
            cteReferenceClassifications,
            result);

        return ApplyIndexOnlyCteSidecarStorage(result, storage);
    }

    private static bool ContainsAllStoredCteIndexes(
        IReadOnlyList<ExecutionNode> nodes,
        IReadOnlyList<CteSidecarIndexSpec> sidecarSpecs)
    {
        var storedSlots = ExecutionIrAnalysis.FlattenNodes(new ExecutionBlock(nodes))
            .Select(static node => node switch
            {
                ExecutionCteSidecarIndexStoreCandidate candidate => candidate.IndexSlot,
                _ => -1
            })
            .Where(static slot => slot >= 0)
            .ToHashSet();

        return sidecarSpecs.All(spec => storedSlots.Contains(spec.IndexSlot));
    }

    private static TableBuildResult ApplyIndexOnlyCteSidecarStorage(
        TableBuildResult result,
        CteSidecarStorageDecision decision)
    {
        if (decision.StoreRows)
            return result;

        return result with
        {
            Nodes =
            [
                new ExecutionCteIndexOnlyStorageCandidate(
                    result.Table.Name,
                    result.RowShape.TypeName,
                    decision.KeepPayloadRows),
                ..result.Nodes
            ]
        };
    }
}
