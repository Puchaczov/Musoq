using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Musoq.Evaluator.IR.Execution.Facts;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool CanRenderExpressions(IEnumerable<ExecutionExpression> expressions) => expressions.All(CanRenderExpression);

    private static bool CanRenderOptionalExpression(ExecutionExpression? expression) => expression == null || CanRenderExpression(expression);

    private static bool CanRenderValuesRows(ExecutionCreateValuesRows valuesRows) => valuesRows.Values.All(CanRenderRowValues);

    private static bool CanRenderRowValues(IEnumerable<ExecutionRowValue> values) => values.All(value => CanRenderExpression(value.Value));

    private static bool CanRenderRowConstruction(
        IEnumerable<ExecutionRowValue> values,
        IEnumerable<ExecutionExpression> contexts,
        ExecutionContextLayout? contextLayout)
    {
        return CanRenderRowValues(values) &&
               CanRenderExpressions(contexts) &&
               CanRenderExpressions(GetContextLayoutExpressions(contextLayout));
    }

    private static bool CanRenderBoundedRecordList(ExecutionCreateBoundedRecordList createList)
    {
        return CanRenderOrderKeys(createList.Keys) &&
               CanRenderGeneratedRecordShape(createList.RecordShape) &&
               createList.Selection is ExecutionTakeOrderRecordSelection or ExecutionSkipTakeOrderRecordSelection;
    }

    private static bool CanRenderParallelSingleKeyAggregateLoop(ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        return parallelAggregate is { MaxDegreeOfParallelism: > 1, Threshold: > 0, GroupShape.RequiresParentLinks: false } &&
               CanRenderExpression(parallelAggregate.SourceRows) &&
               CanRenderExpression(parallelAggregate.Key) &&
               CanReferenceType(parallelAggregate.Source.Type) &&
               CanReferenceType(parallelAggregate.KeyType) &&
               CanRenderAggregateGroupShape(parallelAggregate.GroupShape) &&
               CanRenderBlock(parallelAggregate.AggregateBody);
    }

    private static bool CanRenderParallelFilterProjectLoop(ExecutionParallelFilterProjectLoop parallelProject)
    {
        return parallelProject is { MaxDegreeOfParallelism: > 1, Threshold: > 0 } &&
               CanReferenceType(parallelProject.Source.Type) &&
               CanRenderExpression(parallelProject.SourceRows) &&
               CanRenderOptionalExpression(parallelProject.Predicate) &&
               CanRenderRowConstruction(
                   parallelProject.AppendRow.Values,
                   parallelProject.AppendRow.Contexts,
                   parallelProject.AppendRow.ContextLayout) &&
               CanRenderBlock(parallelProject.ProjectionBody);
    }

    private static bool CanRenderParallelBlock(ExecutionParallelBlock parallel)
    {
        return parallel is { MaxDegreeOfParallelism: > 0, Tasks.Count: > 0 } &&
               parallel.Tasks.All(CanRenderParallelTaskOutput) &&
               parallel.Tasks.All(task => CanRenderBlock(task.Body)) &&
               CanRenderBlock(parallel.Merge.Body);
    }

    private static bool CanRenderParallelTaskOutput(ExecutionParallelTask task)
    {
        if (task.Output.Type.RequireClrType() == typeof(Table) ||
            !string.IsNullOrWhiteSpace(task.Output.GeneratedRowTypeName))
        {
            return true;
        }

        return task.Output.Type.RequireClrType() == typeof(object) &&
               task.RelatedTableIndex == null &&
               ExecutionIrAnalysis.CollectNodes<ExecutionStoreCteIndex>(task.Body).Any();
    }

    private static bool CanRenderDefaultConstructibleObject(Type targetType) => CanReferenceType(targetType) && targetType.GetConstructor(Type.EmptyTypes) != null;

    private static bool CanRenderCreateHashPayload(ExecutionCreateHashPayload createPayload)
    {
        return createPayload.Values.Count == GetHashPayloadFields(createPayload.PayloadShape).Length &&
               CanRenderHashPayloadShape(createPayload.PayloadShape) &&
               CanRenderRowValues(createPayload.Values);
    }

    private static bool CanRenderAppendRecord(ExecutionAppendRecord appendRecord)
    {
        var valueCount = appendRecord.Values.Count;
        var fieldCount = appendRecord.RecordShape.Fields.Count;
        return (valueCount == fieldCount || valueCount + 1 == fieldCount) &&
               CanRenderRowValues(appendRecord.Values);
    }

    private static bool CanRenderRankingWindow(ExecutionComputeRankingWindow ranking)
    {
        return ExecutionNodeFacts.TryGetWindowComputation(ranking, out var window) &&
               CanRenderWindowComputationCommon(window, requireOrderKeys: true) &&
               ranking.Results.Type.RequireClrType() == typeof(long[]);
    }

    private static bool CanRenderOffsetWindow(ExecutionComputeOffsetWindow offset)
    {
        return ExecutionNodeFacts.TryGetWindowComputation(offset, out var window) &&
               CanRenderWindowComputationCommon(window, requireOrderKeys: true) &&
               offset.Results.Type.RequireClrType().IsArray &&
               CanRenderExpression(offset.Value) &&
               CanRenderExpression(offset.Offset) &&
               CanRenderExpression(offset.DefaultValue);
    }

    private static bool CanRenderPluginWindow(ExecutionComputePluginWindow plugin)
    {
        return ExecutionNodeFacts.TryGetWindowComputation(plugin, out var window) &&
               CanRenderWindowComputationCommon(window, requireOrderKeys: false) &&
               CanRenderPluginWindowResults(plugin) &&
               (IsBuiltInDirectPluginWindow(plugin) || CanRenderStreamingPluginWindow(plugin)) &&
               CanRenderExpression(plugin.Value) &&
               CanRenderExpressions(plugin.Arguments);
    }

    private static bool CanRenderWindowComputationCommon(
        ExecutionWindowComputationMetadata window,
        bool requireOrderKeys)
    {
        return CanUseIndexedLoopItem(window.Item, window.RowAccessMode) &&
               CanRenderWindowOrderKeys(window.OrderKeys, requireOrderKeys) &&
               CanRenderOptionalExpression(window.PartitionKey);
    }

    private static bool CanRenderWindowOrderKeys(IReadOnlyList<ExecutionWindowOrderKey> keys, bool requireAny) =>
        (!requireAny || keys.Count > 0) && keys.All(key => CanRenderExpression(key.Expression));

    private static bool CanRenderHashTypes(Type keyType, Type rowType)
    {
        return CanReferenceType(keyType) &&
               CanReferenceType(rowType) &&
               rowType != typeof(DynamicObject);
    }

    private static bool CanRenderHashTypes(ExecutionTypeRef keyType, ExecutionTypeRef rowType) =>
        CanRenderHashTypes(keyType.RequireClrType(), rowType.RequireClrType());

    private static bool CanRenderHashAdd(ExecutionHashAdd hashAdd)
    {
        return CanRenderExpression(hashAdd.Key) &&
               CanRenderHashTypes(hashAdd.KeyType, hashAdd.RowType) &&
               hashAdd.RowType.RequireClrType().IsAssignableFrom(hashAdd.Row.Type.RequireClrType());
    }

    private static bool CanRenderHashProbe(ExecutionHashProbe hashProbe)
    {
        return CanRenderExpression(hashProbe.Key) &&
               CanRenderHashTypes(hashProbe.KeyType, hashProbe.RowType) &&
               CanRenderMatchFound(hashProbe.MatchFound) &&
               CanRenderBlock(hashProbe.Body) &&
               CanRenderOptionalBlock(hashProbe.NoMatchBody);
    }

    private static bool CanRenderStrictKeyType(Type keyType) => CanReferenceType(keyType) && keyType != typeof(object);

    private static bool CanRenderStrictKeyType(ExecutionTypeRef keyType) =>
        CanRenderStrictKeyType(keyType.RequireClrType());

    private static bool CanRenderKeySetProbe(ExecutionKeySetProbe keySetProbe)
    {
        return CanRenderExpression(keySetProbe.Key) &&
               CanRenderStrictKeyType(keySetProbe.KeyType) &&
               CanRenderMatchFound(keySetProbe.MatchFound) &&
               CanRenderBlock(keySetProbe.Body) &&
               CanRenderOptionalBlock(keySetProbe.NoMatchBody);
    }

    private static bool CanRenderCteIndexLoad(ExecutionLoadCteIndex loadCteIndex)
    {
        return loadCteIndex.Kind switch
        {
            ExecutionCteSidecarIndexKind.Hash => loadCteIndex.RowType != null &&
                                                 CanRenderHashTypes(loadCteIndex.KeyType.RequireClrType(), loadCteIndex.RowType.RequireClrType()),
            ExecutionCteSidecarIndexKind.KeySet => CanRenderStrictKeyType(loadCteIndex.KeyType),
            _ => false
        };
    }

    private static bool CanRenderMatchFound(ExecutionVariable? matchFound) =>
        matchFound == null || matchFound.Type.RequireClrType() == typeof(bool);

    private static bool CanRenderCreateAsOfIndex(ExecutionCreateAsOfIndex createIndex)
    {
        return CanRenderExpression(createIndex.Candidates) &&
               CanRenderExpression(createIndex.CandidateKey) &&
               CanRenderOptionalExpression(createIndex.TieBreak?.Key) &&
               (createIndex.TieBreak == null || CanReferenceType(createIndex.TieBreak.Key.ReturnType)) &&
               createIndex.EqualityKeys.All(static key => CanRenderExpression(key.Right)) &&
               CanRenderJoinEntityType(createIndex.Candidate.Type);
    }

    private static bool CanRenderAsOfProbe(ExecutionAsOfProbe asOfProbe)
    {
        return CanRenderExpression(asOfProbe.Candidates) &&
               CanRenderExpression(asOfProbe.ProbeKey) &&
               CanRenderExpression(asOfProbe.CandidateKey) &&
               CanRenderOptionalExpression(asOfProbe.TieBreak?.Key) &&
               (asOfProbe.TieBreak == null || CanReferenceType(asOfProbe.TieBreak.Key.ReturnType)) &&
               asOfProbe.EqualityKeys.All(static key => CanRenderExpression(key.Left) && CanRenderExpression(key.Right)) &&
               CanRenderJoinEntityType(asOfProbe.Match.Type) &&
               asOfProbe.Candidate.Type == asOfProbe.Match.Type &&
               CanRenderBlock(asOfProbe.Body) &&
               CanRenderOptionalBlock(asOfProbe.NoMatchBody);
    }

    private static bool CanRenderCreateRangeIndex(ExecutionCreateRangeIndex createIndex)
    {
        return CanRenderExpression(createIndex.Candidates) &&
               CanRenderExpression(createIndex.CandidateKey) &&
               CanRenderJoinEntityType(createIndex.Candidate.Type) &&
               CanRenderStrictKeyType(createIndex.KeyType);
    }

    private static bool CanRenderRangeProbe(ExecutionRangeProbe rangeProbe)
    {
        return CanRenderJoinEntityType(rangeProbe.Match.Type) &&
               CanRenderStrictKeyType(rangeProbe.KeyType) &&
               CanRenderExpression(rangeProbe.ProbeKey) &&
               CanRenderBlock(rangeProbe.Body);
    }

    private static bool CanRenderJoinEntityType(Type entityType)
    {
        return CanReferenceType(entityType) &&
               !entityType.IsValueType &&
               !DynamicEntityBoundary.IsDynamicMetaObjectProvider(entityType);
    }

    private static bool CanRenderJoinEntityType(ExecutionTypeRef entityType) =>
        CanRenderJoinEntityType(entityType.RequireClrType());

    private static bool CanRenderSingleKeyAggregateLookup(ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup)
    {
        return CanRenderExpression(getOrAddGroup.Key) &&
               CanReferenceType(getOrAddGroup.KeyType) &&
               CanRenderAggregateGroupPlan(getOrAddGroup.GroupPlan);
    }

    private static bool CanRenderValueTupleAggregateContext(ExecutionCreateValueTupleAggregateContext context)
    {
        return context.KeyTypes.Count > 1 &&
               context.KeyTypes.All(CanReferenceType) &&
               CanRenderAggregateGroupPlan(context.GroupPlan);
    }

    private static bool CanRenderValueTupleAggregateLookup(ExecutionGetOrAddValueTupleAggregateGroup getOrAddGroup)
    {
        return getOrAddGroup.Keys.Count > 1 &&
               getOrAddGroup.Keys.Count == getOrAddGroup.KeyNames.Count &&
               getOrAddGroup.Keys.Count == getOrAddGroup.KeyTypes.Count &&
               getOrAddGroup.GroupDictionaries.Count > 0 &&
               getOrAddGroup.KeyTypes.All(CanReferenceType) &&
               CanRenderAggregateGroupPlan(getOrAddGroup.GroupPlan) &&
               CanRenderExpressions(getOrAddGroup.Keys);
    }

    private static bool CanRenderAggregateSet(ExecutionAggregateSet aggregateSet)
    {
        var isUnitKernel = aggregateSet.Accumulator.Kernel.InputShape.ArgumentTypes.Count == 0;
        return (isUnitKernel ||
                (aggregateSet.AccumulatorInput != null &&
                 CanRenderExpression(aggregateSet.AccumulatorInput))) &&
               CanReferenceType(aggregateSet.Accumulator.InputType) &&
               CanReferenceType(aggregateSet.Accumulator.ResultType) &&
               CanReferenceType(aggregateSet.Accumulator.AccumulatorType) &&
               CanReferenceType(aggregateSet.Accumulator.Kernel.KernelType);
    }

    private static bool CanRenderAggregateCapturedValueSet(ExecutionAggregateCapturedValueSet capturedValueSet)
    {
        return CanReferenceType(capturedValueSet.ValueType) &&
               CanRenderExpression(capturedValueSet.Value) &&
               CanReferenceType(capturedValueSet.CapturedField.Type);
    }

    private static bool CanRenderOrderKeys(IReadOnlyList<ExecutionOrderField> keys) => keys.Count > 0 && keys.All(key => CanReferenceType(key.Type));

    private static bool CanRenderProjectedTable(ExecutionProjectTable project)
    {
        return project.FieldIndexes.Count == project.RowShape.Fields.Count &&
               CanRenderFieldTypes(project.RowShape.Fields);
    }

    private static bool CanRenderOrderRecordList(ExecutionOrderRecordList orderRecords)
    {
        return CanRenderOrderKeys(orderRecords.Keys) &&
               CanRenderGeneratedRecordShape(orderRecords.RecordShape);
    }

    private static bool CanRenderRecordListMaterialization(ExecutionMaterializeRecordListToTable materialize)
    {
        return materialize.FieldIndexes.Count == materialize.RowShape.Fields.Count &&
               CanRenderGeneratedRecordShape(materialize.RecordShape) &&
               CanRenderFieldTypes(materialize.RowShape.Fields);
    }

    private static bool CanUseIndexedLoopItem(ExecutionVariable item, ExecutionRowAccessMode accessMode)
    {
        return accessMode switch
        {
            ExecutionRowAccessMode.Direct => item.Type.RequireClrType() != typeof(object),
            ExecutionRowAccessMode.ExpandoAdapter => true,
            _ => false
        };
    }
}
