using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Diagnostics;

[TestClass]
public sealed class DiagnosticChunkedRowSourceProfilingTests
{
    [TestMethod]
    public async Task DiagnosticChunkedRowSource_WhenProducerIsSlow_DiagnosesSourceBound()
    {
        using var watchdog = new TestWatchdog();
        var recorder = new SourceProfileRecorder("slow", StopwatchProfileClock.Instance);
        var metricSignal = new MetricSignal();
        var source = new SlowProducerChunkedSource(CreateContext(recorder, metricSignal, watchdog.Token), "slow");
        using var enumerator = ProfiledChunkedEnumerable<int>.Create(source.Chunks, recorder).GetEnumerator();
        Task<bool>? moveNext = null;

        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.WaitAsync(source.ProducerStarted.Task, "the held producer");
                moveNext = TestWatchdog.StartWorker(enumerator.MoveNext);
                var waiting = metricSignal.Reached(
                    ChunkMetric("slow", DiagnosticChunkMetricNames.ConsumerWaitOnEmptyCount),
                    1
                );
                await watchdog.ReachedBeforeCompletionAsync(waiting, moveNext, "the consumer wait on the empty queue");
                source.AllowProduce.TrySetResult();

                await watchdog.WaitAsync(moveNext, "the first chunk");
                Assert.IsTrue(await moveNext);
                CollectionAssert.AreEqual(new[] { 1 }, enumerator.Current.ToArray());
                Assert.IsFalse(enumerator.MoveNext());

                var snapshot = recorder.CreateSnapshot();
                Assert.AreEqual(SourceProfileDiagnosis.SourceBound, snapshot.Diagnosis);
                AssertMetricAtLeast(
                    snapshot,
                    ChunkMetric("slow", DiagnosticChunkMetricNames.ConsumerWaitOnEmptyCount),
                    1
                );
                AssertMetric(snapshot, ChunkMetric("slow", DiagnosticChunkMetricNames.ChunksProduced), 1);
                AssertMetric(snapshot, ChunkMetric("slow", DiagnosticChunkMetricNames.RowsConsumed), 1);
            },
            () =>
            {
                source.AllowProduce.TrySetResult();
                return moveNext ?? Task.CompletedTask;
            }
        );
    }

    [TestMethod]
    public async Task DiagnosticChunkedRowSource_WhenProducerWaitsOnFullQueue_DiagnosesEvaluatorBound()
    {
        using var watchdog = new TestWatchdog();
        var recorder = new SourceProfileRecorder("fast", StopwatchProfileClock.Instance);
        var metricSignal = new MetricSignal();
        var source = new FastProducerBoundedChunkedSource(
            CreateContext(recorder, metricSignal, watchdog.Token),
            "fast"
        );
        using var enumerator = ProfiledChunkedEnumerable<int>.Create(source.Chunks, recorder).GetEnumerator();
        var waitOnFull = ChunkMetric("fast", DiagnosticChunkMetricNames.ProducerWaitOnFullCount);

        await watchdog.WaitAsync(metricSignal.Reached(waitOnFull, 1), "the producer's first wait on the full queue");
        Assert.IsTrue(enumerator.MoveNext());
        CollectionAssert.AreEqual(new[] { 1 }, enumerator.Current.ToArray());
        await watchdog.WaitAsync(metricSignal.Reached(waitOnFull, 2), "the producer's second wait on the full queue");
        Assert.IsTrue(enumerator.MoveNext());
        CollectionAssert.AreEqual(new[] { 2 }, enumerator.Current.ToArray());
        await watchdog.WaitAsync(source.WritesCompleted.Task, "all producer writes");
        Assert.IsTrue(enumerator.MoveNext());
        CollectionAssert.AreEqual(new[] { 3 }, enumerator.Current.ToArray());
        Assert.IsFalse(enumerator.MoveNext());

        var snapshot = recorder.CreateSnapshot();
        Assert.AreEqual(SourceProfileDiagnosis.EvaluatorBound, snapshot.Diagnosis);
        AssertMetric(snapshot, waitOnFull, 2);
        AssertMetric(snapshot, ChunkMetric("fast", DiagnosticChunkMetricNames.PeakBacklogInChunks), 1);
        AssertMetric(snapshot, ChunkMetric("fast", DiagnosticChunkMetricNames.ChunksConsumed), 3);
        AssertMetric(snapshot, ChunkMetric("fast", DiagnosticChunkMetricNames.RowsConsumed), 3);
    }

    private static SourceExecutionContext CreateContext(
        SourceProfileRecorder recorder,
        MetricSignal metricSignal,
        CancellationToken cancellationToken
    )
    {
        ISchemaColumn[] columns = [new SchemaColumn("Value", 0, typeof(int))];
        var diagnostics = new SourceDiagnostics(
            new SignalingDiagnosticsSink(recorder.CreateDiagnostics(), metricSignal)
        );

        return new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            cancellationToken,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance,
            sourceDiagnostics: diagnostics
        );
    }

    private static void AssertMetric(SourceProfileSnapshot snapshot, string name, long expected)
    {
        Assert.IsTrue(snapshot.Metrics.TryGetValue(name, out var value), $"Metric '{name}' was not recorded.");
        Assert.AreEqual(expected, value, name);
    }

    private static void AssertMetricAtLeast(SourceProfileSnapshot snapshot, string name, long expected)
    {
        Assert.IsTrue(snapshot.Metrics.TryGetValue(name, out var value), $"Metric '{name}' was not recorded.");
        Assert.IsGreaterThanOrEqualTo(expected, value, name);
    }

    private static string ChunkMetric(string sourceName, string metricName) =>
        DiagnosticChunkMetricNames.ForSource(sourceName, metricName);

    private sealed class SignalingDiagnosticsSink(SourceDiagnostics inner, MetricSignal metricSignal)
        : ISourceDiagnosticsSink
    {
        public IDisposable Measure(string name, SourceDiagnosticOperation operation) => inner.Measure(name, operation);

        public void AddRowsProduced(long count) => inner.AddRowsProduced(count);

        public void AddBytesRead(long bytes) => inner.AddBytesRead(bytes);

        public void AddMetric(string name, long value)
        {
            inner.AddMetric(name, value);
            metricSignal.Record(name, value);
        }
    }

    private sealed class MetricSignal
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, long> _values = new(StringComparer.Ordinal);
        private readonly Dictionary<(string Name, long Expected), TaskCompletionSource> _waiters = [];

        public void Record(string name, long value)
        {
            lock (_gate)
            {
                _values.TryGetValue(name, out var current);
                var total = current + value;
                _values[name] = total;
                var completedWaiters = _waiters
                    .Where(waiter => waiter.Key.Name == name && total >= waiter.Key.Expected)
                    .Select(static waiter => waiter.Value)
                    .ToArray();

                foreach (var waiter in completedWaiters)
                    waiter.TrySetResult();
            }
        }

        public Task Reached(string name, long expected)
        {
            lock (_gate)
            {
                if (_values.TryGetValue(name, out var current) && current >= expected)
                    return Task.CompletedTask;

                var signal = TestWatchdog.Signal();
                _waiters.Add((name, expected), signal);
                return signal.Task;
            }
        }
    }

    private sealed class SlowProducerChunkedSource(SourceExecutionContext context, string sourceName)
        : DiagnosticChunkedRowSource<int>(context, sourceName)
    {
        public TaskCompletionSource ProducerStarted { get; } = TestWatchdog.Signal();

        public TaskCompletionSource AllowProduce { get; } = TestWatchdog.Signal();

        protected override void CollectChunks(DiagnosticChunkWriter<int> writer)
        {
            ProducerStarted.SetResult();
            var token = writer.CancellationToken;
            AllowProduce.Task.WaitAsync(token).GetAwaiter().GetResult();
            writer.Write([1]);
        }
    }

    private sealed class FastProducerBoundedChunkedSource(SourceExecutionContext context, string sourceName)
        : DiagnosticChunkedRowSource<int>(context, sourceName, new DiagnosticChunkedRowSourceOptions(1))
    {
        public TaskCompletionSource WritesCompleted { get; } = TestWatchdog.Signal();

        protected override void CollectChunks(DiagnosticChunkWriter<int> writer)
        {
            writer.Write([1]);
            writer.Write([2]);
            writer.Write([3]);
            WritesCompleted.SetResult();
        }
    }
}
