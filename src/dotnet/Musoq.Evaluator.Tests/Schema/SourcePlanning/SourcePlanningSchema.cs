using System;
using System.Collections.Generic;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;
using Musoq.Tests.Common.SourcePlanning;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public sealed partial class SourcePlanningSchema(
    string schemaName,
    IReadOnlyList<SourcePlanningEntity> rows,
    SourcePlanningMode mode,
    SourcePlanningRecorder recorder)
    : SchemaBase(schemaName, CreateLibrary())
{
    public const string Items = "items";

    private const string StrategyProperty = "TestSourcePlanningStrategy";

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
            return new SourcePlanningTable();

        return base.GetTableByName(name, metadataContext, parameters);
    }

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters)
    {
        recorder.RecordDescribe();
        var descriptor = base.DescribeSource(name, context, parameters);
        return mode switch
        {
            SourcePlanningMode.AcceptTypedStringPredicate => descriptor with
            {
                PredicateCapabilities = CreateStringPredicateCapabilities()
            },
            SourcePlanningMode.MalformedTypedMissingApplication or
                SourcePlanningMode.MalformedTypedDuplicateApplication or
                SourcePlanningMode.MalformedTypedAlteredApplication or
                SourcePlanningMode.MalformedTypedNestedAccepted => descriptor with
                {
                    PredicateCapabilities = CreateStringPredicateCapabilities()
                },
            SourcePlanningMode.AcceptTypedStringPrefixPredicate => descriptor with
            {
                PredicateCapabilities = CreateStringPredicateCapabilities(SourceStringMatchOperations.Prefix)
            },
            SourcePlanningMode.AcceptTypedStringPredicateUnknownVersion => descriptor with
            {
                PredicateCapabilities = CreateStringPredicateCapabilities(SourceStringMatchOperations.All) with
                {
                    ContractVersion = SourcePredicateCapabilities.CurrentContractVersion + 100
                }
            },
            SourcePlanningMode.MalformedTypedUnknownVersionApplication => descriptor with
            {
                PredicateCapabilities = CreateStringPredicateCapabilities() with
                {
                    ContractVersion = SourcePredicateCapabilities.CurrentContractVersion + 100
                }
            },
            SourcePlanningMode.MalformedTypedUnadvertisedPhaseApplication => descriptor with
            {
                PredicateCapabilities = CreateRowFilterOnlyStringPredicateCapabilities()
            },
            _ => descriptor
        };
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        if (!string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
            return SourcePlanResult.RejectAll(request);

        recorder.RecordRequest(request);
        var result = CreatePlanResult(request);
        recorder.RecordPlanResult(result);
        return result;
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
        {
            recorder.RecordExecutionPlan(executionContext.Plan);
            return EnsureSourceType<T, SourcePlanningEntity>(
                name,
                new SourcePlanningRowSource(rows, executionContext.Plan, recorder));
        }

        return base.GetRowSource<T>(name, executionContext, parameters);
    }

    private SourcePlanResult CreatePlanResult(SourcePlanRequest request)
    {
        return mode switch
        {
            SourcePlanningMode.RejectAll => SourcePlanResult.RejectAll(request),
            SourcePlanningMode.RejectAllWithExactCardinality => SourcePlanResult.RejectAll(request) with
            {
                Cardinality = CardinalityEstimate.Exact(rows.Count, "Test source knows its exact row count.")
            },
            SourcePlanningMode.RejectAllWithLowConfidenceCardinality => SourcePlanResult.RejectAll(request) with
            {
                Cardinality = CardinalityEstimate.Bounded(0, rows.Count, 0.25d, "Test source row count is low confidence.")
            },
            SourcePlanningMode.AcceptProjection => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take),
            SourcePlanningMode.AcceptPredicate => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                acceptedPredicate: request.Predicate,
                predicateAccepted: true),
            SourcePlanningMode.AcceptTypedStringPredicate or
                SourcePlanningMode.AcceptTypedStringPrefixPredicate or
                SourcePlanningMode.AcceptTypedStringPredicateUnknownVersion => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                acceptedPredicate: request.Predicate,
                predicateAccepted: request.Predicate != null),
            SourcePlanningMode.MalformedTypedMissingApplication => CreateMalformedTypedResult(
                request, MalformedTypedResultKind.MissingApplication),
            SourcePlanningMode.MalformedTypedDuplicateApplication => CreateMalformedTypedResult(
                request, MalformedTypedResultKind.DuplicateApplication),
            SourcePlanningMode.MalformedTypedAlteredApplication => CreateMalformedTypedResult(
                request, MalformedTypedResultKind.AlteredApplication),
            SourcePlanningMode.MalformedTypedNestedAccepted => CreateMalformedNestedTypedResult(request),
            SourcePlanningMode.MalformedTypedUnknownVersionApplication => CreateMalformedUnknownVersionResult(request),
            SourcePlanningMode.MalformedTypedUnadvertisedPhaseApplication => CreateMalformedTypedResult(
                request, MalformedTypedResultKind.UnadvertisedPhase),
            SourcePlanningMode.AcceptFirstPredicate => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                acceptedPredicate: SourcePlanningPlanResultBuilder.GetFirstConjunct(request.Predicate),
                residualPredicate: SourcePlanningPlanResultBuilder.RemoveFirstConjunct(request.Predicate),
                predicateAccepted: SourcePlanningPlanResultBuilder.GetFirstConjunct(request.Predicate) != null),
            SourcePlanningMode.AcceptPredicateOrderSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null,
                acceptedPredicate: request.Predicate,
                predicateAccepted: request.Predicate != null,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.TopN),
            SourcePlanningMode.AcceptTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: request.Take,
                residualTake: null),
            SourcePlanningMode.AcceptSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null),
            SourcePlanningMode.AcceptOrder => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaiveSort),
            SourcePlanningMode.AcceptOrderSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaiveSort),
            SourcePlanningMode.AcceptNaiveOrder => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaiveSort),
            SourcePlanningMode.AcceptNaiveOrderSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaiveSort),
            SourcePlanningMode.AcceptTopNOrder => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.TopN),
            SourcePlanningMode.AcceptTopNOrderSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.TopN),
            SourcePlanningMode.AcceptNaturalOrder => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaturalOrder),
            SourcePlanningMode.AcceptNaturalOrderSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaturalOrder),
            _ => SourcePlanResult.RejectAll(request)
        };
    }

    private static SourcePredicateCapabilities CreateStringPredicateCapabilities(
        SourceStringMatchOperations operations = SourceStringMatchOperations.All)
    {
        return new SourcePredicateCapabilities
        {
            StringMatches =
            [
                CreateStringMatchCapability(nameof(SourcePlanningEntity.Name), operations),
                CreateStringMatchCapability(nameof(SourcePlanningEntity.Category), operations)
            ]
        };
    }

    private static SourcePredicateCapabilities CreateRowFilterOnlyStringPredicateCapabilities()
    {
        return new SourcePredicateCapabilities
        {
            StringMatches =
            [
                new SourceStringMatchCapability(
                    new SourceColumnRef(nameof(SourcePlanningEntity.Name)),
                    SourceStringMatchOperations.All,
                    phases: SourcePredicateEvaluationPhases.RowFiltering)
            ]
        };
    }

    private static SourceStringMatchCapability CreateStringMatchCapability(
        string columnName,
        SourceStringMatchOperations operations)
    {
        return new SourceStringMatchCapability(
            new SourceColumnRef(columnName),
            operations,
            SourceStringComparison.LikeIgnoreCase,
            supportsNegation: true,
            SourcePredicateEvaluationPhases.All);
    }

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        return new MethodsAggregator(methodsManager);
    }
}
