using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Schema.DataSources;
using Musoq.Tests.Common.SourcePlanning;

namespace Musoq.Evaluator.Tests.Schema.SourcePlanning;

public sealed class SourcePlanningRowSource(
    IReadOnlyList<SourcePlanningEntity> rows,
    SourceExecutionPlan plan,
    SourcePlanningRecorder recorder,
    Func<Exception>? failureFactory = null)
    : RowSourceBase<SourcePlanningEntity>
{
    private const string StrategyProperty = "TestSourcePlanningStrategy";

    protected override void CollectChunks(IChunkWriter<SourcePlanningEntity> writer)
    {
        if (failureFactory != null)
            throw failureFactory();
        var chunk = ApplyPlan(rows, plan, recorder).ToArray();
        recorder.RecordRowsProduced(chunk.Length);
        writer.Write(chunk);
    }

    private static IEnumerable<SourcePlanningEntity> ApplyPlan(
        IEnumerable<SourcePlanningEntity> sourceRows,
        SourceExecutionPlan executionPlan,
        SourcePlanningRecorder recorder)
    {
        foreach (var application in executionPlan.PredicateApplications)
        {
            if (application.Predicate is not SourcePredicateRegexMatch regex)
                continue;
            var matcher = Operators.PrepareRLike(regex.Pattern, regex.MatchTimeout);
            var selector = CreateKeySelector(regex.Column.Name);
            sourceRows = sourceRows.Where(row =>
                Operators.RLikePrepared((string?)selector(row), matcher) != regex.IsNegated);
        }

        var remainingPlan = executionPlan with
        {
            AcceptedPredicate = RemoveRegexConjuncts(executionPlan.AcceptedPredicate),
            PredicateApplications = executionPlan.PredicateApplications
                .Where(application => application.Predicate is not SourcePredicateRegexMatch).ToArray()
        };
        return SourcePlanningRowExecution.ApplyPlan(
            sourceRows,
            remainingPlan,
            new SourcePlanningRowExecutionOptions<SourcePlanningEntity>(
                StrategyProperty,
                CreateKeySelector,
                static row => row.Id,
                (rows, plan) => RecordSourceComputations(rows, plan, recorder)));
    }

    private static SourcePredicateExpression? RemoveRegexConjuncts(SourcePredicateExpression? predicate)
    {
        if (predicate is SourcePredicateRegexMatch)
            return null;
        if (predicate is not SourcePredicateLogical { Operator: SourcePredicateLogicalOperator.And } logical)
            return predicate;
        var left = RemoveRegexConjuncts(logical.Left);
        var right = RemoveRegexConjuncts(logical.Right);
        if (left == null)
            return right;
        if (right == null)
            return left;
        return logical with { Left = left, Right = right };
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
