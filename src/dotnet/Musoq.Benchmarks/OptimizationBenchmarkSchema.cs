using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Tests.Common.SourcePlanning;

namespace Musoq.Benchmarks;

public sealed class OptimizationBenchmarkSchema(
    string schemaName,
    IReadOnlyList<OptimizationBenchmarkEntity> rows,
    OptimizationBenchmarkPlanningMode mode)
    : SchemaBase(schemaName, CreateLibrary())
{
    public const string Items = "items";

    private const string StrategyProperty = "BenchmarkSourcePlanningStrategy";
    private const string ProjectionWorkProperty = "BenchmarkProjectionWork";

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext metadataContext,
        params object?[] parameters)
    {
        if (string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
            return new OptimizationBenchmarkTable();

        return base.GetTableByName(name, metadataContext, parameters);
    }

    public override SourcePlanResult TryPlanSource(
        string name,
        SourcePlanRequest request,
        params object?[] parameters)
    {
        if (!string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
            return SourcePlanResult.RejectAll(request);

        return CreatePlanResult(request);
    }

    public override RowSource<T> GetRowSource<T>(
        string name,
        SourceExecutionContext executionContext,
        params object?[] parameters)
    {
        if (string.Equals(name, Items, StringComparison.OrdinalIgnoreCase))
        {
            return EnsureSourceType<T, OptimizationBenchmarkEntity>(
                name,
                new OptimizationBenchmarkRowSource(rows, executionContext.Plan));
        }

        return base.GetRowSource<T>(name, executionContext, parameters);
    }

    private SourcePlanResult CreatePlanResult(SourcePlanRequest request)
    {
        return mode switch
        {
            OptimizationBenchmarkPlanningMode.RejectAll => SourcePlanResult.RejectAll(request),
            OptimizationBenchmarkPlanningMode.RejectAllWithExactCardinality => SourcePlanResult.RejectAll(request) with
            {
                Cardinality = CardinalityEstimate.Exact(rows.Count, "Benchmark source knows its exact row count.")
            },
            OptimizationBenchmarkPlanningMode.RejectProjection => SourcePlanningPlanResultBuilder.CreateRejectedWithProperties(
                request,
                ProjectionWorkProperty,
                projectionWork: true),
            OptimizationBenchmarkPlanningMode.AcceptProjection => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                projectionWorkPropertyName: ProjectionWorkProperty,
                projectionWork: true),
            OptimizationBenchmarkPlanningMode.AcceptPredicate => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                acceptedPredicate: request.Predicate,
                predicateAccepted: true),
            OptimizationBenchmarkPlanningMode.AcceptTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: request.Take,
                residualTake: null),
            OptimizationBenchmarkPlanningMode.AcceptSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: [],
                residualOrderBy: request.OrderBy,
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null),
            OptimizationBenchmarkPlanningMode.AcceptOrder => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaiveSort),
            OptimizationBenchmarkPlanningMode.AcceptOrderSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaiveSort),
            OptimizationBenchmarkPlanningMode.AcceptNaiveOrder => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaiveSort),
            OptimizationBenchmarkPlanningMode.AcceptNaiveOrderSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaiveSort),
            OptimizationBenchmarkPlanningMode.AcceptTopNOrder => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.TopN),
            OptimizationBenchmarkPlanningMode.AcceptTopNOrderSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: request.Skip,
                residualSkip: null,
                acceptedTake: request.Take,
                residualTake: null,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.TopN),
            OptimizationBenchmarkPlanningMode.AcceptNaturalOrder => SourcePlanningPlanResultBuilder.CreateAccepted(
                request,
                acceptedOrderBy: request.OrderBy,
                residualOrderBy: [],
                acceptedSkip: null,
                residualSkip: request.Skip,
                acceptedTake: null,
                residualTake: request.Take,
                strategyPropertyName: StrategyProperty,
                strategy: SourcePlanningExecutionStrategy.NaturalOrder),
            OptimizationBenchmarkPlanningMode.AcceptNaturalOrderSkipTake => SourcePlanningPlanResultBuilder.CreateAccepted(
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

    private static MethodsAggregator CreateLibrary()
    {
        var methodsManager = new MethodsManager();
        methodsManager.RegisterLibraries(new LibraryBase());
        methodsManager.RegisterLibraries(new OptimizationBenchmarkLibrary());
        return new MethodsAggregator(methodsManager);
    }
}
