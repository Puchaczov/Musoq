using Musoq.Schema.DataSources;
using Musoq.Tests.Common.SourcePlanning;

namespace Musoq.Benchmarks;

public sealed class OptimizationBenchmarkRowSource(
    IReadOnlyList<OptimizationBenchmarkEntity> rows,
    SourceExecutionPlan plan)
    : RowSource<OptimizationBenchmarkEntity>
{
    private const string StrategyProperty = "BenchmarkSourcePlanningStrategy";
    private const string ProjectionWorkProperty = "BenchmarkProjectionWork";
    private readonly IReadOnlyList<IReadOnlyList<OptimizationBenchmarkEntity>> _chunks =
        BenchmarkSourceChunks.Create(ApplyPlan(rows, plan));

    public override IEnumerable<IReadOnlyList<OptimizationBenchmarkEntity>> Chunks => _chunks;

    private static IEnumerable<OptimizationBenchmarkEntity> ApplyPlan(
        IEnumerable<OptimizationBenchmarkEntity> sourceRows,
        SourceExecutionPlan executionPlan)
    {
        return SourcePlanningRowExecution.ApplyPlan(
            sourceRows,
            executionPlan,
            new SourcePlanningRowExecutionOptions<OptimizationBenchmarkEntity>(
                StrategyProperty,
                CreateKeySelector,
                static row => row.Id,
                ApplyProjectionWork));
    }

    private static IEnumerable<OptimizationBenchmarkEntity> ApplyProjectionWork(
        IEnumerable<OptimizationBenchmarkEntity> sourceRows,
        SourceExecutionPlan executionPlan)
    {
        if (!executionPlan.Properties.TryGetValue(ProjectionWorkProperty, out var enabled) ||
            enabled is not true)
        {
            return sourceRows;
        }

        return SourcePlanningRowExecution.ApplyAcceptedColumnWork(
            sourceRows,
            executionPlan,
            nameof(OptimizationBenchmarkEntity.Payload),
            static row =>
            {
                _ = SimulatePayloadRead(row.Payload);
            });
    }

    private static int SimulatePayloadRead(string payload)
    {
        var hash = 17;

        for (var pass = 0; pass < 8; pass++)
            foreach (var character in payload)
                hash = unchecked(hash * 31 + character + pass);

        return hash;
    }

    private static Func<OptimizationBenchmarkEntity, object?> CreateKeySelector(string columnName)
    {
        return columnName switch
        {
            nameof(OptimizationBenchmarkEntity.Id) => static entity => entity.Id,
            nameof(OptimizationBenchmarkEntity.Name) => static entity => entity.Name,
            nameof(OptimizationBenchmarkEntity.Category) => static entity => entity.Category,
            nameof(OptimizationBenchmarkEntity.GroupKey) => static entity => entity.GroupKey,
            nameof(OptimizationBenchmarkEntity.JoinKey) => static entity => entity.JoinKey,
            nameof(OptimizationBenchmarkEntity.Score) => static entity => entity.Score,
            nameof(OptimizationBenchmarkEntity.Value) => static entity => entity.Value,
            nameof(OptimizationBenchmarkEntity.CreatedAt) => static entity => entity.CreatedAt,
            nameof(OptimizationBenchmarkEntity.Payload) => static entity => entity.Payload,
            _ => throw new InvalidOperationException($"Unsupported benchmark order column '{columnName}'.")
        };
    }

}
