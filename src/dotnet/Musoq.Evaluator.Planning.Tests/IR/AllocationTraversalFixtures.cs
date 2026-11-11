using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.IR;

/// <summary>Explicit constructor fixtures for every execution node and expression.</summary>
internal sealed partial class AllocationTraversalFixtures(bool includeOptional)
{
    private static readonly IReadOnlyDictionary<Type, Func<AllocationTraversalFixtures, object>> Factories =
        CreateFactories();
    private readonly bool _includeOptional = includeOptional;
    private readonly HashSet<Type> _active = [];
    private int _nextName;

    internal object Create(Type type)
    {
        if (!_active.Add(type))
            return null!;
        try
        {
            return Factories.TryGetValue(type, out var factory)
                ? factory(this)
                : throw new ArgumentException("Unregistered typed traversal fixture: " + type, nameof(type));
        }
        finally
        {
            _active.Remove(type);
        }
    }

    private static IReadOnlyDictionary<Type, Func<AllocationTraversalFixtures, object>> CreateFactories()
    {
        var factories = new Dictionary<Type, Func<AllocationTraversalFixtures, object>>();
        RegisterNodes(factories);
        RegisterExpressions(factories);
        RegisterDependencies(factories);
        return factories;
    }

    private ExecutionStructuralConstructionPlan CreateConstructionPlan()
    {
        var callable = (ExecutionCallableRef)Create(typeof(ExecutionCallableRef));
        var indexes = Enumerable.Range(0, callable.Descriptor.ParameterTypes.Count).ToArray();
        var defaults = indexes
            .Select(_ => (ExecutionExpression)new ExecutionLiteral(++_nextName, typeof(int)))
            .ToArray();
        return new ExecutionStructuralConstructionPlan(
            ExecutionClrBindingFactory.FromClr(typeof(int)),
            callable,
            (ExecutionStructuralShape)Create(typeof(ExecutionStructuralShape)),
            indexes,
            defaults,
            ExecutionStructuralPreparationLifetime.Inline
        );
    }

    internal static IEnumerable<object[]> NodeCases() =>
        new Type[]
        {
            typeof(ExecutionCteFusedProducerCandidate),
            typeof(ExecutionCteIndexOnlyStorageCandidate),
            typeof(ExecutionCteReadOnceFusionCandidate),
            typeof(ExecutionCteSidecarAppendRewriteCandidate),
            typeof(ExecutionCteSidecarIndexBuildCandidate),
            typeof(ExecutionCteSidecarIndexLoadCandidate),
            typeof(ExecutionCteSidecarIndexStoreCandidate),
            typeof(ExecutionCreateGeneratedRow),
            typeof(ExecutionCreateHashPayload),
            typeof(ExecutionHoistCandidateLet),
            typeof(ExecutionMethodTargetDeclarationCandidate),
            typeof(ExecutionAggregateCapturedValueSet),
            typeof(ExecutionAggregateSet),
            typeof(ExecutionCreateAggregateContext),
            typeof(ExecutionCreateAggregateLibrary),
            typeof(ExecutionCreateSingleKeyAggregateContext),
            typeof(ExecutionCreateValueTupleAggregateContext),
            typeof(ExecutionEnsureAggregateGroup),
            typeof(ExecutionGetOrAddSingleKeyAggregateGroup),
            typeof(ExecutionGetOrAddValueTupleAggregateGroup),
            typeof(ExecutionInitializeAggregateState),
            typeof(ExecutionParallelSingleKeyAggregateLoop),
            typeof(ExecutionAppendExistingRow),
            typeof(ExecutionAppendRecord),
            typeof(ExecutionAppendRow),
            typeof(ExecutionArrayAssign),
            typeof(ExecutionAssign),
            typeof(ExecutionBreak),
            typeof(ExecutionContinue),
            typeof(ExecutionContinueIf),
            typeof(ExecutionCreateBooleanArray),
            typeof(ExecutionCreateObject),
            typeof(ExecutionForEach),
            typeof(ExecutionForEachIndexed),
            typeof(ExecutionForEachWithOrdinality),
            typeof(ExecutionIf),
            typeof(ExecutionLet),
            typeof(ExecutionMaterializeFilteredList),
            typeof(ExecutionMaterializeList),
            typeof(ExecutionParallelBlock),
            typeof(ExecutionParallelFilterProjectLoop),
            typeof(ExecutionPrepareStructuralInput),
            typeof(ExecutionScopedBlock),
            typeof(ExecutionFusedCteProducer),
            typeof(ExecutionRecursiveCte),
            typeof(ExecutionRecursiveCteAppend),
            typeof(ExecutionRecursiveCteSnapshotRowGuard),
            typeof(ExecutionAsOfProbe),
            typeof(ExecutionCreateAsOfIndex),
            typeof(ExecutionCreateHash),
            typeof(ExecutionCreateKeySet),
            typeof(ExecutionCreateRangeIndex),
            typeof(ExecutionHashAdd),
            typeof(ExecutionHashProbe),
            typeof(ExecutionKeySetAdd),
            typeof(ExecutionKeySetProbe),
            typeof(ExecutionLoadCteIndex),
            typeof(ExecutionRangeProbe),
            typeof(ExecutionStoreCteIndex),
            typeof(ExecutionDistinctTable),
            typeof(ExecutionMaterializeRecordListToTable),
            typeof(ExecutionOrderRecordList),
            typeof(ExecutionPhaseBoundary),
            typeof(ExecutionProjectTable),
            typeof(ExecutionRelatedCtePhase),
            typeof(ExecutionSetOperation),
            typeof(ExecutionSkipTable),
            typeof(ExecutionSliceTable),
            typeof(ExecutionSortTable),
            typeof(ExecutionStoreTable),
            typeof(ExecutionTakeTable),
            typeof(ExecutionTopNTable),
            typeof(ExecutionTopOffsetTable),
            typeof(ExecutionReturnDesc),
            typeof(ExecutionReturnTable),
            typeof(ExecutionEnumerableSource),
            typeof(ExecutionInterpretSource),
            typeof(ExecutionSourceScan),
            typeof(ExecutionCreateBoundedRecordList),
            typeof(ExecutionCreateRecordList),
            typeof(ExecutionCreateTable),
            typeof(ExecutionCreateValuesRows),
            typeof(ExecutionEnsureTableCapacity),
            typeof(ExecutionComputeOffsetWindow),
            typeof(ExecutionComputePluginWindow),
            typeof(ExecutionComputeRankingWindow),
            typeof(ExecutionWindowAggregateKernel),
            typeof(ExecutionWindowKernelPlan),
            typeof(ExecutionSingleUsePipelineFusionCandidate),
        }.SelectMany(type => new[] { new object[] { type, false }, new object[] { type, true } });

    internal static IEnumerable<object[]> ExpressionCases() =>
        new Type[]
        {
            typeof(ExecutionMethodTargetReuseCandidate),
            typeof(ExecutionAggregateCall),
            typeof(ExecutionAggregateCapturedValueRead),
            typeof(ExecutionAggregateResultRef),
            typeof(ExecutionArrayAccess),
            typeof(ExecutionBetween),
            typeof(ExecutionBinary),
            typeof(ExecutionCaseWhen),
            typeof(ExecutionCoalesce),
            typeof(ExecutionCollectionInCheck),
            typeof(ExecutionContextArray),
            typeof(ExecutionCteCollectionInput),
            typeof(ExecutionDynamicLikeMatch),
            typeof(ExecutionDynamicRLikeMatch),
            typeof(ExecutionFieldRead),
            typeof(ExecutionGroupKeyRead),
            typeof(ExecutionInCheck),
            typeof(ExecutionIndexedHashRowCreate),
            typeof(ExecutionIndexedHashRowIndexRead),
            typeof(ExecutionIndexedHashRowRowRead),
            typeof(ExecutionIsNullCheck),
            typeof(ExecutionLikeMatcherCacheSlot),
            typeof(ExecutionLiteral),
            typeof(ExecutionMemberRead),
            typeof(ExecutionMethodCall),
            typeof(ExecutionNullContextArray),
            typeof(ExecutionPatternMatch),
            typeof(ExecutionPreparedLikeMatch),
            typeof(ExecutionPreparedRLikeMatch),
            typeof(ExecutionPrepareLikeMatcher),
            typeof(ExecutionPrepareRLikeMatcher),
            typeof(ExecutionRLikeMatcherCacheSlot),
            typeof(ExecutionRowContextsRead),
            typeof(ExecutionRowPresence),
            typeof(ExecutionRowStream),
            typeof(ExecutionScalarRowStream),
            typeof(ExecutionScriptParameterRead),
            typeof(ExecutionScriptVariableRead),
            typeof(ExecutionStoredTable),
            typeof(ExecutionStoredTableRows),
            typeof(ExecutionStrictCast),
            typeof(ExecutionStringConcatenation),
            typeof(ExecutionStringMatch),
            typeof(ExecutionStructuralRecord),
            typeof(ExecutionStructuralArray),
            typeof(ExecutionStructuralConversion),
            typeof(ExecutionSubquery),
            typeof(ExecutionUnary),
            typeof(ExecutionUnionMemberRead),
            typeof(ExecutionValueTupleKey),
            typeof(ExecutionVariableRead),
            typeof(ExecutionWindowResultRef),
            typeof(ExecutionWindowValueRead),
        }.SelectMany(type => new[] { new object[] { type, false }, new object[] { type, true } });
}
