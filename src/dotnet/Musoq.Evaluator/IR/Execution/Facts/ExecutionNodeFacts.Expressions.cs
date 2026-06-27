using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution.Facts;

internal static partial class ExecutionNodeFacts
{
    internal static IEnumerable<ExecutionExpression> GetContextLayoutExpressions(ExecutionContextLayout? contextLayout)
    {
        return contextLayout == null
            ? []
            : contextLayout.Segments.Select(static segment => segment.Value);
    }

    internal static IEnumerable<ExecutionExpression> GetLocalExpressions(ExecutionNode node)
    {
        var windowExpressions = GetWindowExpressions(node);
        if (windowExpressions != null)
            return windowExpressions;

        switch (node)
        {
            case ExecutionSourceScan sourceScan:
                return sourceScan.Binding.Arguments;
            case ExecutionInterpretSource interpret:
                return interpret.Arguments;
            case ExecutionEnumerableSource enumerable:
                return [enumerable.Source];
            case ExecutionCreateValuesRows valuesRows:
                return valuesRows.Values.SelectMany(static row => row).Select(static value => value.Value);
            case ExecutionForEach forEach:
                return [forEach.Source];
            case ExecutionForEachWithOrdinality forEach:
                return [forEach.Source];
            case ExecutionParallelSingleKeyAggregateLoop parallelAggregate:
                return [parallelAggregate.SourceRows, parallelAggregate.Key, parallelAggregate.SerialLoop.Source];
            case ExecutionParallelFilterProjectLoop parallelProject:
                return new[] { parallelProject.SourceRows, parallelProject.SerialLoop.Source }
                    .Concat(OptionalExpression(parallelProject.Predicate))
                    .Concat(parallelProject.AppendRow.Values.Select(static value => value.Value))
                    .Concat(parallelProject.AppendRow.Contexts)
                    .Concat(GetContextLayoutExpressions(parallelProject.AppendRow.ContextLayout));
            case ExecutionLet let:
                return [let.Value];
            case ExecutionHoistCandidateLet candidate:
                return [candidate.Value];
            case ExecutionAssign assign:
                return [assign.Value];
            case ExecutionArrayAssign arrayAssign:
                return [arrayAssign.Index, arrayAssign.Value];
            case ExecutionContinueIf continueIf:
                return [continueIf.Condition];
            case ExecutionIf ifNode:
                return [ifNode.Condition];
            case ExecutionCreateGeneratedRow createRow:
                return createRow.Values.Select(static value => value.Value)
                    .Concat(createRow.Contexts)
                    .Concat(GetContextLayoutExpressions(createRow.ContextLayout));
            case ExecutionCreateHashPayload createPayload:
                return createPayload.Values.Select(static value => value.Value);
            case ExecutionAppendRow appendRow:
                return appendRow.Values.Select(static value => value.Value)
                    .Concat(appendRow.Contexts)
                    .Concat(GetContextLayoutExpressions(appendRow.ContextLayout));
            case ExecutionAppendExistingRow:
                return [];
            case ExecutionAppendRecord appendRecord:
                return appendRecord.Values.Select(static value => value.Value);
            case ExecutionMaterializeList materialize:
                return [materialize.Source];
            case ExecutionMaterializeFilteredList materialize:
                return [materialize.Source, materialize.Predicate];
            case ExecutionMaterializeExpandoList materialize:
                return materialize.Predicate == null
                    ? [materialize.Source]
                    : [materialize.Source, materialize.Predicate];
            case ExecutionHashAdd hashAdd:
                return [hashAdd.Key];
            case ExecutionHashProbe hashProbe:
                return [hashProbe.Key];
            case ExecutionKeySetAdd keySetAdd:
                return [keySetAdd.Key];
            case ExecutionKeySetProbe keySetProbe:
                return [keySetProbe.Key];
            case ExecutionStoreCteIndex:
            case ExecutionLoadCteIndex:
            case ExecutionCteSidecarIndexStoreCandidate:
            case ExecutionCteSidecarIndexLoadCandidate:
            case ExecutionCteSidecarIndexBuildCandidate:
            case ExecutionCteSidecarAppendRewriteCandidate:
                return [];
            case ExecutionCreateAsOfIndex createIndex:
                return new[] { createIndex.Candidates }
                    .Concat(createIndex.EqualityKeys.Select(static key => key.Right))
                    .Concat([createIndex.CandidateKey])
                    .Concat(OptionalExpression(createIndex.TieBreak?.Key));
            case ExecutionAsOfProbe asOfProbe:
                return new[] { asOfProbe.Candidates }
                    .Concat(asOfProbe.EqualityKeys.SelectMany(static key => new[] { key.Left, key.Right }))
                    .Concat([asOfProbe.ProbeKey, asOfProbe.CandidateKey])
                    .Concat(OptionalExpression(asOfProbe.TieBreak?.Key));
            case ExecutionCreateRangeIndex createIndex:
                return [createIndex.Candidates, createIndex.CandidateKey];
            case ExecutionRangeProbe rangeProbe:
                return [rangeProbe.ProbeKey];
            case ExecutionGetOrAddSingleKeyAggregateGroup getOrAdd:
                return [getOrAdd.Key];
            case ExecutionGetOrAddValueTupleAggregateGroup getOrAdd:
                return getOrAdd.Keys;
            case ExecutionAggregateSet aggregateSet:
                return aggregateSet.AccumulatorInput == null
                    ? aggregateSet.Arguments
                    : aggregateSet.Arguments.Concat([aggregateSet.AccumulatorInput]);
            case ExecutionAggregateCapturedValueSet capturedValueSet:
                return [capturedValueSet.Value];
            case ExecutionReturnDesc returnDesc:
                return returnDesc.Arguments;
            default:
                return [];
        }
    }

    private static IEnumerable<ExecutionExpression>? GetWindowExpressions(ExecutionNode node)
    {
        if (!TryGetWindowComputation(node, out var window))
            return null;

        var keyExpressions = OptionalExpression(window.PartitionKey)
            .Concat(window.OrderKeys.Select(static key => key.Expression));

        return node switch
        {
            ExecutionComputeRankingWindow => keyExpressions,
            ExecutionComputeOffsetWindow offset => keyExpressions.Concat([offset.Value, offset.Offset, offset.DefaultValue]),
            ExecutionComputePluginWindow plugin => keyExpressions.Concat([plugin.Value]).Concat(plugin.Arguments),
            ExecutionWindowAggregateKernel kernel => keyExpressions.Concat([kernel.Value]),
            _ => keyExpressions
        };
    }

    private static IEnumerable<ExecutionExpression> OptionalExpression(ExecutionExpression? expression)
    {
        if (expression != null)
            yield return expression;
    }
}
