using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class CteSidecarStoragePlanner(bool useCteSidecarIndexes)
{
    public CteSidecarStorageDecision CreateStorageDecision(
        string definitionName,
        IReadOnlyList<CteSidecarIndexSpec> sidecarSpecs,
        IReadOnlyDictionary<string, CteReferenceClassification> classifications,
        bool resultSupported,
        IReadOnlyList<ExecutionNode> resultNodes,
        string rowTypeName)
    {
        if (!useCteSidecarIndexes ||
            sidecarSpecs.Count == 0 ||
            !resultSupported ||
            !classifications.TryGetValue(definitionName, out var classification) ||
            classification.ReferenceCount != sidecarSpecs.Count ||
            !ContainsAllStoredCteIndexes(resultNodes, sidecarSpecs) ||
            ContainsSideEffectSensitiveSiblingExpression(resultNodes))
        {
            return new CteSidecarStorageDecision(StoreRows: true, KeepPayloadRows: true);
        }

        return new CteSidecarStorageDecision(
            StoreRows: false,
            KeepPayloadRows: ContainsHashSidecarRowPayload(resultNodes, rowTypeName));
    }

    public IReadOnlyList<ExecutionNode> ApplyIndexOnlyStorage(
        string tableName,
        string rowTypeName,
        IReadOnlyList<ExecutionNode> nodes,
        CteSidecarStorageDecision decision)
    {
        if (decision.StoreRows)
            return nodes;

        return
        [
            new ExecutionCteIndexOnlyStorageCandidate(
                tableName,
                rowTypeName,
                decision.KeepPayloadRows),
            ..nodes
        ];
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

    private static bool ContainsSideEffectSensitiveSiblingExpression(IReadOnlyList<ExecutionNode> nodes)
    {
        var block = new ExecutionBlock(nodes);
        return ExecutionIrAnalysis.CollectExpressions<ExecutionMethodCall>(block).Any();
    }
}
