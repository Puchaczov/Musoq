using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static ExecutionExpression? GetLocalExpressionAt(ExecutionNode node, int index)
    {
        var paging = PagingCountExecution.ReadAt(node, index);
        if (paging is not null)
            return paging;
        return node switch
        {
            ExecutionPrepareStructuralInput structuralInput => index switch { 0 => structuralInput.Input, _ => null },
            ExecutionSourceScan sourceScan => ExecutionExpressionChildren.At(sourceScan.Binding.Arguments, index),
            ExecutionInterpretSource interpret => ExecutionExpressionChildren.At(interpret.Arguments, index),
            ExecutionEnumerableSource enumerable => index switch { 0 => enumerable.Source, _ => null },
            ExecutionForEach forEach => index switch { 0 => forEach.Source, _ => null },
            ExecutionForEachWithOrdinality forEach => index switch { 0 => forEach.Source, _ => null },
            ExecutionParallelSingleKeyAggregateLoop parallelAggregate => index switch { 0 => parallelAggregate.SourceRows, 1 => parallelAggregate.Key, _ => null },
            ExecutionLet let => index switch { 0 => let.Value, _ => null },
            ExecutionHoistCandidateLet candidate => index switch { 0 => candidate.Value, _ => null },
            ExecutionAssign assign => index switch { 0 => assign.Value, _ => null },
            ExecutionArrayAssign arrayAssign => index switch { 0 => arrayAssign.Index, 1 => arrayAssign.Value, _ => null },
            ExecutionContinueIf continueIf => index switch { 0 => continueIf.Condition, _ => null },
            ExecutionIf ifNode => index switch { 0 => ifNode.Condition, _ => null },
            ExecutionMaterializeList materialize => index switch { 0 => materialize.Source, _ => null },
            ExecutionMaterializeFilteredList materialize => index switch { 0 => materialize.Source, 1 => materialize.Predicate, _ => null },
            ExecutionHashAdd hashAdd => index switch { 0 => hashAdd.Key, _ => null },
            ExecutionHashProbe hashProbe => index switch { 0 => hashProbe.Key, _ => null },
            ExecutionKeySetAdd keySetAdd => index switch { 0 => keySetAdd.Key, _ => null },
            ExecutionKeySetProbe keySetProbe => index switch { 0 => keySetProbe.Key, _ => null },
            ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd => index switch { 0 => getOrAdd.Key, _ => null },
            ExecutionGetOrAddValueTupleAggregateGroup getOrAdd => ExecutionExpressionChildren.At(getOrAdd.Keys, index),
            ExecutionAggregateCapturedValueSet capturedValueSet => index switch { 0 => capturedValueSet.Value, _ => null },
            ExecutionReturnDesc returnDesc => ExecutionExpressionChildren.At(returnDesc.Arguments, index),
            ExecutionCreateValuesRows rows => ValuesRowAt(rows, index),
            ExecutionParallelFilterProjectLoop parallel => ParallelExpressionAt(parallel, index),
            ExecutionCreateGeneratedRow row => RowExpressionAt(row.Values, row.Contexts, row.ContextLayout, index),
            ExecutionRecursiveCteAppend append => RowExpressionAt(append.AppendRow.Values, append.AppendRow.Contexts, append.AppendRow.ContextLayout, index),
            ExecutionAppendRow row => RowExpressionAt(row.Values, row.Contexts, row.ContextLayout, index),
            ExecutionCreateHashPayload payload => RowValueAt(payload.Values, index),
            ExecutionAppendRecord record => RowValueAt(record.Values, index),
            ExecutionCreateAsOfIndex create => AsOfCreateAt(create, index),
            ExecutionAsOfProbe probe => AsOfProbeAt(probe, index),
            ExecutionCreateRangeIndex create => RangeCreateAt(create, index),
            ExecutionRangeProbe probe => RangeProbeAt(probe, index),
            ExecutionAggregateSet aggregate => AggregateAt(aggregate, index),
            ExecutionComputeRankingWindow or ExecutionComputeOffsetWindow or ExecutionComputePluginWindow or ExecutionWindowAggregateKernel => WindowExpressionAt(node, index),
            _ => null
        };
    }

    private static ExecutionExpression? RowValueAt(IReadOnlyList<ExecutionRowValue> values, int index) =>
        (uint)index < (uint)values.Count ? values[index].Value : null;

    private static ExecutionExpression? RowExpressionAt(IReadOnlyList<ExecutionRowValue> values,
        IReadOnlyList<ExecutionExpression> contexts, ExecutionContextLayout? layout, int index)
    {
        if (index < values.Count)
            return values[index].Value;
        index -= values.Count;
        if (index < contexts.Count)
            return contexts[index];
        index -= contexts.Count;
        return layout is not null && index < layout.Segments.Count ? layout.Segments[index].Value : null;
    }

    private static ExecutionExpression? ParallelExpressionAt(ExecutionParallelFilterProjectLoop node, int index)
    {
        if (index == 0)
            return node.SourceRows;
        index--;
        if (node.Predicate is not null)
        {
            if (index == 0)
                return node.Predicate;
            index--;
        }
        return RowExpressionAt(node.AppendRow.Values, node.AppendRow.Contexts, node.AppendRow.ContextLayout, index);
    }

    private static ExecutionExpression? ValuesRowAt(ExecutionCreateValuesRows node, int index)
    {
        for (var row = 0; row < node.Values.Count; row++)
        {
            if (index < node.Values[row].Count)
                return node.Values[row][index].Value;
            index -= node.Values[row].Count;
        }
        return null;
    }

    private static ExecutionExpression? AggregateAt(ExecutionAggregateSet node, int index)
    {
        var count = Math.Max(0, node.Arguments.Count - 1);
        if (index < count)
            return node.Arguments[index + 1];
        index -= count;
        if (node.FilterPredicate is not null)
        {
            if (index == 0)
                return node.FilterPredicate;
            index--;
        }
        return index == 0 ? node.AccumulatorInput : null;
    }
}
