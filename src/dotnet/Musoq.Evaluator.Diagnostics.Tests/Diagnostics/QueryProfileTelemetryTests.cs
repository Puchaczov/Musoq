using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Diagnostics;
using Musoq.Schema.Diagnostics;

namespace Musoq.Evaluator.Tests.Diagnostics;

[TestClass]
public sealed class QueryProfileTelemetryTests
{
    [TestMethod]
    public void QueryProfileTelemetry_WhenActivityListenerIsEnabled_EmitsQuerySourceAndOperatorActivities()
    {
        var activities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == QueryProfileTelemetry.Name,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Enqueue
        };

        ActivitySource.AddActivityListener(listener);

        QueryProfileTelemetry.Emit(CreateSnapshot("activity-query"));

        var queryActivity = activities.First(static activity =>
            activity.OperationName == "Musoq.Query" &&
            Equals(activity.GetTagItem("musoq.query.id"), "activity-query"));
        Assert.AreEqual("activity-query", queryActivity.GetTagItem("musoq.query.id"));
        Assert.IsTrue(activities.Any(static activity =>
            activity.OperationName == "Musoq.Query.Source" &&
            Equals(activity.GetTagItem("musoq.query.id"), "activity-query")));
        Assert.IsTrue(activities.Any(static activity =>
            activity.OperationName == "Musoq.Query.Operator" &&
            Equals(activity.GetTagItem("musoq.query.id"), "activity-query")));

        var sourceActivity = activities.First(static activity =>
            activity.OperationName == "Musoq.Query.Source" &&
            Equals(activity.GetTagItem("musoq.query.id"), "activity-query") &&
            Equals(activity.GetTagItem("musoq.source.name"), "items"));
        Assert.AreEqual("activity-query", sourceActivity.GetTagItem("musoq.query.id"));
        Assert.AreEqual("items", sourceActivity.GetTagItem("musoq.source.id"));
        Assert.AreEqual("items", sourceActivity.GetTagItem("musoq.source.name"));
        Assert.AreEqual("Balanced", sourceActivity.GetTagItem("musoq.source.diagnosis"));

        var operatorActivity = activities.First(static activity =>
            activity.OperationName == "Musoq.Query.Operator" &&
            Equals(activity.GetTagItem("musoq.query.id"), "activity-query") &&
            Equals(activity.GetTagItem("musoq.operator.id"), "op1"));
        Assert.AreEqual("activity-query", operatorActivity.GetTagItem("musoq.query.id"));
        Assert.AreEqual("op1", operatorActivity.GetTagItem("musoq.operator.id"));
        Assert.AreEqual("SourceScan", operatorActivity.GetTagItem("musoq.operator.name"));
    }

    [TestMethod]
    public void QueryProfileTelemetry_WhenSnapshotsAreEmittedConcurrently_ListenerReceivesCompleteActivityTrees()
    {
        var activities = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == QueryProfileTelemetry.Name,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Enqueue
        };

        ActivitySource.AddActivityListener(listener);
        Parallel.For(0, 8, _ => QueryProfileTelemetry.Emit(CreateSnapshot("concurrent-query")));

        var matchingActivities = activities
            .Where(static activity => Equals(activity.GetTagItem("musoq.query.id"), "concurrent-query"))
            .ToArray();
        Assert.HasCount(8, matchingActivities.Where(static activity => activity.OperationName == "Musoq.Query"));
        Assert.HasCount(8, matchingActivities.Where(static activity => activity.OperationName == "Musoq.Query.Source"));
        Assert.HasCount(8, matchingActivities.Where(static activity => activity.OperationName == "Musoq.Query.Operator"));
    }

    [TestMethod]
    public void QueryProfileTelemetry_WhenMeterListenerIsEnabled_EmitsRowsDurationsAndBacklogMeasurements()
    {
        var measurements = new ConcurrentQueue<RecordedMeasurement>();
        using var listener = new MeterListener();

        listener.InstrumentPublished = static (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == QueryProfileTelemetry.Name)
                meterListener.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) =>
            measurements.Enqueue(new RecordedMeasurement(instrument.Name, ToDictionary(tags))));
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) =>
            measurements.Enqueue(new RecordedMeasurement(instrument.Name, ToDictionary(tags))));
        listener.Start();

        QueryProfileTelemetry.Emit(CreateSnapshot("meter-query"));

        var queryMeasurements = measurements
            .Where(static measurement =>
                measurement.Tags.TryGetValue("musoq.query.id", out var queryId) &&
                Equals(queryId, "meter-query"))
            .ToArray();
        var measurementNames = queryMeasurements.Select(static measurement => measurement.Name).ToArray();
        CollectionAssert.Contains(measurementNames, "musoq.query.elapsed");
        CollectionAssert.Contains(measurementNames, "musoq.query.source.rows_read");
        CollectionAssert.Contains(measurementNames, "musoq.query.source.rows_produced");
        CollectionAssert.Contains(measurementNames, "musoq.query.source.move_next_wait");
        CollectionAssert.Contains(measurementNames, "musoq.query.source.consumer_gap");
        CollectionAssert.Contains(measurementNames, "musoq.query.source.backlog_chunks");
        CollectionAssert.Contains(measurementNames, "musoq.query.operator.rows");
        CollectionAssert.Contains(measurementNames, "musoq.query.operator.elapsed");

        var queryMeasurement = queryMeasurements.First(static measurement => measurement.Name == "musoq.query.elapsed");
        Assert.AreEqual("meter-query", queryMeasurement.Tags["musoq.query.id"]);

        var sourceMeasurement = queryMeasurements.First(static measurement => measurement.Name == "musoq.query.source.rows_read");
        Assert.AreEqual("meter-query", sourceMeasurement.Tags["musoq.query.id"]);
        Assert.AreEqual("items", sourceMeasurement.Tags["musoq.source.id"]);

        var operatorMeasurement = queryMeasurements.First(static measurement => measurement.Name == "musoq.query.operator.rows");
        Assert.AreEqual("meter-query", operatorMeasurement.Tags["musoq.query.id"]);
        Assert.AreEqual("op1", operatorMeasurement.Tags["musoq.operator.id"]);
    }

    private static QueryProfileSnapshot CreateSnapshot(string queryId)
    {
        return new QueryProfileSnapshot(
            TimeSpan.FromMilliseconds(12),
            [
                new SourceProfileSnapshot(
                    "items",
                    3,
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromMilliseconds(4),
                    TimeSpan.FromMilliseconds(5),
                    TimeSpan.FromMilliseconds(2),
                    0,
                    null,
                    null,
                    SourceProfileDiagnosis.Balanced)
                {
                    RowsProduced = 3,
                    Metrics = new Dictionary<string, long>(StringComparer.Ordinal)
                    {
                        [DiagnosticChunkMetricNames.ForSource("items", DiagnosticChunkMetricNames.PeakBacklogInChunks)] = 2
                    }
                }
            ],
            [
                new OperatorProfileSnapshot(
                    "op1",
                    "SourceScan",
                    0,
                    3,
                    TimeSpan.FromMilliseconds(6))
            ])
        {
            QueryId = queryId
        };
    }

    private static Dictionary<string, object?> ToDictionary(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in tags)
            result[tag.Key] = tag.Value;

        return result;
    }

    private sealed record RecordedMeasurement(string Name, IReadOnlyDictionary<string, object?> Tags);
}
