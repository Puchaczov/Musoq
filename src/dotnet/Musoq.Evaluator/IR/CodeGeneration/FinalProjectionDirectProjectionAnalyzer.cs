using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static class FinalProjectionDirectProjectionAnalyzer
{
    public static FinalProjectionSinkPlan Analyze(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo,
        FinalProjectionSinkTarget target)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(resultInfo);

        var sourceScans = plan.Body.Nodes.OfType<ExecutionSourceScan>().ToArray();
        if (sourceScans.Length != 1)
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.ExpectedOneSourceScan,
                $"Expected one source scan, but found {sourceScans.Length}.");
        var setupNodes = plan.Body.Nodes
            .Where(static node => node is ExecutionCreateObject)
            .ToArray();

        if (!TryGetReturnedTable(plan, resultInfo, out var table))
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.FinalReturnedTableMismatch,
                "Final returned table does not match generated row metadata.");

        var loopNodes = plan.Body.Nodes
            .Where(static node => node is ExecutionSourceLoop or ExecutionParallelFilterProjectLoop)
            .ToArray();
        if (loopNodes.Length != 1)
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.ExpectedOneProjectionLoop,
                $"Expected one projection loop, but found {loopNodes.Length}.");

        if (plan.Body.Nodes.Any(node => !IsAllowedDirectProjectionNode(node, table, loopNodes[0])))
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.UnexpectedPlanNodes,
                "Plan contains nodes outside the direct projection sink.");

        if (!FinalProjectionSinkPlanningHelpers.TryCreateProjectionLoop(loopNodes[0], table, resultInfo, out var projectionLoop))
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.ProjectionLoopMismatch,
                "Projection loop does not append the final table shape.");

        if (target == FinalProjectionSinkTarget.TableRows &&
            loopNodes[0] is ExecutionParallelFilterProjectLoop parallelLoop &&
            FinalProjectionSinkPlanningHelpers.HasDuplicatedUncachedMethodCalls(parallelLoop) &&
            !FinalProjectionSinkPlanningHelpers.CanRenderOptionalProjectionProjectorBody(parallelLoop.SequentialLoop.Body))
        {
            return FinalProjectionSinkPlan.Rejected(
                FinalProjectionSinkRejectionKind.UnexpectedPlanNodes,
                "Direct row-shard projection would duplicate uncached row-local method expressions.");
        }

        return FinalProjectionSinkPlan.Accepted(
            sourceScans,
            projectionLoop,
            projectionLoop.AppendRow.Table,
            FinalProjectionSinkPlanningHelpers.CreateResultMetadata(target, projectionLoop.CanUseParallel),
            [],
            setupNodes);
    }

    private static bool TryGetReturnedTable(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo,
        out ExecutionVariable table)
    {
        table = null!;
        var returnTable = plan.Body.Nodes.OfType<ExecutionReturnTable>().LastOrDefault();
        var createTable = plan.Body.Nodes
            .OfType<ExecutionCreateTable>()
            .LastOrDefault(candidate => candidate.Table.Name == returnTable?.Table.Name);

        if (returnTable == null ||
            createTable == null ||
            createTable.Table.Name != resultInfo.TableName)
        {
            return false;
        }

        table = createTable.Table;
        return true;
    }

    private static bool IsAllowedDirectProjectionNode(
        ExecutionNode node,
        ExecutionVariable table,
        ExecutionNode loopNode)
    {
        return node switch
        {
            ExecutionSourceScan => true,
            ExecutionCreateObject => true,
            ExecutionCreateTable createTable => createTable.Table.Name == table.Name,
            ExecutionReturnTable returnTable => returnTable.Table.Name == table.Name,
            _ when ReferenceEquals(node, loopNode) => true,
            _ => false
        };
    }
}
