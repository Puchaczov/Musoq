using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    public IReadOnlyList<MemberDeclarationSyntax> RenderClassMembers(
        ExecutionPlan plan,
        string? finalShapeTableName = null,
        string? finalShapeTypeName = null,
        IReadOnlyList<FieldBinding>? finalShapeFields = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        EnsureConstantInSetFields(plan);
        EnsureStaticMetadataFields(plan);
        EnsureAggregateGenerationState(plan);

        var previousIncludeCteIndexResults = _includeCteIndexResults;
        var previousIncludeCteRowResults = _includeCteRowResults;
        var previousIncludeTableResults = _includeTableResults;
        var previousTypedStoredTableResults = _typedStoredTableResults;
        var previousGeneratedRowConstructorUsagesByType = _generatedRowConstructorUsagesByType;
        var previousGeneratedRowTypesUsedAsRowContexts = _generatedRowTypesUsedAsRowContexts;
        var previousGeneratedRowTypesUsedAtPublicBoundary = _generatedRowTypesUsedAtPublicBoundary;
        var previousGeneratedRowTypesRequiringRowBase = _generatedRowTypesRequiringRowBase;
        var previousOperatorCatalog = _operatorCatalog;
        var previousSingleKeyAggregateUpdateHelpersByBlock = _singleKeyAggregateUpdateHelpersByBlock;
        var previousEnumerableTraversalHelpersByBlock = _enumerableTraversalHelpersByBlock;
        var previousFinalShapeYieldSink = _finalShapeYieldSink;
        var previousTypedRowBufferVariables = _typedRowBufferVariables;
        if (finalShapeTableName != null)
        {
            var sourceBuffers = finalShapeTypeName != null && finalShapeFields != null
                ? CreateFinalShapeSourceBuffers(plan.Body, finalShapeTableName, finalShapeTypeName, finalShapeFields)
                : null;
            _finalShapeYieldSink = new FinalShapeYieldSink(
                finalShapeTableName,
                finalShapeTypeName ?? string.Empty,
                finalShapeFields ?? [],
                null,
                sourceBuffers);
        }

        _typedStoredTableResults = CreateTypedStoredTableResults(plan);
        _includeCteIndexResults = PlanUsesCteIndexResults(plan);
        _includeCteRowResults = _typedStoredTableResults.Count > 0;
        _includeTableResults = PlanUsesTableResults(plan, _typedStoredTableResults);
        _operatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
        _typedRowBufferVariables = CreateTypedRowBufferVariables(plan.Body, finalShapeTableName);
        _singleKeyAggregateUpdateHelpersByBlock = CollectSingleKeyAggregateUpdateHelpersByBlock(plan.Body);
        _enumerableTraversalHelpersByBlock = finalShapeTableName == null
            ? CollectEnumerableTraversalHelpersByBlock(plan.Body)
            : CollectEnumerableTraversalHelpersByBlock(plan.Body)
                .Where(pair => !CapturesCurrentFinalShapeTargetOrSourceBuffer(pair.Value))
                .ToDictionary(static pair => pair.Key, static pair => pair.Value);

        try
        {
            var members = new List<MemberDeclarationSyntax>();

            var constructorUsages = CollectGeneratedRowConstructorUsages(plan.Body);
            _generatedRowConstructorUsagesByType = constructorUsages;
            _generatedRowTypesUsedAsRowContexts = CollectGeneratedRowTypesUsedAsRowContexts(plan.Body);
            _generatedRowTypesUsedAtPublicBoundary = CollectGeneratedRowTypesUsedAtPublicBoundary(plan.Body);
            _generatedRowTypesRequiringRowBase = GeneratedRowCarrierUsage.CollectTypesRequiringRowBase(plan.Body, constructorUsages);
            var renderedGeneratedRows = new HashSet<string>(StringComparer.Ordinal);
            var renderedHashPayloads = new HashSet<string>(StringComparer.Ordinal);
            var renderedAggregateGroups = new HashSet<string>(StringComparer.Ordinal);
            var renderedOrderComparers = new HashSet<string>(StringComparer.Ordinal);
            var renderedGeneratedRowOrderComparers = new HashSet<string>(StringComparer.Ordinal);
            var tableRowShapesByVariableName = CreateTableRowShapeMap(plan.Body);

            foreach (var shape in plan.Shapes)
            {
                switch (shape)
                {
                    case GeneratedRowShape generated:
                        if (renderedGeneratedRows.Add(generated.TypeName))
                        {
                            constructorUsages.TryGetValue(generated.TypeName, out var usedConstructors);
                            members.Add(RenderGeneratedRowClass(generated, usedConstructors));
                        }
                        break;
                    case ValuesRowShape values:
                        if (renderedGeneratedRows.Add(values.GeneratedShape.TypeName))
                        {
                            constructorUsages.TryGetValue(values.GeneratedShape.TypeName, out var usedConstructors);
                            members.Add(RenderGeneratedRowClass(values.GeneratedShape, usedConstructors));
                        }
                        break;
                    case GeneratedRecordShape generated:
                        members.Add(RenderGeneratedRecordClass(generated));
                        break;
                    case HashPayloadShape hashPayload:
                        if (renderedHashPayloads.Add(hashPayload.TypeName))
                            members.Add(RenderHashPayloadStruct(hashPayload));
                        break;
                    case AggregateGroupShape aggregateGroup:
                        if (renderedAggregateGroups.Add(GetAggregateGroupTypeName(aggregateGroup)))
                            members.Add(RenderAggregateGroupClass(aggregateGroup));
                        break;
                    case ExpandoAdapterShape expando:
                        members.Add(RenderExpandoAdapterClass(expando));
                        break;
                }
            }

            if (plan.FinalResult != null)
                members.Add(RenderFinalSelectShapeClass(plan.FinalResult));

            members.AddRange(CreateCteIndexResultMembers(plan));
            members.AddRange(CreateCteRowResultMembers(_typedStoredTableResults));
            members.AddRange(_constantInSetFields.Select(static field => CreateConstantInSetField(field)));
            members.AddRange(_staticMetadataFields.Select(static field => CreateStaticMetadataField(field)));
            AddCollectionParameterMembers(plan, members);

            members.AddRange(_singleKeyAggregateUpdateHelpersByBlock.Values
                .Select(CreateSingleKeyAggregateUpdateFunction));
            members.AddRange(_enumerableTraversalHelpersByBlock.Values
                .Select(CreateEnumerableTraversalFunction));
            members.AddRange(CollectStoredTableBuilds(plan.Body).Select(CreateStoredTableBuildFunction));
            members.AddRange(CollectHashJoinHelperSets(plan.Body)
                .Where(CanUseHashJoinHelperSetInCurrentSink)
                .SelectMany(CreateHashJoinHelperFunctions));
            members.AddRange(CollectKeySetHelperSets(plan.Body)
                .Where(CanUseKeySetHelperSetInCurrentSink)
                .SelectMany(CreateKeySetHelperFunctions));
            members.AddRange(CollectGeneratedWindowKeyStructs(plan.Body)
                .GroupBy(static key => key.TypeName)
                .Select(static group => group.First())
                .Select(CreateGeneratedWindowKeyStruct));
            members.AddRange(CollectRankingWindowKeyExtractionHelpers(plan.Body)
                .Select(CreateRankingWindowKeyExtractionFunction));
            members.AddRange(CollectWindowAppendRowsHelpers(plan.Body)
                .Where(CanUseWindowAppendRowsHelperInCurrentSink)
                .Select(CreateWindowAppendRowsFunction));
            var previousTableRowShapesByVariableName = _tableRowShapesByVariableName;
            _tableRowShapesByVariableName = tableRowShapesByVariableName;
            try
            {
                members.AddRange(CollectSortedCopyHelpers(plan.Body)
                    .Where(CanUseSortedCopyHelperInCurrentSink)
                    .Select(CreateSortedCopyFunction));
            }
            finally
            {
                _tableRowShapesByVariableName = previousTableRowShapesByVariableName;
            }

            members.AddRange(CollectValueTupleAggregateHelpers(plan.Body)
                .Where(helper => CanUseAggregateFinalizeHelperInCurrentSink(helper.EnsureCapacity.Table.Name))
                .SelectMany(CreateValueTupleAggregateFunctions));
            members.AddRange(CollectSingleKeyHashAggregateHelpers(plan.Body)
                .Where(helper => CanUseAggregateFinalizeHelperInCurrentSink(helper.EnsureCapacity.Table.Name))
                .SelectMany(CreateSingleKeyAggregateFunctions));
            members.AddRange(CollectParallelBlocks(plan.Body)
                .SelectMany(CreateParallelBlockMembers));
            var parallelFilterProjectLoops = CollectParallelFilterProjectLoops(plan.Body).ToArray();
            members.AddRange(parallelFilterProjectLoops
                .Where(CanUseParallelFilterProjectHelperInCurrentSink)
                .GroupBy(CreateParallelFilterProjectFunctionName)
                .Select(static group => group.First())
                .Select(CreateParallelFilterProjectFunction));
            var parallelAggregateLoops = CollectParallelSingleKeyAggregateLoops(plan.Body).ToArray();
            var uniqueParallelAggregateLoops = parallelAggregateLoops
                .GroupBy(CreateParallelSingleKeyAggregateFunctionName)
                .Select(static group => group.First())
                .ToArray();
            members.AddRange(uniqueParallelAggregateLoops.Select(CreateParallelSingleKeyAggregateFunction));
            members.AddRange(uniqueParallelAggregateLoops.Select(CreateParallelSingleKeyAggregateShardFunction));
            members.AddRange(uniqueParallelAggregateLoops.Select(CreateParallelSingleKeyAggregateWorkerClass));
            members.AddRange(FlattenNodes(plan.Body)
                .OfType<ExecutionOrderRecordList>()
                .Where(order => renderedOrderComparers.Add(CreateOrderRecordComparerTypeName(order.RecordShape)))
                .Select(CreateOrderRecordComparerClass));
            members.AddRange(FlattenNodes(plan.Body)
                .OfType<ExecutionCreateBoundedRecordList>()
                .Where(order => renderedOrderComparers.Add(CreateOrderRecordComparerTypeName(order.RecordShape)))
                .Select(CreateOrderRecordComparerClass));
            foreach (var input in CollectGeneratedRowOrderComparerInputs(plan.Body, tableRowShapesByVariableName))
            {
                if (IsCurrentFinalShapeSourceBuffer(input.SourceName) ||
                    IsCurrentFinalShapeSourceBuffer(input.TargetName))
                {
                    continue;
                }

                var comparerTypeName = CreateGeneratedRowOrderComparerTypeName(input.RowShape, input.Keys);
                if (renderedGeneratedRowOrderComparers.Add(comparerTypeName))
                    members.Add(CreateGeneratedRowOrderComparerClass(input.RowShape, input.Keys));
            }

            return CodegenHelperExtractionMetadata.AnnotateCandidateMembers(members);
        }
        finally
        {
            _includeCteIndexResults = previousIncludeCteIndexResults;
            _includeCteRowResults = previousIncludeCteRowResults;
            _includeTableResults = previousIncludeTableResults;
            _typedStoredTableResults = previousTypedStoredTableResults;
            _generatedRowConstructorUsagesByType = previousGeneratedRowConstructorUsagesByType;
            _generatedRowTypesUsedAsRowContexts = previousGeneratedRowTypesUsedAsRowContexts;
            _generatedRowTypesUsedAtPublicBoundary = previousGeneratedRowTypesUsedAtPublicBoundary;
            _generatedRowTypesRequiringRowBase = previousGeneratedRowTypesRequiringRowBase;
            _operatorCatalog = previousOperatorCatalog;
            _singleKeyAggregateUpdateHelpersByBlock = previousSingleKeyAggregateUpdateHelpersByBlock;
            _enumerableTraversalHelpersByBlock = previousEnumerableTraversalHelpersByBlock;
            _finalShapeYieldSink = previousFinalShapeYieldSink;
            _typedRowBufferVariables = previousTypedRowBufferVariables;
        }
    }

    private static bool PlanUsesCteIndexResults(ExecutionPlan plan)
    {
        return FlattenNodes(plan.Body).Any(static node => node is ExecutionStoreCteIndex or ExecutionLoadCteIndex);
    }

    private static bool PlanUsesTableResults(
        ExecutionPlan plan,
        IReadOnlyDictionary<int, TypedStoredTableResult> typedResults)
    {
        if (ExecutionIrAnalysis.CollectExpressions<ExecutionStoredTable>(plan.Body).Any())
            return true;

        if (ExecutionIrAnalysis
            .CollectExpressions<ExecutionStoredTableRows>(plan.Body)
            .Any(storedRows => !typedResults.ContainsKey(storedRows.TableIndex)))
        {
            return true;
        }

        foreach (var node in FlattenNodes(plan.Body))
        {
            switch (node)
            {
                case ExecutionStoreTable store when !typedResults.ContainsKey(store.TableIndex):
                    return true;
                case ExecutionCreateHash { CapacityHint: ExecutionStoredTableCountCapacityHint hint } when
                    !typedResults.ContainsKey(hint.TableIndex):
                    return true;
                case ExecutionCreateKeySet { CapacityHint: ExecutionStoredTableCountCapacityHint hint } when
                    !typedResults.ContainsKey(hint.TableIndex):
                    return true;
                case ExecutionCreateTable { CapacityHint: ExecutionStoredTableCountCapacityHint hint } when
                    !typedResults.ContainsKey(hint.TableIndex):
                    return true;
                case ExecutionEnsureTableCapacity { CapacityHint: ExecutionStoredTableCountCapacityHint hint } when
                    !typedResults.ContainsKey(hint.TableIndex):
                    return true;
            }
        }

        return false;
    }
}
