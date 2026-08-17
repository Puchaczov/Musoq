using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using Musoq.Schema.Diagnostics;

namespace Musoq.Evaluator.Diagnostics;

public static class QueryProfileTelemetry
{
    public const string Name = "Musoq.Query";

    public static ActivitySource ActivitySource { get; } = new(Name);

    public static Meter Meter { get; } = new(Name);

    private static readonly Histogram<double> QueryElapsedMilliseconds =
        Meter.CreateHistogram<double>("musoq.query.elapsed", "ms", "Total profiled query elapsed time.");

    private static readonly Counter<long> SourceRowsRead =
        Meter.CreateCounter<long>("musoq.query.source.rows_read", "rows", "Rows read from a profiled source.");

    private static readonly Counter<long> SourceRowsProduced =
        Meter.CreateCounter<long>("musoq.query.source.rows_produced", "rows", "Rows produced by a profiled source.");

    private static readonly Histogram<double> SourceElapsedMilliseconds =
        Meter.CreateHistogram<double>("musoq.query.source.elapsed", "ms", "Elapsed time until the last source row.");

    private static readonly Histogram<double> SourceMoveNextWaitMilliseconds =
        Meter.CreateHistogram<double>("musoq.query.source.move_next_wait", "ms", "Time spent waiting for source MoveNext.");

    private static readonly Histogram<double> SourceConsumerGapMilliseconds =
        Meter.CreateHistogram<double>("musoq.query.source.consumer_gap", "ms", "Time between source rows consumed by the evaluator.");

    private static readonly Histogram<long> SourceBacklogChunks =
        Meter.CreateHistogram<long>("musoq.query.source.backlog_chunks", "chunks", "Observed chunked source backlog.");

    private static readonly Counter<long> OperatorRows =
        Meter.CreateCounter<long>("musoq.query.operator.rows", "rows", "Rows output by a profiled operator.");

    private static readonly Histogram<double> OperatorElapsedMilliseconds =
        Meter.CreateHistogram<double>("musoq.query.operator.elapsed", "ms", "Elapsed time attributed to a profiled operator.");

    public static void Emit(QueryProfileSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        EmitActivities(snapshot);
        EmitMeasurements(snapshot);
    }

    private static void EmitActivities(QueryProfileSnapshot snapshot)
    {
        if (!ActivitySource.HasListeners())
            return;

        using var queryActivity = ActivitySource.StartActivity("Musoq.Query", ActivityKind.Internal);
        queryActivity?.SetTag("musoq.query.id", snapshot.QueryId);
        queryActivity?.SetTag("musoq.query.elapsed_ms", snapshot.TotalElapsed.TotalMilliseconds);
        queryActivity?.SetTag("musoq.query.sources", snapshot.Sources.Count);
        queryActivity?.SetTag("musoq.query.operators", snapshot.Operators.Count);

        foreach (var source in snapshot.Sources)
            EmitSourceActivity(snapshot.QueryId, source);

        foreach (var operation in snapshot.Operators)
            EmitOperatorActivity(snapshot.QueryId, operation);
    }

    private static void EmitSourceActivity(string? queryId, SourceProfileSnapshot source)
    {
        using var activity = ActivitySource.StartActivity("Musoq.Query.Source", ActivityKind.Internal);

        activity?.SetTag("musoq.query.id", queryId);
        activity?.SetTag("musoq.source.id", source.Name);
        activity?.SetTag("musoq.source.name", source.Name);
        activity?.SetTag("musoq.source.rows_read", source.RowsRead);
        activity?.SetTag("musoq.source.rows_produced", source.RowsProduced);
        activity?.SetTag("musoq.source.move_next_wait_ms", source.MoveNextWaitTime.TotalMilliseconds);
        activity?.SetTag("musoq.source.consumer_gap_ms", source.ConsumerGapTime.TotalMilliseconds);
        activity?.SetTag("musoq.source.diagnosis", source.Diagnosis.ToString());

        foreach (var backlog in GetBacklogValues(source))
            activity?.SetTag("musoq.source.backlog_chunks", backlog);
    }

    private static void EmitOperatorActivity(string? queryId, OperatorProfileSnapshot operation)
    {
        using var activity = ActivitySource.StartActivity("Musoq.Query.Operator", ActivityKind.Internal);

        activity?.SetTag("musoq.query.id", queryId);
        activity?.SetTag("musoq.operator.id", operation.Id);
        activity?.SetTag("musoq.operator.name", operation.Name);
        activity?.SetTag("musoq.operator.has_actual_stats", operation.HasActualStats);
        activity?.SetTag("musoq.operator.input_rows", operation.InputRows);
        activity?.SetTag("musoq.operator.output_rows", operation.OutputRows);
        activity?.SetTag("musoq.operator.elapsed_ms", operation.ElapsedTime.TotalMilliseconds);
    }

    private static void EmitMeasurements(QueryProfileSnapshot snapshot)
    {
        if (QueryElapsedMilliseconds.Enabled)
            QueryElapsedMilliseconds.Record(
                snapshot.TotalElapsed.TotalMilliseconds,
                CreateQueryTags(snapshot.QueryId));

        foreach (var source in snapshot.Sources)
            EmitSourceMeasurements(snapshot.QueryId, source);

        foreach (var operation in snapshot.Operators)
            EmitOperatorMeasurements(snapshot.QueryId, operation);
    }

    private static void EmitSourceMeasurements(string? queryId, SourceProfileSnapshot source)
    {
        var tags = CreateSourceTags(queryId, source.Name);

        if (SourceRowsRead.Enabled)
            SourceRowsRead.Add(source.RowsRead, tags);

        if (SourceRowsProduced.Enabled)
            SourceRowsProduced.Add(source.RowsProduced, tags);

        if (SourceElapsedMilliseconds.Enabled && source.LastRowTime.HasValue)
            SourceElapsedMilliseconds.Record(source.LastRowTime.Value.TotalMilliseconds, tags);

        if (SourceMoveNextWaitMilliseconds.Enabled)
            SourceMoveNextWaitMilliseconds.Record(source.MoveNextWaitTime.TotalMilliseconds, tags);

        if (SourceConsumerGapMilliseconds.Enabled)
            SourceConsumerGapMilliseconds.Record(source.ConsumerGapTime.TotalMilliseconds, tags);

        if (SourceBacklogChunks.Enabled)
        {
            foreach (var backlog in GetBacklogValues(source))
                SourceBacklogChunks.Record(backlog, tags);
        }
    }

    private static void EmitOperatorMeasurements(string? queryId, OperatorProfileSnapshot operation)
    {
        if (!operation.HasActualStats)
            return;

        var tags = CreateOperatorTags(queryId, operation);

        if (OperatorRows.Enabled)
            OperatorRows.Add(operation.OutputRows, tags);

        if (OperatorElapsedMilliseconds.Enabled)
            OperatorElapsedMilliseconds.Record(operation.ElapsedTime.TotalMilliseconds, tags);
    }

    private static IEnumerable<long> GetBacklogValues(SourceProfileSnapshot source)
    {
        return source.Metrics
            .Where(static metric => DiagnosticChunkMetricNames.IsForSourceMetric(
                metric.Key,
                DiagnosticChunkMetricNames.PeakBacklogInChunks))
            .Select(static metric => Math.Max(0, metric.Value));
    }

    private static KeyValuePair<string, object?>[] CreateQueryTags(string? queryId) =>
    [
        new("musoq.query.id", queryId)
    ];

    private static KeyValuePair<string, object?>[] CreateSourceTags(string? queryId, string sourceName) =>
    [
        new("musoq.query.id", queryId),
        new("musoq.source.id", sourceName),
        new("musoq.source.name", sourceName)
    ];

    private static KeyValuePair<string, object?>[] CreateOperatorTags(string? queryId, OperatorProfileSnapshot operation) =>
    [
        new("musoq.query.id", queryId),
        new("musoq.operator.id", operation.Id),
        new("musoq.operator.name", operation.Name)
    ];
}
