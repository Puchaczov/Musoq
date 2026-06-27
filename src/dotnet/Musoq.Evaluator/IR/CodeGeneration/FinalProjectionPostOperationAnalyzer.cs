using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static class FinalProjectionPostOperationAnalyzer
{
    public static FinalProjectionSinkPlan Analyze(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo,
        FinalProjectionSinkTarget target)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(resultInfo);

        var nodes = plan.Body.Nodes;
        var sourceScans = nodes.OfType<ExecutionSourceScan>().ToArray();
        if (sourceScans.Length != 1)
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.ExpectedOneSourceScan,
                $"Expected one source scan, but found {sourceScans.Length}.");

        var returnTable = nodes.OfType<ExecutionReturnTable>().LastOrDefault();
        if (returnTable == null || returnTable.Table.Name != resultInfo.TableName)
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.FinalReturnedTableMismatch,
                "Final returned table does not match generated row metadata.");

        var loopNodes = nodes
            .Where(static node => node is ExecutionSourceLoop or ExecutionParallelFilterProjectLoop)
            .ToArray();
        if (loopNodes.Length != 1)
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.ExpectedOneProjectionLoop,
                $"Expected one projection loop, but found {loopNodes.Length}.");

        if (!FinalProjectionSinkPlanningHelpers.TryGetProjectionAppendTable(loopNodes[0], out var appendTable))
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.ProjectionAppendMissing,
                "Projection loop does not append rows.");

        var createTable = nodes
            .OfType<ExecutionCreateTable>()
            .LastOrDefault(candidate => candidate.Table.Name == appendTable.Name);
        if (createTable == null)
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.ProjectionAppendTargetMissing,
                "Projection append target was not created in this plan.");

        if (!FinalProjectionSinkPlanningHelpers.TryCreateProjectionLoop(loopNodes[0], createTable.Table, resultInfo, out var projectionLoop))
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.ProjectionLoopMismatch,
                "Projection loop does not append the generated row shape.");

        if (!TryCreateTypedPostOperationChain(nodes, loopNodes[0], appendTable, returnTable.Table, out var postOperations))
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.UnsupportedPostOperationChain,
                "Post-operation chain is not supported by the final projection sink.");

        if (!nodes.All(node => IsAllowedTypedPostOperationNode(node, createTable.Table, loopNodes[0])))
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.UnexpectedPlanNodes,
                "Plan contains nodes outside the post-operation final projection sink.");

        return FinalProjectionSinkPlan.Accepted(
            sourceScans,
            projectionLoop,
            appendTable,
            FinalProjectionSinkPlanningHelpers.CreateResultMetadata(target, projectionLoop.CanUseParallel),
            postOperations);
    }

    private static bool TryCreateTypedPostOperationChain(
        IReadOnlyList<ExecutionNode> nodes,
        ExecutionNode loopNode,
        ExecutionVariable firstTable,
        ExecutionVariable returnTable,
        out IReadOnlyList<TypedPostOperation> postOperations)
    {
        var operations = new List<TypedPostOperation>();
        postOperations = operations;
        var currentTable = firstTable;
        var passedLoop = false;

        foreach (var node in nodes)
        {
            if (ReferenceEquals(node, loopNode))
            {
                passedLoop = true;
                continue;
            }

            if (!passedLoop)
                continue;

            if (node is ExecutionReturnTable)
                break;

            if (!TryCreateTypedPostOperation(node, currentTable, out var operation, out var targetTable))
                return false;

            operations.AddRange(operation);
            currentTable = targetTable;
        }

        return operations.Count > 0 && currentTable.Name == returnTable.Name;
    }

    private static bool TryCreateTypedPostOperation(
        ExecutionNode node,
        ExecutionVariable currentTable,
        out IReadOnlyList<TypedPostOperation> operations,
        out ExecutionVariable targetTable)
    {
        operations = [];
        targetTable = null!;

        switch (node)
        {
            case ExecutionDistinctTable distinct when distinct.Source.Name == currentTable.Name:
                operations = [TypedPostOperation.Distinct.Instance];
                targetTable = distinct.Target;
                return true;

            case ExecutionSortTable { RenumberFieldIndexes.Count: 0 } sort
                when sort.Source.Name == currentTable.Name && FinalProjectionSinkPlanningHelpers.CanUseTypedOrderKeys(sort.Keys):
                operations = [new TypedPostOperation.Order(sort.Keys)];
                targetTable = sort.Target;
                return true;

            case ExecutionTopNTable { RenumberFieldIndexes.Count: 0 } topN
                when topN.Source.Name == currentTable.Name && FinalProjectionSinkPlanningHelpers.CanUseTypedOrderKeys(topN.Keys):
                operations = [new TypedPostOperation.Order(topN.Keys), new TypedPostOperation.Take(topN.Count)];
                targetTable = topN.Target;
                return true;

            case ExecutionTopOffsetTable { RenumberFieldIndexes.Count: 0 } topOffset
                when topOffset.Source.Name == currentTable.Name && FinalProjectionSinkPlanningHelpers.CanUseTypedOrderKeys(topOffset.Keys):
                operations =
                [
                    new TypedPostOperation.Order(topOffset.Keys),
                    new TypedPostOperation.Skip(topOffset.SkipCount),
                    new TypedPostOperation.Take(topOffset.TakeCount)
                ];
                targetTable = topOffset.Target;
                return true;

            case ExecutionSkipTable skip when skip.Source.Name == currentTable.Name:
                operations = [new TypedPostOperation.Skip(skip.Count)];
                targetTable = skip.Target;
                return true;

            case ExecutionTakeTable take when take.Source.Name == currentTable.Name:
                operations = [new TypedPostOperation.Take(take.Count)];
                targetTable = take.Target;
                return true;

            case ExecutionSliceTable slice when slice.Source.Name == currentTable.Name:
                operations = [new TypedPostOperation.Skip(slice.SkipCount), new TypedPostOperation.Take(slice.TakeCount)];
                targetTable = slice.Target;
                return true;

            default:
                return false;
        }
    }

    private static bool IsAllowedTypedPostOperationNode(
        ExecutionNode node,
        ExecutionVariable createTable,
        ExecutionNode loopNode)
    {
        return node switch
        {
            ExecutionSourceScan => true,
            ExecutionCreateTable table => table.Table.Name == createTable.Name,
            ExecutionReturnTable => true,
            ExecutionDistinctTable or ExecutionSortTable or ExecutionTopNTable or ExecutionTopOffsetTable or ExecutionSkipTable or ExecutionTakeTable or ExecutionSliceTable => true,
            _ when ReferenceEquals(node, loopNode) => true,
            _ => false
        };
    }
}
