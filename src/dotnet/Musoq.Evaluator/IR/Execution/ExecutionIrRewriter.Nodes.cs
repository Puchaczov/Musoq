using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal abstract partial class ExecutionIrRewriter
{
    protected virtual ExecutionNode RewriteSourceScan(ExecutionSourceScan node)
    {
        var binding = RewriteSourceBinding(node.Binding);
        return ReferenceEquals(binding, node.Binding) ? node : node with { Binding = binding };
    }

    protected virtual ExecutionNode RewriteInterpretSource(ExecutionInterpretSource node)
    {
        var arguments = RewriteExpressionList(node.Arguments);
        return ReferenceEquals(arguments, node.Arguments) ? node : node with { Arguments = arguments };
    }

    protected virtual ExecutionNode RewriteEnumerableSource(ExecutionEnumerableSource node)
    {
        return RewriteExpressionOwner(node, node.Source, source => node with { Source = source });
    }

    protected virtual ExecutionNode RewriteCreateTable(ExecutionCreateTable node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteCreateRecordList(ExecutionCreateRecordList node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteCreateValuesRows(ExecutionCreateValuesRows node)
    {
        var values = RewriteRowValueRows(node.Values);
        return ReferenceEquals(values, node.Values) ? node : node with { Values = values };
    }

    protected virtual ExecutionNode RewriteRecursiveCte(ExecutionRecursiveCte node)
    {
        var anchor = RewriteBlock(node.Anchor);
        var invariantSetup = RewriteBlock(node.InvariantSetup);
        var recursiveMember = RewriteBlock(node.RecursiveMember);
        return ReferenceEquals(anchor, node.Anchor) &&
               ReferenceEquals(invariantSetup, node.InvariantSetup) &&
               ReferenceEquals(recursiveMember, node.RecursiveMember)
            ? node
            : node with
            {
                Anchor = anchor,
                InvariantSetup = invariantSetup,
                RecursiveMember = recursiveMember
            };
    }

    protected virtual ExecutionNode RewriteRecursiveCteAppend(ExecutionRecursiveCteAppend node)
    {
        var append = (ExecutionAppendRow)RewriteAppendRow(node.AppendRow);
        return ReferenceEquals(append, node.AppendRow) ? node : node with { AppendRow = append };
    }

    protected virtual ExecutionNode RewriteRecursiveCteSnapshotRowGuard(
        ExecutionRecursiveCteSnapshotRowGuard node) => node;

    protected virtual ExecutionNode RewriteCreateBoundedRecordList(ExecutionCreateBoundedRecordList node) => node;

    protected virtual ExecutionNode RewriteForEach(ExecutionForEach node)
    {
        return RewriteExpressionAndBlockOwner(
            node,
            node.Source,
            node.Body,
            (source, body) => node with { Source = source, Body = body });
    }

    protected virtual ExecutionNode RewriteForEachWithOrdinality(ExecutionForEachWithOrdinality node)
    {
        return RewriteExpressionAndBlockOwner(
            node,
            node.Source,
            node.Body,
            (source, body) => node with { Source = source, Body = body });
    }

    protected virtual ExecutionNode RewriteForEachIndexed(ExecutionForEachIndexed node)
    {
        var body = RewriteBlock(node.Body);
        return ReferenceEquals(body, node.Body) ? node : node with { Body = body };
    }

    protected virtual ExecutionNode RewriteParallelBlock(ExecutionParallelBlock node)
    {
        var tasks = RewriteList(node.Tasks, RewriteParallelTask);
        var merge = RewriteParallelMerge(node.Merge);
        return ReferenceEquals(tasks, node.Tasks) && ReferenceEquals(merge, node.Merge)
            ? node
            : node with { Tasks = tasks, Merge = merge };
    }

    protected virtual ExecutionNode RewriteParallelFilterProjectLoop(ExecutionParallelFilterProjectLoop node)
    {
        var sourceRows = RewriteExpression(node.SourceRows);
        var predicate = RewriteOptionalExpression(node.Predicate);
        var appendRow = (ExecutionAppendRow)RewriteAppendRow(node.AppendRow);
        var projectionBody = RewriteBlock(node.ProjectionBody);
        return ReferenceEquals(sourceRows, node.SourceRows) &&
               ReferenceEquals(predicate, node.Predicate) &&
               ReferenceEquals(appendRow, node.AppendRow) &&
               ReferenceEquals(projectionBody, node.ProjectionBody)
            ? node
            : node with
            {
                SourceRows = sourceRows,
                Predicate = predicate,
                AppendRow = appendRow,
                ProjectionBody = projectionBody
            };
    }

    protected virtual ExecutionNode RewriteParallelSingleKeyAggregateLoop(ExecutionParallelSingleKeyAggregateLoop node)
    {
        var sourceRows = RewriteExpression(node.SourceRows);
        var key = RewriteExpression(node.Key);
        var aggregateBody = RewriteBlock(node.AggregateBody);
        return ReferenceEquals(sourceRows, node.SourceRows) &&
               ReferenceEquals(key, node.Key) &&
               ReferenceEquals(aggregateBody, node.AggregateBody)
            ? node
            : node with
            {
                SourceRows = sourceRows,
                Key = key,
                AggregateBody = aggregateBody
            };
    }

    protected virtual ExecutionNode RewriteLet(ExecutionLet node)
    {
        return RewriteExpressionOwner(node, node.Value, value => node with { Value = value });
    }

    protected virtual ExecutionNode RewriteHoistCandidateLet(ExecutionHoistCandidateLet node)
    {
        return RewriteExpressionOwner(node, node.Value, value => node with { Value = value });
    }

    protected virtual ExecutionNode RewriteAssign(ExecutionAssign node)
    {
        return RewriteExpressionOwner(node, node.Value, value => node with { Value = value });
    }

    protected virtual ExecutionNode RewriteCreateBooleanArray(ExecutionCreateBooleanArray node) => node;

    protected virtual ExecutionNode RewriteArrayAssign(ExecutionArrayAssign node)
    {
        var index = RewriteExpression(node.Index);
        var value = RewriteExpression(node.Value);
        return ReferenceEquals(index, node.Index) && ReferenceEquals(value, node.Value)
            ? node
            : node with { Index = index, Value = value };
    }

    protected virtual ExecutionNode RewriteContinue(ExecutionContinue node) => node;

    protected virtual ExecutionNode RewriteContinueIf(ExecutionContinueIf node)
    {
        return RewriteExpressionOwner(node, node.Condition, condition => node with { Condition = condition });
    }

    protected virtual ExecutionNode RewriteBreak(ExecutionBreak node) => node;

    protected virtual ExecutionNode RewriteAdaptExpando(ExecutionAdaptExpando node) => node;

    protected virtual ExecutionNode RewriteCreateObject(ExecutionCreateObject node) => node;

    protected virtual ExecutionNode RewriteMethodTargetDeclarationCandidate(ExecutionMethodTargetDeclarationCandidate node) => node;

    protected virtual ExecutionNode RewriteIf(ExecutionIf node)
    {
        return RewriteExpressionAndBlockOwner(
            node,
            node.Condition,
            node.Body,
            (condition, body) => node with { Condition = condition, Body = body });
    }

    protected virtual ExecutionNode RewriteCreateGeneratedRow(ExecutionCreateGeneratedRow node)
    {
        return RewriteRowValuesAndContextsOwner(
            node,
            node.Values,
            node.Contexts,
            node.ContextLayout,
            (values, contexts, contextLayout) => node with
            {
                Values = values,
                Contexts = contexts,
                ContextLayout = contextLayout
            });
    }

    protected virtual ExecutionNode RewriteCreateHashPayload(ExecutionCreateHashPayload node)
    {
        return RewriteRowValuesOwner(node, node.Values, values => node with { Values = values });
    }

    protected virtual ExecutionNode RewriteAppendRow(ExecutionAppendRow node)
    {
        return RewriteRowValuesAndContextsOwner(
            node,
            node.Values,
            node.Contexts,
            node.ContextLayout,
            (values, contexts, contextLayout) => node with
            {
                Values = values,
                Contexts = contexts,
                ContextLayout = contextLayout
            });
    }

    protected virtual ExecutionNode RewriteAppendExistingRow(ExecutionAppendExistingRow node) => node;

    protected virtual ExecutionNode RewriteAppendRecord(ExecutionAppendRecord node)
    {
        return RewriteRowValuesOwner(node, node.Values, values => node with { Values = values });
    }

    protected virtual ExecutionNode RewriteMaterializeList(ExecutionMaterializeList node)
    {
        return RewriteExpressionOwner(node, node.Source, source => node with { Source = source });
    }

    protected virtual ExecutionNode RewriteMaterializeFilteredList(ExecutionMaterializeFilteredList node)
    {
        var source = RewriteExpression(node.Source);
        var predicate = RewriteExpression(node.Predicate);
        return ReferenceEquals(source, node.Source) && ReferenceEquals(predicate, node.Predicate)
            ? node
            : node with { Source = source, Predicate = predicate };
    }

    protected virtual ExecutionNode RewriteMaterializeExpandoList(ExecutionMaterializeExpandoList node)
    {
        var source = RewriteExpression(node.Source);
        var predicate = RewriteOptionalExpression(node.Predicate);
        return ReferenceEquals(source, node.Source) && ReferenceEquals(predicate, node.Predicate)
            ? node
            : node with { Source = source, Predicate = predicate };
    }

    protected virtual ExecutionNode RewriteWindowKernelPlan(ExecutionWindowKernelPlan node)
    {
        var kernels = RewriteList(node.Kernels, RewriteNode);
        return ReferenceEquals(kernels, node.Kernels)
            ? node
            : node with { Kernels = kernels };
    }

    protected virtual ExecutionNode RewriteComputeRankingWindow(ExecutionComputeRankingWindow node)
    {
        var partitionKey = RewriteOptionalExpression(node.PartitionKey);
        var orderKeys = RewriteWindowOrderKeys(node.OrderKeys);
        return ReferenceEquals(partitionKey, node.PartitionKey) && ReferenceEquals(orderKeys, node.OrderKeys)
            ? node
            : node with { PartitionKey = partitionKey, OrderKeys = orderKeys };
    }

    protected virtual ExecutionNode RewriteComputeOffsetWindow(ExecutionComputeOffsetWindow node)
    {
        var partitionKey = RewriteOptionalExpression(node.PartitionKey);
        var orderKeys = RewriteWindowOrderKeys(node.OrderKeys);
        var value = RewriteExpression(node.Value);
        var offset = RewriteExpression(node.Offset);
        var defaultValue = RewriteExpression(node.DefaultValue);
        return ReferenceEquals(partitionKey, node.PartitionKey) &&
               ReferenceEquals(orderKeys, node.OrderKeys) &&
               ReferenceEquals(value, node.Value) &&
               ReferenceEquals(offset, node.Offset) &&
               ReferenceEquals(defaultValue, node.DefaultValue)
            ? node
            : node with
            {
                PartitionKey = partitionKey,
                OrderKeys = orderKeys,
                Value = value,
                Offset = offset,
                DefaultValue = defaultValue
            };
    }

    protected virtual ExecutionNode RewriteComputePluginWindow(ExecutionComputePluginWindow node)
    {
        var partitionKey = RewriteOptionalExpression(node.PartitionKey);
        var orderKeys = RewriteWindowOrderKeys(node.OrderKeys);
        var value = RewriteExpression(node.Value);
        var arguments = RewriteExpressionList(node.Arguments);
        return ReferenceEquals(partitionKey, node.PartitionKey) &&
               ReferenceEquals(orderKeys, node.OrderKeys) &&
               ReferenceEquals(value, node.Value) &&
               ReferenceEquals(arguments, node.Arguments)
            ? node
            : node with
            {
                PartitionKey = partitionKey,
                OrderKeys = orderKeys,
                Value = value,
                Arguments = arguments
            };
    }

    protected virtual ExecutionNode RewriteWindowAggregateKernel(ExecutionWindowAggregateKernel node)
    {
        var partitionKey = RewriteOptionalExpression(node.PartitionKey);
        var orderKeys = RewriteWindowOrderKeys(node.OrderKeys);
        var value = RewriteExpression(node.Value);
        var filterPredicate = RewriteOptionalExpression(node.FilterPredicate);
        return ReferenceEquals(partitionKey, node.PartitionKey) &&
               ReferenceEquals(orderKeys, node.OrderKeys) &&
               ReferenceEquals(value, node.Value) &&
               ReferenceEquals(filterPredicate, node.FilterPredicate)
            ? node
            : node with { PartitionKey = partitionKey, OrderKeys = orderKeys, Value = value, FilterPredicate = filterPredicate };
    }

    protected virtual ExecutionNode RewriteCreateHash(ExecutionCreateHash node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteHashAdd(ExecutionHashAdd node)
    {
        return RewriteExpressionOwner(node, node.Key, key => node with { Key = key });
    }

    protected virtual ExecutionNode RewriteHashProbe(ExecutionHashProbe node)
    {
        return RewriteKeyBlockAndOptionalBlockOwner(
            node,
            node.Key,
            node.Body,
            node.NoMatchBody,
            (key, body, noMatchBody) => node with { Key = key, Body = body, NoMatchBody = noMatchBody });
    }

    protected virtual ExecutionNode RewriteCreateKeySet(ExecutionCreateKeySet node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteKeySetAdd(ExecutionKeySetAdd node)
    {
        return RewriteExpressionOwner(node, node.Key, key => node with { Key = key });
    }

    protected virtual ExecutionNode RewriteKeySetProbe(ExecutionKeySetProbe node)
    {
        return RewriteKeyBlockAndOptionalBlockOwner(
            node,
            node.Key,
            node.Body,
            node.NoMatchBody,
            (key, body, noMatchBody) => node with { Key = key, Body = body, NoMatchBody = noMatchBody });
    }


    protected virtual ExecutionNode RewriteStoreCteIndex(ExecutionStoreCteIndex node) => node;

    protected virtual ExecutionNode RewriteLoadCteIndex(ExecutionLoadCteIndex node) => node;

    protected virtual ExecutionNode RewriteCreateAsOfIndex(ExecutionCreateAsOfIndex node)
    {
        var candidates = RewriteExpression(node.Candidates);
        var equalityKeys = RewriteAsOfEqualityKeys(node.EqualityKeys);
        var candidateKey = RewriteExpression(node.CandidateKey);
        var tieBreak = RewriteAsOfTieBreak(node.TieBreak);
        return ReferenceEquals(candidates, node.Candidates) &&
               ReferenceEquals(equalityKeys, node.EqualityKeys) &&
               ReferenceEquals(candidateKey, node.CandidateKey) &&
               ReferenceEquals(tieBreak, node.TieBreak)
            ? node
            : node with
            {
                Candidates = candidates,
                EqualityKeys = equalityKeys,
                CandidateKey = candidateKey,
                TieBreak = tieBreak
            };
    }

    protected virtual ExecutionNode RewriteAsOfProbe(ExecutionAsOfProbe node)
    {
        var candidates = RewriteExpression(node.Candidates);
        var equalityKeys = RewriteAsOfEqualityKeys(node.EqualityKeys);
        var probeKey = RewriteExpression(node.ProbeKey);
        var candidateKey = RewriteExpression(node.CandidateKey);
        var tieBreak = RewriteAsOfTieBreak(node.TieBreak);
        var body = RewriteBlock(node.Body);
        var noMatchBody = RewriteOptionalBlock(node.NoMatchBody);
        return ReferenceEquals(candidates, node.Candidates) &&
               ReferenceEquals(equalityKeys, node.EqualityKeys) &&
               ReferenceEquals(probeKey, node.ProbeKey) &&
               ReferenceEquals(candidateKey, node.CandidateKey) &&
               ReferenceEquals(tieBreak, node.TieBreak) &&
               ReferenceEquals(body, node.Body) &&
               ReferenceEquals(noMatchBody, node.NoMatchBody)
            ? node
            : node with
            {
                Candidates = candidates,
                EqualityKeys = equalityKeys,
                ProbeKey = probeKey,
                CandidateKey = candidateKey,
                TieBreak = tieBreak,
                Body = body,
                NoMatchBody = noMatchBody
            };
    }

    protected virtual ExecutionNode RewriteCreateRangeIndex(ExecutionCreateRangeIndex node)
    {
        var candidates = RewriteExpression(node.Candidates);
        var partitionKeys = node.PartitionKeys == null ? null : RewriteAsOfEqualityKeys(node.PartitionKeys);
        var candidateKey = RewriteExpression(node.CandidateKey);
        return ReferenceEquals(candidates, node.Candidates) &&
               ReferenceEquals(partitionKeys, node.PartitionKeys) &&
               ReferenceEquals(candidateKey, node.CandidateKey)
            ? node
            : node with { Candidates = candidates, PartitionKeys = partitionKeys, CandidateKey = candidateKey };
    }

    protected virtual ExecutionNode RewriteRangeProbe(ExecutionRangeProbe node)
    {
        var partitionKeys = node.PartitionKeys == null ? null : RewriteAsOfEqualityKeys(node.PartitionKeys);
        var probeKey = RewriteExpression(node.ProbeKey);
        var body = RewriteBlock(node.Body);
        var noMatchBody = RewriteOptionalBlock(node.NoMatchBody);
        return ReferenceEquals(partitionKeys, node.PartitionKeys) &&
               ReferenceEquals(probeKey, node.ProbeKey) &&
               ReferenceEquals(body, node.Body) &&
               ReferenceEquals(noMatchBody, node.NoMatchBody)
            ? node
            : node with
            {
                PartitionKeys = partitionKeys,
                ProbeKey = probeKey,
                Body = body,
                NoMatchBody = noMatchBody
            };
    }

    protected virtual ExecutionNode RewriteCreateAggregateLibrary(ExecutionCreateAggregateLibrary node) => node;

    protected virtual ExecutionNode RewriteCreateAggregateContext(ExecutionCreateAggregateContext node) => node;

    protected virtual ExecutionNode RewriteEnsureAggregateGroup(ExecutionEnsureAggregateGroup node) => node;

    protected virtual ExecutionNode RewriteCreateSingleKeyAggregateContext(ExecutionCreateSingleKeyAggregateContext node) => node;

    protected virtual ExecutionNode RewriteGetOrAddSingleKeyAggregateGroup(ExecutionGetOrAddSingleKeyAggregateGroup node)
    {
        return RewriteExpressionOwner(node, node.Key, key => node with { Key = key });
    }

    protected virtual ExecutionNode RewriteCreateValueTupleAggregateContext(ExecutionCreateValueTupleAggregateContext node) => node;

    protected virtual ExecutionNode RewriteGetOrAddValueTupleAggregateGroup(ExecutionGetOrAddValueTupleAggregateGroup node)
    {
        var keys = RewriteExpressionList(node.Keys);
        return ReferenceEquals(keys, node.Keys) ? node : node with { Keys = keys };
    }

    protected virtual ExecutionNode RewriteAggregateSet(ExecutionAggregateSet node)
    {
        var arguments = RewriteExpressionList(node.Arguments);
        var filterPredicate = RewriteOptionalExpression(node.FilterPredicate);
        var accumulatorInput = RewriteOptionalExpression(node.AccumulatorInput);
        return ReferenceEquals(arguments, node.Arguments) &&
               ReferenceEquals(filterPredicate, node.FilterPredicate) &&
               ReferenceEquals(accumulatorInput, node.AccumulatorInput)
            ? node
            : node with { Arguments = arguments, FilterPredicate = filterPredicate, AccumulatorInput = accumulatorInput };
    }

    protected virtual ExecutionNode RewriteAggregateCapturedValueSet(ExecutionAggregateCapturedValueSet node)
    {
        return RewriteExpressionOwner(node, node.Value, value => node with { Value = value });
    }

    protected virtual ExecutionNode RewriteSetOperation(ExecutionSetOperation node) => node;

    protected virtual ExecutionNode RewriteDistinctTable(ExecutionDistinctTable node) => node;

    protected virtual ExecutionNode RewriteSortTable(ExecutionSortTable node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteTopNTable(ExecutionTopNTable node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteTopOffsetTable(ExecutionTopOffsetTable node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteSkipTable(ExecutionSkipTable node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteTakeTable(ExecutionTakeTable node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteSliceTable(ExecutionSliceTable node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteProjectTable(ExecutionProjectTable node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteOrderRecordList(ExecutionOrderRecordList node) => node;

    protected virtual ExecutionNode RewriteMaterializeRecordListToTable(ExecutionMaterializeRecordListToTable node)
    {
        return RewriteCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteStoreTable(ExecutionStoreTable node) => node;

    protected virtual ExecutionNode RewriteRelatedCtePhase(ExecutionRelatedCtePhase node) => node;

    protected virtual ExecutionNode RewriteFusedCteProducer(ExecutionFusedCteProducer node)
    {
        return RewriteBlockOwner(node, node.Body, body => node with { Body = body });
    }

    protected virtual ExecutionNode RewriteSingleUsePipelineFusionCandidate(ExecutionSingleUsePipelineFusionCandidate node)
    {
        return RewriteBlockOwner(node, node.Body, body => node with { Body = body });
    }

    protected virtual ExecutionNode RewriteCteReadOnceFusionCandidate(ExecutionCteReadOnceFusionCandidate node)
    {
        return RewriteBlockOwner(node, node.Body, body => node with { Body = body });
    }

    protected virtual ExecutionNode RewriteCteSidecarIndexStoreCandidate(ExecutionCteSidecarIndexStoreCandidate node) => node;

    protected virtual ExecutionNode RewriteCteSidecarIndexLoadCandidate(ExecutionCteSidecarIndexLoadCandidate node) => node;

    protected virtual ExecutionNode RewriteCteSidecarIndexBuildCandidate(ExecutionCteSidecarIndexBuildCandidate node)
    {
        var indexes = node.Indexes
            .Select(spec =>
            {
                return RewriteCapacityHintOwner(
                    spec,
                    spec.CapacityHint,
                    capacityHint => spec with { CapacityHint = capacityHint });
            })
            .ToArray();

        return indexes.SequenceEqual(node.Indexes) ? node : node with { Indexes = indexes };
    }

    protected virtual ExecutionNode RewriteCteSidecarAppendRewriteCandidate(ExecutionCteSidecarAppendRewriteCandidate node)
    {
        var appendRow = (ExecutionAppendRow)RewriteAppendRow(node.AppendRow);
        var indexes = node.Indexes
            .Select(spec =>
            {
                var key = RewriteExpression(spec.Key);
                var payloadValues = spec.PayloadValues
                    .Select(value =>
                    {
                        var rewritten = RewriteExpression(value.Value);
                        return ReferenceEquals(rewritten, value.Value)
                            ? value
                            : value with { Value = rewritten };
                    })
                    .ToArray();

                return ReferenceEquals(key, spec.Key) && payloadValues.SequenceEqual(spec.PayloadValues)
                    ? spec
                    : spec with { Key = key, PayloadValues = payloadValues };
            })
            .ToArray();

        return ReferenceEquals(appendRow, node.AppendRow) && indexes.SequenceEqual(node.Indexes)
            ? node
            : node with { AppendRow = appendRow, Indexes = indexes };
    }

    protected virtual ExecutionNode RewriteCteFusedProducerCandidate(ExecutionCteFusedProducerCandidate node)
    {
        return RewriteBlockOwner(node, node.Body, body => node with { Body = body });
    }

    protected virtual ExecutionNode RewriteCteIndexOnlyStorageCandidate(ExecutionCteIndexOnlyStorageCandidate node) => node;

    protected virtual ExecutionNode RewriteEnsureTableCapacity(ExecutionEnsureTableCapacity node)
    {
        return RewriteRequiredCapacityHintOwner(node, node.CapacityHint, capacityHint => node with { CapacityHint = capacityHint });
    }

    protected virtual ExecutionNode RewriteReturnDesc(ExecutionReturnDesc node)
    {
        var arguments = RewriteExpressionList(node.Arguments);
        return ReferenceEquals(arguments, node.Arguments) ? node : node with { Arguments = arguments };
    }

    protected virtual ExecutionNode RewriteReturnTable(ExecutionReturnTable node) => node;
}
