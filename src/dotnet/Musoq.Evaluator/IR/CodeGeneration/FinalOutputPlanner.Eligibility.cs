using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static partial class FinalOutputPlanner
{
    private static bool CanStreamSerialFinalAppendRows(
        ExecutionPlan plan,
        TableViaRowsResultInfo resultInfo)
    {
        if (!plan.Body.Nodes.OfType<ExecutionCreateTable>().Any(create => create.Table.Name == resultInfo.TableName))
            return false;

        var sourceLoops = plan.Body.Nodes.OfType<ExecutionSourceLoop>().ToArray();
        if (sourceLoops.Length != 1)
            return false;

        var loop = sourceLoops[0];
        if (plan.Body.Nodes.Any(node => !IsAllowedSerialShapeStreamNode(node, resultInfo.TableName, loop)))
            return false;

        if (ExecutionIrAnalysis.CollectNodes<ExecutionParallelFilterProjectLoop>(plan.Body).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionParallelBlock>(plan.Body).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionAppendExistingRow>(plan.Body)
                .Any(append => append.Table.Name == resultInfo.TableName))
        {
            return false;
        }

        if (ExecutionIrAnalysis.CollectNodes<ExecutionEnumerableSource>(loop.Body).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionGetOrAddSingleKeyAggregateGroup>(loop.Body).Any() ||
            ExecutionIrAnalysis.CollectNodes<ExecutionGetOrAddValueTupleAggregateGroup>(loop.Body).Any())
        {
            return false;
        }

        var appendRows = ExecutionIrAnalysis.CollectNodes<ExecutionAppendRow>(loop)
            .Where(append => append.Table.Name == resultInfo.TableName)
            .ToArray();

        return appendRows.Length == 1 &&
               appendRows[0].Values.Count == resultInfo.Columns.Count;
    }

    private static bool IsAllowedSerialShapeStreamNode(
        ExecutionNode node,
        string finalTableName,
        ExecutionSourceLoop loop)
    {
        return node switch
        {
            ExecutionSourceScan => true,
            ExecutionInterpretSource => true,
            ExecutionEnumerableSource => true,
            ExecutionCreateObject => true,
            ExecutionCreateValuesRows => true,
            ExecutionPhaseBoundary => true,
            ExecutionCreateTable createTable => createTable.Table.Name == finalTableName,
            ExecutionEnsureTableCapacity ensureCapacity => ensureCapacity.Table.Name == finalTableName,
            ExecutionReturnTable returnTable => returnTable.Table.Name == finalTableName,
            ExecutionLet => true,
            _ when ReferenceEquals(node, loop) => true,
            _ => false
        };
    }

    private static bool HasSupportedFinalShapeWriter(
        ExecutionBlock block,
        string finalTableName,
        int columnCount)
    {
        if (ExecutionIrAnalysis.CollectNodes<ExecutionAppendRow>(block)
            .Any(append => append.Table.Name == finalTableName && append.Values.Count == columnCount))
        {
            return true;
        }

        if (ExecutionIrAnalysis.CollectNodes<ExecutionAppendExistingRow>(block)
            .Any(append => append.Table.Name == finalTableName))
        {
            return true;
        }

        if (block.Nodes.OfType<ExecutionMaterializeRecordListToTable>()
            .Any(materialize => materialize.Target.Name == finalTableName && materialize.FieldIndexes.Count == columnCount))
        {
            return true;
        }

        if (block.Nodes.OfType<ExecutionSetOperation>().Any(setOperation => setOperation.Target.Name == finalTableName))
            return true;

        return block.Nodes.Any(node => IsSupportedFinalShapePostOperation(node, finalTableName));
    }

    private static bool IsSupportedFinalShapePostOperation(ExecutionNode node, string finalTableName)
    {
        return node switch
        {
            ExecutionDistinctTable distinct => distinct.Target.Name == finalTableName,
            ExecutionSortTable sort => sort.Target.Name == finalTableName,
            ExecutionTopNTable topN => topN.Target.Name == finalTableName,
            ExecutionTopOffsetTable topOffset => topOffset.Target.Name == finalTableName,
            ExecutionSkipTable skip => skip.Target.Name == finalTableName,
            ExecutionTakeTable take => take.Target.Name == finalTableName,
            ExecutionSliceTable slice => slice.Target.Name == finalTableName,
            _ => false
        };
    }

}
