using System.Globalization;
using System.Linq;
using System.Text;
namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static void AppendNode(StringBuilder builder, ExecutionNode node, int indentation)
    {
        var start = builder.Length;

        if (ExecutionNodeRegistry.TryGetDescriptor(node, out var descriptor))
        {
            descriptor.Behavior.Printer(builder, node, indentation);
            CaptureNodeDescription(builder, start, node);
            return;
        }

        AppendNodeLegacy(builder, node, indentation);
        CaptureNodeDescription(builder, start, node);
    }

    private static void CaptureNodeDescription(StringBuilder builder, int start, ExecutionNode node)
    {
        if (NodeDescriptions.Value is not { } descriptions || builder.Length <= start)
            return;

        var rendered = builder.ToString(start, builder.Length - start).Trim();
        var lineEnd = rendered.IndexOf('\n');
        if (lineEnd >= 0)
            rendered = rendered[..lineEnd].TrimEnd('\r');

        var nodeKindEnd = rendered.IndexOfAny([' ', '[']);
        var nodeKind = nodeKindEnd < 0 ? rendered : rendered[..nodeKindEnd];
        descriptions[node] = new ExecutionNodePrintDescription(rendered, nodeKind);
    }

    internal static void AppendNodeLegacy(StringBuilder builder, ExecutionNode node, int indentation) {
        var prefix = new string(' ', indentation);

        switch (node)
        {
            case ExecutionSourceScan sourceScan:
                var sourceDescription =
                    $"{prefix}SourceScan [{sourceScan.Source.Name}: {FormatType(sourceScan.Source.Type)}] -> {sourceScan.Rows.Name}" +
                    (sourceScan.Binding.QueryRowSourceTransfer is { } transfer
                        ? $" [query-row:{transfer.Carrier};lifetime={transfer.Lifetime};shape={transfer.ShapeFingerprint}]"
                        : string.Empty);
                builder.AppendLine(sourceDescription);
                break;
            case ExecutionInterpretSource interpret:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}InterpretSource [{interpret.SchemaName}.{interpret.Kind}({FormatExpressionList(interpret.Arguments)}) -> {interpret.Rows.Name}]");
                break;
            case ExecutionEnumerableSource enumerable:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}EnumerableSource [{FormatExpression(enumerable.Source)} -> {enumerable.Rows.Name}]");
                break;
            case ExecutionCreateTable createTable:
                if (IsFinalShapeSourceBuffer(createTable.Table.Name, out var sourceBufferShapeTypeName))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateShapeRows [{createTable.Table.Name}: {sourceBufferShapeTypeName} from {createTable.RowShape.TypeName}]");
                    break;
                }

                if (TryGetTypedRowBuffer(createTable.Table.Name, out var typedSetRowTypeName))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateRowBuffer [{createTable.Table.Name}: List<{typedSetRowTypeName}>]");
                    break;
                }

                if (IsFinalShapeTarget(createTable.Table.Name) &&
                    TryGetFinalShapeContext(out var createShapeContext))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateShapeRows [{createTable.Table.Name}: {createShapeContext.ShapeTypeName} from {createTable.RowShape.TypeName}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateTable [{createTable.Table.Name}: {createTable.RowShape.TypeName}]");
                break;
            case ExecutionCreateValuesRows valuesRows:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateValuesRows [{valuesRows.Rows.Name}: {valuesRows.RowShape.TypeName} x {valuesRows.Values.Count}]");
                break;
            case ExecutionCreateRecordList createList:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateRecordList [{createList.List.Name}: {createList.RecordShape.TypeName}]");
                break;
            case ExecutionCreateBoundedRecordList createList:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateBoundedRecordList [{createList.List.Name}: {createList.RecordShape.TypeName} by {FormatOrderFields(createList.Keys)}{FormatOrderRecordSelection(createList.Selection)}]");
                break;
            case ExecutionEnsureTableCapacity ensureCapacity:
                if (IsFinalShapeAppendTarget(ensureCapacity.Table.Name, out _))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}EnsureShapeCapacity [{ensureCapacity.Table.Name} <- {FormatCapacityHint(ensureCapacity.CapacityHint)}]");
                    break;
                }

                if (TryGetTypedRowBuffer(ensureCapacity.Table.Name, out _))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}EnsureRowBufferCapacity [{ensureCapacity.Table.Name} <- {FormatCapacityHint(ensureCapacity.CapacityHint)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}EnsureCapacity [{ensureCapacity.Table.Name} <- {FormatCapacityHint(ensureCapacity.CapacityHint)}]");
                break;
            case ExecutionForEach forEach:
                builder.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"{prefix}{FormatForEachName(forEach.Source)} [{forEach.Item.Name} in {FormatExpression(forEach.Source)}]");
                AppendBlock(builder, forEach.Body, indentation + 2);
                break;
            case ExecutionForEachWithOrdinality forEach:
                builder.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"{prefix}{FormatForEachWithOrdinalityName(forEach.Source)} [{forEach.Ordinal.Name}, {forEach.Item.Name} in {FormatExpression(forEach.Source)}]");
                AppendBlock(builder, forEach.Body, indentation + 2);
                break;
            case ExecutionScopedBlock scopedBlock: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ScopedBlock"); AppendBlock(builder, scopedBlock.Body, indentation + 2); break;
            case ExecutionForEachIndexed forEachIndexed:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ForEachIndexed [{forEachIndexed.Index.Name}, {forEachIndexed.Item.Name} in {forEachIndexed.Source.Name}]");
                AppendBlock(builder, forEachIndexed.Body, indentation + 2);
                break;
            case ExecutionParallelBlock parallel:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ParallelBlock [{parallel.Name}, tasks {parallel.Tasks.Count}, maxDegree {parallel.MaxDegreeOfParallelism}]");
                foreach (var task in parallel.Tasks)
                    AppendLabeledBlock(builder, prefix, $"  ParallelTask [{task.Name} -> {task.Output.Name}]", task.Body, indentation + 4);

                AppendLabeledBlock(builder, prefix, "  ParallelMerge", parallel.Merge.Body, indentation + 4);
                break;
            case ExecutionLet let:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}Let [{let.Variable.Name}: {FormatType(let.Variable.Type)} = {FormatExpression(let.Value)}]");
                break;
            case ExecutionHoistCandidateLet candidate: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}HoistCandidate [{candidate.Variable.Name}: {FormatType(candidate.Variable.Type)} = {FormatExpression(candidate.Value)}; {candidate.Kind}/{candidate.Scope}]"); break;
            case ExecutionAssign assign: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}Assign [{assign.Variable.Name} = {FormatExpression(assign.Value)}]"); break;
            case ExecutionCreateBooleanArray createArray: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateBooleanArray [{createArray.Array.Name} <- {createArray.LengthSource.Name}.Count]"); break;
            case ExecutionArrayAssign arrayAssign: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ArrayAssign [{arrayAssign.Array.Name}[{FormatExpression(arrayAssign.Index)}] = {FormatExpression(arrayAssign.Value)}]"); break;
            case ExecutionContinue:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}Continue");
                break;
            case ExecutionContinueIf continueIf:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ContinueIf [{FormatExpression(continueIf.Condition)}]");
                break;
            case ExecutionBreak:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}Break");
                break;
            case ExecutionAdaptExpando adapt:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AdaptExpando [{adapt.Target.Name}: {adapt.Shape.TypeName} <- {adapt.Source.Name}]");
                break;
            case ExecutionCreateObject createObject: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateObject [{createObject.Target.Name}: {FormatType(createObject.Target.Type)}]"); break;
            case ExecutionMethodTargetDeclarationCandidate candidate: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateObjectCandidate [{candidate.Target.Name}: {FormatType(candidate.Target.Type)}]"); break;
            case ExecutionIf branch:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}If [{FormatExpression(branch.Condition)}]");
                AppendBlock(builder, branch.Body, indentation + 2);
                break;
            case ExecutionCreateGeneratedRow createRow:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateGeneratedRow [{createRow.Row.Name} <- {createRow.RowShape.TypeName}({FormatRowValues(createRow.Values)})]");
                break;
            case ExecutionRecursiveCte recursiveCte:
                builder.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"{prefix}RecursiveCte [{recursiveCte.Name}; result {recursiveCte.Result.Name}; frontiers {recursiveCte.CurrentFrontier.Name}, {recursiveCte.NextFrontier.Name}; identity {FormatRecursiveIdentity(recursiveCte)}; max iterations {recursiveCte.MaxIterations}; max rows {recursiveCte.MaxRows}; max snapshot rows {recursiveCte.MaxSnapshotRows}]");
                AppendLabeledBlock(builder, prefix, "  Anchor", recursiveCte.Anchor, indentation + 4);
                AppendLabeledBlock(builder, prefix, "  InvariantSetup", recursiveCte.InvariantSetup, indentation + 4, skipWhenEmpty: true);
                AppendLabeledBlock(builder, prefix, "  RecursiveMember", recursiveCte.RecursiveMember, indentation + 4);
                break;
            case ExecutionRecursiveCteAppend append:
                builder.AppendLine(
                    CultureInfo.InvariantCulture,
                    $"{prefix}RecursiveAppend [{append.Frontier.Name} <- {append.AppendRow.RowShape.TypeName}({FormatRowValues(append.AppendRow.Values)}); identity {FormatRecursiveIdentity(append)}; guard {append.Result.Name}.Count + {append.Frontier.Name}.Count < {append.MaxRows}]");
                break;
            case ExecutionRecursiveCteSnapshotRowGuard guard:
                builder.AppendLine(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"{prefix}RecursiveSnapshotGuard [{guard.Counter.Name} < {guard.MaxRows}; {guard.Name}]");
                break;
            case ExecutionCreateHashPayload createPayload:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateHashPayload [{createPayload.Payload.Name} <- {createPayload.PayloadShape.TypeName}({FormatRowValues(createPayload.Values)})]");
                break;
            case ExecutionAppendRow appendRow:
                if (IsFinalShapeAppendTarget(appendRow.Table.Name, out var appendShapeTypeName))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AppendShape [{appendRow.Table.Name} <- {appendShapeTypeName}({FormatRowValues(appendRow.Values)})]");
                    break;
                }

                if (TryGetTypedRowBuffer(appendRow.Table.Name, out var typedAppendRowTypeName))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AppendRowBuffer [{appendRow.Table.Name} <- {typedAppendRowTypeName}({FormatRowValues(appendRow.Values)})]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AppendRow [{appendRow.Table.Name} <- {appendRow.RowShape.TypeName}({FormatRowValues(appendRow.Values)})]");
                break;
            case ExecutionAppendExistingRow appendRow:
                if (IsFinalShapeAppendTarget(appendRow.Table.Name, out _))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AppendExistingShape [{appendRow.Table.Name} <- {appendRow.Row.Name}]");
                    break;
                }

                if (TryGetTypedRowBuffer(appendRow.Table.Name, out _))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AppendExistingRowBuffer [{appendRow.Table.Name} <- {appendRow.Row.Name}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AppendExistingRow [{appendRow.Table.Name} <- {appendRow.Row.Name}]");
                break;
            case ExecutionAppendRecord appendRecord:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AppendRecord [{appendRecord.List.Name} <- {appendRecord.RecordShape.TypeName}({FormatRowValues(appendRecord.Values)})]");
                break;
            case ExecutionMaterializeList materialize:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{FormatMaterializeName(materialize.Source)} [{FormatExpression(materialize.Source)} -> {materialize.Buffer.Name}]");
                break;
            case ExecutionMaterializeFilteredList materialize:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{FormatMaterializeFilteredName(materialize.Source)} [{FormatExpression(materialize.Source)} where {FormatExpression(materialize.Predicate)} -> {materialize.Buffer.Name}]");
                break;
            case ExecutionMaterializeExpandoList materialize:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{FormatMaterializeExpandoName(materialize.Source)} [{FormatExpression(materialize.Source)}{FormatOptionalPredicate(materialize.Predicate)} as {materialize.Shape.TypeName} -> {materialize.Buffer.Name}]");
                break;
            case ExecutionWindowKernelPlan plan:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}WindowKernelPlan [{FormatWindowKernelPlanStrategy(plan.Strategy)}; kernels {plan.Kernels.Count.ToString(CultureInfo.InvariantCulture)}; {plan.Signature}]");
                foreach (var kernel in plan.Kernels)
                    AppendNode(builder, kernel, indentation + 2);
                break;
            case ExecutionComputeRankingWindow ranking:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{FormatRankingNodeName(ranking.Function)} [{FormatWindowComputationTarget(ranking)}{FormatPartition(ranking.PartitionKey)} order by {FormatWindowOrderKeys(ranking.OrderKeys)}{FormatRankingQualifyUpperBound(ranking.QualifyUpperBound)}]");
                break;
            case ExecutionComputeOffsetWindow offset:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{FormatOffsetNodeName(offset.Function)} [{FormatWindowComputationTarget(offset)} value {FormatExpression(offset.Value)}{FormatPartition(offset.PartitionKey)} order by {FormatWindowOrderKeys(offset.OrderKeys)} offset {FormatExpression(offset.Offset)} default {FormatExpression(offset.DefaultValue)}]");
                break;
            case ExecutionComputePluginWindow plugin:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{FormatPluginNodeName(plugin.FunctionName)} [{FormatWindowComputationTarget(plugin)} value {FormatExpression(plugin.Value)}{FormatPartition(plugin.PartitionKey)}{FormatOptionalWindowOrderKeys(plugin.OrderKeys)}{FormatWindowFrame(plugin.Frame)}{FormatPluginArguments(plugin.Arguments)}]");
                break;
            case ExecutionWindowAggregateKernel kernel:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{FormatWindowAggregateKernelNodeName(kernel.Descriptor)} [{FormatWindowComputationTarget(kernel)} value {FormatExpression(kernel.Value)}{FormatPartition(kernel.PartitionKey)}{FormatOptionalWindowOrderKeys(kernel.OrderKeys)}{FormatWindowFrame(kernel.Frame)}]");
                break;
            case ExecutionCreateHash createHash:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateHash [{createHash.Hash.Name}: {FormatType(createHash.KeyType)} -> {FormatType(createHash.RowType)}{FormatOptionalCapacity(createHash.CapacityHint)}]");
                break;
            case ExecutionHashAdd hashAdd:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}HashAdd [{hashAdd.Hash.Name}[{FormatExpression(hashAdd.Key)}] += {hashAdd.Row.Name}]");
                break;
            case ExecutionHashProbe hashProbe:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}HashProbe [{hashProbe.Hash.Name}[{FormatExpression(hashProbe.Key)}] -> {hashProbe.Matches.Name}]{FormatMatchTracking(hashProbe.MatchFound)}");
                AppendBlock(builder, hashProbe.Body, indentation + 2);
                AppendLabeledBlock(builder, prefix, "HashProbeNoMatch", hashProbe.NoMatchBody, indentation + 2, skipWhenEmpty: true);
                break;
            case ExecutionCreateKeySet createSet:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateKeySet [{createSet.Set.Name}: {FormatType(createSet.KeyType)}{FormatOptionalCapacity(createSet.CapacityHint)}]");
                break;
            case ExecutionKeySetAdd keySetAdd:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}KeySetAdd [{keySetAdd.Set.Name} += {FormatExpression(keySetAdd.Key)}]");
                break;
            case ExecutionKeySetProbe keySetProbe:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}KeySetProbe [{keySetProbe.Set.Name}[{FormatExpression(keySetProbe.Key)}]]{FormatMatchTracking(keySetProbe.MatchFound)}");
                AppendBlock(builder, keySetProbe.Body, indentation + 2);
                AppendLabeledBlock(builder, prefix, "KeySetProbeNoMatch", keySetProbe.NoMatchBody, indentation + 2, skipWhenEmpty: true);
                break;
            case ExecutionStoreCteIndex storeCteIndex: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}StoreCteIndex [{storeCteIndex.Index.Name} -> _cteIndexResults.Slot{storeCteIndex.IndexSlot} {storeCteIndex.Kind}]"); break;
            case ExecutionLoadCteIndex loadCteIndex: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}LoadCteIndex [{loadCteIndex.Index.Name} <- _cteIndexResults.Slot{loadCteIndex.IndexSlot} {loadCteIndex.Kind}: {FormatType(loadCteIndex.KeyType)}]"); break;
            case ExecutionCteSidecarIndexStoreCandidate candidate: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}StoreCteIndexCandidate [{candidate.Index.Name} -> _cteIndexResults.Slot{candidate.IndexSlot} {candidate.Kind}]"); break;
            case ExecutionCteSidecarIndexLoadCandidate candidate: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}LoadCteIndexCandidate [{candidate.Index.Name} <- _cteIndexResults.Slot{candidate.IndexSlot} {candidate.Kind}: {FormatType(candidate.KeyType)}]"); break;
            case ExecutionCteSidecarIndexBuildCandidate or ExecutionCteSidecarAppendRewriteCandidate or ExecutionCteIndexOnlyStorageCandidate: AppendCteStrategyCandidateNode(builder, node, prefix); break;
            case ExecutionCreateAsOfIndex createIndex:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateAsOfIndex [{createIndex.Index.Name} <- {FormatExpression(createIndex.Candidates)} by {FormatAsOfIndexKey(createIndex)}]");
                break;
            case ExecutionAsOfProbe asOfProbe:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AsOfProbe [{asOfProbe.Match.Name} <- {FormatExpression(asOfProbe.Candidates)}{FormatAsOfIndex(asOfProbe)} where {FormatAsOfPredicate(asOfProbe)}]");
                AppendBlock(builder, asOfProbe.Body, indentation + 2);
                AppendLabeledBlock(builder, prefix, "AsOfProbeNoMatch", asOfProbe.NoMatchBody, indentation + 2, skipWhenEmpty: true);
                break;
            case ExecutionCreateRangeIndex createIndex:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateRangeIndex [{createIndex.Index.Name} <- {FormatExpression(createIndex.Candidates)} by {FormatRangeIndexKey(createIndex)} {FormatBinaryOperator(createIndex.ComparisonKind)}]");
                break;
            case ExecutionRangeProbe rangeProbe:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}RangeProbe [{rangeProbe.Match.Name} <- {rangeProbe.Index.Name} where {FormatRangeProbeKey(rangeProbe)}]{FormatMatchTracking(rangeProbe.MatchFound)}");
                AppendBlock(builder, rangeProbe.Body, indentation + 2);
                AppendLabeledBlock(builder, prefix, "RangeProbeNoMatch", rangeProbe.NoMatchBody, indentation + 2, skipWhenEmpty: true);
                break;
            case ExecutionCreateAggregateLibrary library: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateAggregateLibrary [{library.Library.Name}: {FormatType(library.LibraryType)}]"); break;
            case ExecutionCreateAggregateContext context:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateAggregateContext [{context.RootGroup.Name}, {context.CurrentGroup.Name}, {context.Groups.Name}{FormatAggregateGroupShape(context.GroupShape)}]");
                break;
            case ExecutionEnsureAggregateGroup ensureGroup:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}EnsureAggregateGroup [{ensureGroup.CurrentGroup.Name}{FormatAggregateGroupShape(ensureGroup.GroupShape)}]");
                break;
            case ExecutionCreateSingleKeyAggregateContext context:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateSingleKeyAggregateContext [{context.Groups.Name}: {FormatType(context.KeyType)} -> {FormatAggregateGroupType(context.GroupShape)}]");
                break;
            case ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}GetOrAddSingleKeyAggregateGroup [{getOrAddGroup.Group.Name} = {getOrAddGroup.Groups.Name}[{FormatExpression(getOrAddGroup.Key)}] by {getOrAddGroup.KeyName}{FormatAggregateGroupShape(getOrAddGroup.GroupShape)}]");
                break;
            case ExecutionParallelSingleKeyAggregateLoop parallelAggregate:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ParallelSingleKeyAggregateLoop [{parallelAggregate.Source.Name} in {FormatExpression(parallelAggregate.SourceRows)} by {FormatExpression(parallelAggregate.Key)}; threshold {parallelAggregate.Threshold.ToString(CultureInfo.InvariantCulture)}, sample {parallelAggregate.CardinalitySampleSize.ToString(CultureInfo.InvariantCulture)}/{parallelAggregate.MaxDistinctSample.ToString(CultureInfo.InvariantCulture)}, maxDegree {parallelAggregate.MaxDegreeOfParallelism.ToString(CultureInfo.InvariantCulture)}, group {parallelAggregate.GroupShape.TypeName}]");
                AppendLabeledBlock(builder, prefix, "  ParallelAccumulate", parallelAggregate.AggregateBody, indentation + 4);
                break;
            case ExecutionParallelFilterProjectLoop parallelProject:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ParallelFilterProjectLoop [{parallelProject.Source.Name} in {FormatExpression(parallelProject.SourceRows)}{FormatOptionalPredicate(parallelProject.Predicate)}; threshold {parallelProject.Threshold.ToString(CultureInfo.InvariantCulture)}, maxDegree {parallelProject.MaxDegreeOfParallelism.ToString(CultureInfo.InvariantCulture)}]");
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}  ParallelProject");
                AppendBlock(builder, parallelProject.ProjectionBody, indentation + 4);
                break;
            case ExecutionFusedCteProducer or ExecutionCteFusedProducerCandidate: AppendCteProducerNode(builder, node, indentation, prefix); break;
            case ExecutionSingleUsePipelineFusionCandidate candidate: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SingleUseFusionCandidate [cte{candidate.RelatedTableIndex.ToString(CultureInfo.InvariantCulture)}]"); AppendBlock(builder, candidate.Body, indentation + 2); break;
            case ExecutionCteReadOnceFusionCandidate candidate: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CteReadOnceFusionCandidate [cte{candidate.RelatedTableIndex.ToString(CultureInfo.InvariantCulture)}]"); AppendBlock(builder, candidate.Body, indentation + 2); break;
            case ExecutionCreateValueTupleAggregateContext context:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CreateValueTupleAggregateContext [{context.GroupDictionaries[^1].Variable.Name}: {FormatTupleType(context.KeyTypes)} -> {FormatAggregateGroupType(context.GroupShape)}]");
                break;
            case ExecutionGetOrAddValueTupleAggregateGroup getOrAddGroup:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}GetOrAddValueTupleAggregateGroup [{getOrAddGroup.Group.Name} = {getOrAddGroup.GroupDictionaries[^1].Variable.Name}[{FormatTupleExpression(getOrAddGroup.Keys)}] by {string.Join(", ", getOrAddGroup.KeyNames)}{FormatAggregateGroupShape(getOrAddGroup.GroupShape)}]");
                break;
            case ExecutionAggregateSet aggregateSet:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{FormatTypedAggregateSet(aggregateSet)}");
                break;
            case ExecutionAggregateCapturedValueSet capturedValueSet:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}AggregateCapturedValueSet [{capturedValueSet.Group.Name}.{capturedValueSet.CapturedField.FieldName} = {FormatExpression(capturedValueSet.Value)}]");
                break;
            case ExecutionSetOperation setOperation:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SetOperation [{setOperation.Target.Name} = {setOperation.Left.Name} {setOperation.Kind} {setOperation.Right.Name}{FormatSetOperationStrategy(setOperation.Strategy)}]");
                break;
            case ExecutionDistinctTable distinct:
                if (IsTypedRowBufferPostOperation(distinct.Target, distinct.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}DistinctRowBuffer [{FormatTablePostOperationFlow(distinct)}]");
                    break;
                }

                if (IsFinalShapePostOperation(distinct.Target, distinct.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}DistinctShapeRows [{FormatTablePostOperationFlow(distinct)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}DistinctTable [{FormatTablePostOperationFlow(distinct)}]");
                break;
            case ExecutionSortTable sort:
                if (IsTypedRowBufferPostOperation(sort.Target, sort.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SortRowBuffer [{FormatTablePostOperationFlow(sort)} by {FormatOrderFields(sort.Keys)}{FormatOptionalCandidateCapacity(sort.CapacityHint)}]");
                    break;
                }

                if (IsFinalShapePostOperation(sort.Target, sort.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SortShapeRows [{FormatTablePostOperationFlow(sort)} by {FormatOrderFields(sort.Keys)}{FormatOptionalCandidateCapacity(sort.CapacityHint)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SortTable [{FormatTablePostOperationFlow(sort)} by {FormatOrderFields(sort.Keys)}{FormatOptionalCandidateCapacity(sort.CapacityHint)}]");
                break;
            case ExecutionTopNTable topN:
                if (IsTypedRowBufferPostOperation(topN.Target, topN.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TopNRowBuffer [{FormatTablePostOperationFlow(topN)} by {FormatOrderFields(topN.Keys)}, {topN.Count}{FormatOptionalCandidateCapacity(topN.CapacityHint)}]");
                    break;
                }

                if (IsFinalShapePostOperation(topN.Target, topN.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TopNShapeRows [{FormatTablePostOperationFlow(topN)} by {FormatOrderFields(topN.Keys)}, {topN.Count}{FormatOptionalCandidateCapacity(topN.CapacityHint)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TopNTable [{FormatTablePostOperationFlow(topN)} by {FormatOrderFields(topN.Keys)}, {topN.Count}{FormatOptionalCandidateCapacity(topN.CapacityHint)}]");
                break;
            case ExecutionTopOffsetTable topOffset:
                if (IsTypedRowBufferPostOperation(topOffset.Target, topOffset.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TopOffsetRowBuffer [{FormatTablePostOperationFlow(topOffset)} by {FormatOrderFields(topOffset.Keys)}, skip {topOffset.SkipCount}, take {topOffset.TakeCount}, {topOffset.Strategy}{FormatOptionalCandidateCapacity(topOffset.CapacityHint)}]");
                    break;
                }

                if (IsFinalShapePostOperation(topOffset.Target, topOffset.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TopOffsetShapeRows [{FormatTablePostOperationFlow(topOffset)} by {FormatOrderFields(topOffset.Keys)}, skip {topOffset.SkipCount}, take {topOffset.TakeCount}, {topOffset.Strategy}{FormatOptionalCandidateCapacity(topOffset.CapacityHint)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TopOffsetTable [{FormatTablePostOperationFlow(topOffset)} by {FormatOrderFields(topOffset.Keys)}, skip {topOffset.SkipCount}, take {topOffset.TakeCount}, {topOffset.Strategy}{FormatOptionalCandidateCapacity(topOffset.CapacityHint)}]");
                break;
            case ExecutionSkipTable skip:
                if (IsTypedRowBufferPostOperation(skip.Target, skip.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SkipRowBuffer [{FormatTablePostOperationFlow(skip)}, {skip.Count}{FormatOptionalCandidateCapacity(skip.CapacityHint)}]");
                    break;
                }

                if (IsFinalShapePostOperation(skip.Target, skip.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SkipShapeRows [{FormatTablePostOperationFlow(skip)}, {skip.Count}{FormatOptionalCandidateCapacity(skip.CapacityHint)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SkipTable [{FormatTablePostOperationFlow(skip)}, {skip.Count}{FormatOptionalCandidateCapacity(skip.CapacityHint)}]");
                break;
            case ExecutionTakeTable take:
                if (IsTypedRowBufferPostOperation(take.Target, take.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TakeRowBuffer [{FormatTablePostOperationFlow(take)}, {take.Count}{FormatOptionalCandidateCapacity(take.CapacityHint)}]");
                    break;
                }

                if (IsFinalShapePostOperation(take.Target, take.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TakeShapeRows [{FormatTablePostOperationFlow(take)}, {take.Count}{FormatOptionalCandidateCapacity(take.CapacityHint)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}TakeTable [{FormatTablePostOperationFlow(take)}, {take.Count}{FormatOptionalCandidateCapacity(take.CapacityHint)}]");
                break;
            case ExecutionSliceTable slice:
                if (IsTypedRowBufferPostOperation(slice.Target, slice.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SliceRowBuffer [{FormatTablePostOperationFlow(slice)}, skip {slice.SkipCount}, take {slice.TakeCount}{FormatOptionalCandidateCapacity(slice.CapacityHint)}]");
                    break;
                }

                if (IsFinalShapePostOperation(slice.Target, slice.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SliceShapeRows [{FormatTablePostOperationFlow(slice)}, skip {slice.SkipCount}, take {slice.TakeCount}{FormatOptionalCandidateCapacity(slice.CapacityHint)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}SliceTable [{FormatTablePostOperationFlow(slice)}, skip {slice.SkipCount}, take {slice.TakeCount}{FormatOptionalCandidateCapacity(slice.CapacityHint)}]");
                break;
            case ExecutionProjectTable project:
                if (IsTypedRowBufferPostOperation(project.Target, project.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ProjectRowBuffer [{FormatTablePostOperationFlow(project)} fields {FormatFieldIndexes(project.FieldIndexes)}{FormatOptionalCandidateCapacity(project.CapacityHint)}]");
                    break;
                }

                if (IsFinalShapePostOperation(project.Target, project.Source))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ProjectShapeRows [{FormatTablePostOperationFlow(project)} fields {FormatFieldIndexes(project.FieldIndexes)}{FormatOptionalCandidateCapacity(project.CapacityHint)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ProjectTable [{FormatTablePostOperationFlow(project)} fields {FormatFieldIndexes(project.FieldIndexes)}{FormatOptionalCandidateCapacity(project.CapacityHint)}]");
                break;
            case ExecutionOrderRecordList orderRecords:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}OrderRecordList [{orderRecords.Source.Name}: {orderRecords.RecordShape.TypeName} by {FormatOrderFields(orderRecords.Keys)}{FormatOrderRecordSelection(orderRecords.Selection)}]");
                break;
            case ExecutionMaterializeRecordListToTable materialize:
                if (IsFinalShapeTarget(materialize.Target.Name) || IsFinalShapeSourceBuffer(materialize.Target.Name, out _))
                {
                    var materializeShapeTypeName = TryGetFinalShapeContext(out var materializeContext)
                        ? materializeContext.ShapeTypeName
                        : materialize.RowShape.TypeName;
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}MaterializeRecordListToShapeRows [{FormatTablePostOperationFlow(materialize)}: {materializeShapeTypeName} fields {FormatFieldIndexes(materialize.FieldIndexes)}{FormatOptionalCandidateCapacity(materialize.CapacityHint)}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}MaterializeRecordListToTable [{FormatTablePostOperationFlow(materialize)}: {materialize.RowShape.TypeName} fields {FormatFieldIndexes(materialize.FieldIndexes)}{FormatOptionalCandidateCapacity(materialize.CapacityHint)}]");
                break;
            case ExecutionStoreTable store:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}StoreTable [{store.Table.Name} -> {FormatStoredTableTarget(store)}]");
                break;
            case ExecutionPhaseBoundary boundary:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}PhaseBoundary [{boundary.Phase}{boundary.QueryIdSuffix}]");
                break;
            case ExecutionRelatedCtePhase phase:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}CtePhase [cte{phase.TableIndex.ToString(CultureInfo.InvariantCulture)}]");
                break;
            case ExecutionReturnDesc desc: builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ReturnDesc [{FormatDesc(desc)}]"); break;
            case ExecutionReturnTable returnTable:
                if (TryGetFinalShapeContext(out var finalShapeContext) &&
                    string.Equals(returnTable.Table.Name, finalShapeContext.FinalTableName, StringComparison.Ordinal))
                {
                    builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ReturnDeferredTable [{finalShapeContext.FinalTableName}: {finalShapeContext.RowTypeName} <- {finalShapeContext.ShapeTypeName}]");
                    break;
                }

                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}ReturnTable [{returnTable.Table.Name}]");
                break;
            default:
                builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}UnknownNode [{node.GetType().Name}]");
                break;
        }
    }

    private static void AppendLabeledBlock(
        StringBuilder builder,
        string prefix,
        string label,
        ExecutionBlock? block,
        int indentation,
        bool skipWhenEmpty = false)
    {
        if (block == null || skipWhenEmpty && block.Nodes.Count == 0)
            return;

        builder.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{label}");
        AppendBlock(builder, block, indentation);
    }

    private static string FormatStoredTableTarget(ExecutionStoreTable store)
    {
        return TryGetTypedStoredTableSlot(store.TableIndex, out var generatedRowTypeName)
            ? $"{FormatCteRowResultSlot(store.TableIndex)}: {generatedRowTypeName}"
            : $"_tableResults[{store.TableIndex.ToString(CultureInfo.InvariantCulture)}]";
    }

    private static string FormatRecursiveIdentity(ExecutionRecursiveCte recursiveCte)
    {
        return recursiveCte.Seen == null
            ? "none"
            : $"{recursiveCte.IdentityMode} via {recursiveCte.Seen.Name} ({FormatRecursiveIdentityFields(recursiveCte.RowShape, recursiveCte.IdentityFieldIndexes)})";
    }

    private static string FormatRecursiveIdentity(ExecutionRecursiveCteAppend append)
    {
        return append.Seen == null
            ? "none"
            : $"{append.Seen.Name} ({FormatRecursiveIdentityFields(append.AppendRow.RowShape, append.IdentityFieldIndexes)})";
    }

    private static string FormatRecursiveIdentityFields(GeneratedRowShape shape, int[] fieldIndexes)
    {
        return string.Join(", ", fieldIndexes.Select(index => shape.Fields[index].Name));
    }
}
