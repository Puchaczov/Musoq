using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal static bool CanUseGeneratedFinalRowSink(ExecutionPlan plan, string finalTableName)
    {
        return FinalGeneratedRowSinkPolicy.CanUse(plan, finalTableName);
    }

    public IReadOnlyList<MemberDeclarationSyntax> RenderClassMembers(
        ExecutionPlan plan,
        string? finalShapeTableName = null,
        string? finalShapeTypeName = null,
        IReadOnlyList<FieldBinding>? finalShapeFields = null,
        bool omitFinalShapeClass = false)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var context = InitializeRenderContext(plan);

        return RenderClassMembers(
            plan,
            finalShapeTableName,
            finalShapeTypeName,
            finalShapeFields,
            context,
            omitFinalShapeClass);
    }

    private IReadOnlyList<MemberDeclarationSyntax> RenderClassMembers(
        ExecutionPlan plan,
        string? finalShapeTableName,
        string? finalShapeTypeName,
        IReadOnlyList<FieldBinding>? finalShapeFields,
        ExecutionRenderContext context,
        bool omitFinalShapeClass)
    {
        var session = context.Session;
        session.UseDirectTypedStoredRowsAlias = CanUseGeneratedFinalRowSink(plan, finalShapeTableName ?? string.Empty);
        if (finalShapeTableName != null)
        {
            var usesGeneratedRowCarrier = CanUseGeneratedFinalRowSink(plan, finalShapeTableName);
            var sinkTypeName = usesGeneratedRowCarrier && plan.FinalResult is { } finalResult
                ? finalResult.Shape.TypeName
                : finalShapeTypeName ?? string.Empty;
            var sourceBuffers = finalShapeTypeName != null && finalShapeFields != null
                ? CreateFinalShapeSourceBuffers(plan.Body, finalShapeTableName, finalShapeTypeName, finalShapeFields)
                : null;
            session.FinalShapeYieldSink = new FinalShapeYieldSink(
                finalShapeTableName,
                sinkTypeName,
                finalShapeFields ?? [],
                null,
                usesGeneratedRowCarrier ? null : sourceBuffers,
                usesGeneratedRowCarrier);
        }

        session.TypedStoredTableResults = CreateTypedStoredTableResults(plan);
        session.IncludeCteIndexResults = PlanUsesCteIndexResults(plan);
        session.IncludeCteRowResults = session.TypedStoredTableResults.Count > 0;
        session.IncludeTableResults = PlanUsesTableResults(plan, session.TypedStoredTableResults);
        session.OperatorCatalog = ExecutionPlanOperatorCatalog.Create(plan);
        session.TypedRowBufferVariables = CreateTypedRowBufferVariables(plan.Body, finalShapeTableName);
        session.SingleKeyAggregateUpdateHelpersByBlock = CollectSingleKeyAggregateUpdateHelpersByBlock(plan.Body);
        session.EnumerableTraversalHelpersByBlock = finalShapeTableName == null
            ? CollectEnumerableTraversalHelpersByBlock(plan.Body, context)
            : CollectEnumerableTraversalHelpersByBlock(plan.Body, context)
                .Where(pair => !CapturesCurrentFinalShapeTargetOrSourceBuffer(pair.Value, context))
                .ToDictionary(static pair => pair.Key, static pair => pair.Value);

            var members = new List<MemberDeclarationSyntax>();

            var constructorUsages = CollectGeneratedRowConstructorUsages(plan.Body, session.TypedStoredTableResults);
            session.GeneratedRowVariableTypeNamesByName = CollectGeneratedRowVariableTypeNames(plan.Body, session.TypedStoredTableResults);
            session.GeneratedRowConstructorUsagesByType = constructorUsages;
            session.GeneratedRowTypesUsedAsRowContexts = CollectGeneratedRowTypesUsedAsRowContexts(plan.Body);
            session.GeneratedRowTypesUsedAtPublicBoundary = CollectGeneratedRowTypesUsedAtPublicBoundary(plan.Body);
            session.GeneratedRowTypesRequiringRowBase = GeneratedRowCarrierUsage.CollectTypesRequiringRowBase(plan.Body, constructorUsages);
            var renderedGeneratedRows = new HashSet<string>(StringComparer.Ordinal);
            var renderedHashPayloads = new HashSet<string>(StringComparer.Ordinal);
            var renderedAggregateGroups = new HashSet<string>(StringComparer.Ordinal);
            var renderedOrderComparers = new HashSet<string>(StringComparer.Ordinal);
            var renderedGeneratedRowOrderComparers = new HashSet<string>(StringComparer.Ordinal);
            var tableRowShapesByVariableName = CreateTableRowShapeMap(plan.Body);
            session.TableRowShapesByVariableName = tableRowShapesByVariableName;

            members.AddRange(CreateQueryRowShapeFields(plan.Body));
            members.AddRange(CreateQueryRowMaterializers(plan.Body));

            foreach (var shape in plan.Shapes)
            {
                switch (shape)
                {
                    case GeneratedRowShape generated:
                        if (renderedGeneratedRows.Add(generated.TypeName))
                        {
                            constructorUsages.TryGetValue(generated.TypeName, out var usedConstructors);
                            members.Add(RenderGeneratedRowClass(generated, usedConstructors, context));
                        }
                        break;
                    case ValuesRowShape values:
                        if (renderedGeneratedRows.Add(values.GeneratedShape.TypeName))
                        {
                            constructorUsages.TryGetValue(values.GeneratedShape.TypeName, out var usedConstructors);
                            members.Add(RenderGeneratedRowClass(values.GeneratedShape, usedConstructors, context));
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
                        if (renderedAggregateGroups.Add(GetAggregateGroupTypeName(aggregateGroup, context)))
                            members.Add(RenderAggregateGroupClass(aggregateGroup, context));
                        break;
                    case ExpandoAdapterShape expando:
                        members.Add(RenderExpandoAdapterClass(expando));
                        break;
                }
            }

            if (plan.FinalResult != null &&
                !omitFinalShapeClass &&
                (session.FinalShapeYieldSink is null || !session.FinalShapeYieldSink.UsesGeneratedRowCarrier))
                members.Add(RenderFinalSelectShapeClass(plan.FinalResult));

            members.AddRange(CreateCteIndexResultMembers(plan));
            members.AddRange(CreateCteRowResultMembers(session.TypedStoredTableResults));
            members.AddRange(session.ConstantInSetFields.Select(static field => CreateConstantInSetField(field)));
            members.AddRange(session.StaticMetadataFields.Select(static field => CreateStaticMetadataField(field)));
            AddCollectionParameterMembers(plan, members);

            members.AddRange(session.SingleKeyAggregateUpdateHelpersByBlock.Values
                .Select(helper => CreateSingleKeyAggregateUpdateFunction(helper, context)));
            members.AddRange(session.EnumerableTraversalHelpersByBlock.Values
                .Select(helper => CreateEnumerableTraversalFunction(helper, context)));
            members.AddRange(CollectStoredTableBuilds(plan.Body, context).Select(build => CreateStoredTableBuildFunction(build, context)));
            members.AddRange(CollectHashJoinHelperSets(plan.Body)
                .Where(helperSet => CanUseHashJoinHelperSetInCurrentSink(helperSet, context))
                .SelectMany(helperSet => CreateHashJoinHelperFunctions(helperSet, context)));
            members.AddRange(CollectKeySetHelperSets(plan.Body)
                .Where(helperSet => CanUseKeySetHelperSetInCurrentSink(helperSet, context))
                .SelectMany(helperSet => CreateKeySetHelperFunctions(helperSet, context)));
            members.AddRange(CollectGeneratedWindowKeyStructs(plan.Body)
                .GroupBy(static key => key.TypeName)
                .Select(static group => group.First())
                .Select(CreateGeneratedWindowKeyStruct));
            members.AddRange(CollectRankingWindowKeyExtractionHelpers(plan.Body)
                .Select(CreateRankingWindowKeyExtractionFunction));
            members.AddRange(CollectWindowAppendRowsHelpers(plan.Body)
                .Where(helper => CanUseWindowAppendRowsHelperInCurrentSink(helper, context))
                .Select(helper => CreateWindowAppendRowsFunction(helper, context)));
            var previousTableRowShapesByVariableName = session.TableRowShapesByVariableName;
            session.TableRowShapesByVariableName = tableRowShapesByVariableName;
            try
            {
                members.AddRange(CollectSortedCopyHelpers(plan.Body)
                    .Where(helper => CanUseSortedCopyHelperInCurrentSink(helper, context))
                    .Select(helper => CreateSortedCopyFunction(helper, context)));
            }
            finally
            {
                session.TableRowShapesByVariableName = previousTableRowShapesByVariableName;
            }

            members.AddRange(CollectValueTupleAggregateHelpers(plan.Body)
                .Where(helper => CanUseAggregateFinalizeHelperInCurrentSink(helper.EnsureCapacity.Table.Name, context))
                .SelectMany(helper => CreateValueTupleAggregateFunctions(helper, context)));
            members.AddRange(CollectSingleKeyHashAggregateHelpers(plan.Body)
                .Where(helper => CanUseAggregateFinalizeHelperInCurrentSink(helper.EnsureCapacity.Table.Name, context))
                .SelectMany(helper => CreateSingleKeyAggregateFunctions(helper, context)));
            members.AddRange(CollectParallelBlocks(plan.Body)
                .SelectMany(block => CreateParallelBlockMembers(block, context)));
            var parallelFilterProjectLoops = CollectParallelFilterProjectLoops(plan.Body).ToArray();
            members.AddRange(parallelFilterProjectLoops
                .Where(loop => CanUseParallelFilterProjectHelperInCurrentSink(loop, context))
                .GroupBy(loop => CreateParallelFilterProjectFunctionName(loop, context))
                .Select(static group => group.First())
                .Select(loop => CreateParallelFilterProjectFunction(loop, context)));
            var parallelAggregateLoops = CollectParallelSingleKeyAggregateLoops(plan.Body).ToArray();
            var uniqueParallelAggregateLoops = parallelAggregateLoops
                .GroupBy(loop => CreateParallelSingleKeyAggregateFunctionName(loop, context))
                .Select(static group => group.First())
                .ToArray();
            members.AddRange(uniqueParallelAggregateLoops.Select(loop => CreateParallelSingleKeyAggregateFunction(loop, context)));
            members.AddRange(uniqueParallelAggregateLoops
                .Where(static loop => !IsChunkedParallelSingleKeyAggregate(loop))
                .Select(loop => CreateParallelSingleKeyAggregateShardFunction(loop, context)));
            members.AddRange(uniqueParallelAggregateLoops
                .Where(static loop => !IsChunkedParallelSingleKeyAggregate(loop))
                .Select(loop => CreateParallelSingleKeyAggregateWorkerClass(loop, context)));
            members.AddRange(uniqueParallelAggregateLoops
                .Where(IsChunkedParallelSingleKeyAggregate)
                .Select(loop => CreateParallelSingleKeyAggregateChunkFunction(loop, context)));
            members.AddRange(uniqueParallelAggregateLoops
                .Where(IsChunkedParallelSingleKeyAggregate)
                .Select(loop => CreateParallelSingleKeyAggregateChunkWorkerClass(loop, context)));
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
                if (IsCurrentFinalShapeSourceBuffer(input.SourceName, context) ||
                    IsCurrentFinalShapeSourceBuffer(input.TargetName, context))
                {
                    continue;
                }

                var comparerTypeName = CreateGeneratedRowOrderComparerTypeName(input.RowShape, input.Keys);
                if (renderedGeneratedRowOrderComparers.Add(comparerTypeName))
                    members.Add(CreateGeneratedRowOrderComparerClass(input.RowShape, input.Keys));
            }

            return CodegenHelperExtractionMetadata.AnnotateCandidateMembers(members);
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
