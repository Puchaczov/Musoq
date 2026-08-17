using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common.SourcePlanning;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public sealed class SourcePlanningRowSource(
    IReadOnlyList<SourcePlanningEntity> rows,
    SourceExecutionPlan plan,
    SourcePlanningRecorder recorder)
    : RowSourceBase<SourcePlanningEntity>
{
    private const string StrategyProperty = "TestSourcePlanningStrategy";

    protected override void CollectChunks(IChunkWriter<SourcePlanningEntity> writer)
    {
        var chunk = ApplyPlan(rows, plan, recorder).ToArray();
        recorder.RecordRowsProduced(chunk.Length);
        writer.Write(chunk);
    }

    private static IEnumerable<SourcePlanningEntity> ApplyPlan(
        IEnumerable<SourcePlanningEntity> sourceRows,
        SourceExecutionPlan executionPlan,
        SourcePlanningRecorder recorder)
    {
        return SourcePlanningRowExecution.ApplyPlan(
            sourceRows,
            executionPlan,
            new SourcePlanningRowExecutionOptions<SourcePlanningEntity>(
                StrategyProperty,
                CreateKeySelector,
                static row => row.Id,
                (rows, plan) => RecordSourceComputations(rows, plan, recorder)));
    }

    private static IEnumerable<SourcePlanningEntity> RecordSourceComputations(
        IEnumerable<SourcePlanningEntity> sourceRows,
        SourceExecutionPlan executionPlan,
        SourcePlanningRecorder recorder)
    {
        return SourcePlanningRowExecution.ApplyAcceptedColumnWork(
            sourceRows,
            executionPlan,
            nameof(SourcePlanningEntity.ExpensivePayload),
            row =>
            {
                recorder.RecordExpensivePayloadComputed();
                _ = row.ExpensivePayload;
            });
    }

    private static Func<SourcePlanningEntity, object?> CreateKeySelector(string columnName)
    {
        return columnName switch
        {
            nameof(SourcePlanningEntity.Id) => static entity => entity.Id,
            nameof(SourcePlanningEntity.Name) => static entity => entity.Name,
            nameof(SourcePlanningEntity.Category) => static entity => entity.Category,
            nameof(SourcePlanningEntity.Score) => static entity => entity.Score,
            nameof(SourcePlanningEntity.CreatedAt) => static entity => entity.CreatedAt,
            nameof(SourcePlanningEntity.JoinKey) => static entity => entity.JoinKey,
            nameof(SourcePlanningEntity.ExpensivePayload) => static entity => entity.ExpensivePayload,
            _ => throw new InvalidOperationException($"Unsupported source-planning order column '{columnName}'.")
        };
    }

}
