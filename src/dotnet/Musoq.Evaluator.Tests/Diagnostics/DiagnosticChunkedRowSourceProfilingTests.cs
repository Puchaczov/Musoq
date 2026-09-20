using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;

namespace Musoq.Evaluator.Tests.Diagnostics;

[TestClass]
public sealed class DiagnosticChunkedRowSourceProfilingTests
{
    [TestMethod]
    public void DiagnosticChunkedRowSource_WhenProducerIsSlow_DiagnosesSourceBound()
    {
        var recorder = new SourceProfileRecorder("slow", StopwatchProfileClock.Instance);
        var metricSignal = new MetricSignal();
        var source = new SlowProducerChunkedSource(CreateContext(recorder, metricSignal), "slow");
        var chunks = source.Chunks;

        using (var enumerator = ProfiledChunkedEnumerable<int>.Create(chunks, recorder).GetEnumerator())
        {
            try
            {
                Assert.IsTrue(source.ProducerStarted.Wait(TimeSpan.FromSeconds(5)));
                var moveNext = ThreadPoolMoveNext(enumerator);

                Assert.IsTrue(metricSignal.WaitFor(
                    ChunkMetric("slow", DiagnosticChunkMetricNames.ConsumerWaitOnEmptyCount),
                    1,
                    TimeSpan.FromSeconds(5)));
                source.AllowProduce.Set();

                Assert.IsTrue(moveNext.Wait(TimeSpan.FromSeconds(5)));
                Assert.IsTrue(moveNext.Result);
                CollectionAssert.AreEqual(new[] { 1 }, enumerator.Current.ToArray());
                Assert.IsFalse(enumerator.MoveNext());

                var snapshot = recorder.CreateSnapshot();

                Assert.AreEqual(SourceProfileDiagnosis.SourceBound, snapshot.Diagnosis);
                AssertMetricAtLeast(snapshot, ChunkMetric("slow", DiagnosticChunkMetricNames.ConsumerWaitOnEmptyCount), 1);
                AssertMetric(snapshot, ChunkMetric("slow", DiagnosticChunkMetricNames.ChunksProduced), 1);
                AssertMetric(snapshot, ChunkMetric("slow", DiagnosticChunkMetricNames.RowsConsumed), 1);
            }
            finally
            {
                source.AllowProduce.Set();
            }
        }
    }

    [TestMethod]
    public void DiagnosticChunkedRowSource_WhenProducerWaitsOnFullQueue_DiagnosesEvaluatorBound()
    {
        var recorder = new SourceProfileRecorder("fast", StopwatchProfileClock.Instance);
        var metricSignal = new MetricSignal();
        var source = new FastProducerBoundedChunkedSource(CreateContext(recorder, metricSignal), "fast");
        var chunks = source.Chunks;

        using var enumerator = ProfiledChunkedEnumerable<int>.Create(chunks, recorder).GetEnumerator();
        Assert.IsTrue(source.BeforeSecondWrite.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(metricSignal.WaitFor(
            ChunkMetric("fast", DiagnosticChunkMetricNames.ProducerWaitOnFullCount),
            1,
            TimeSpan.FromSeconds(5)));

        Assert.IsTrue(enumerator.MoveNext());
        CollectionAssert.AreEqual(new[] { 1 }, enumerator.Current.ToArray());
        Assert.IsTrue(metricSignal.WaitFor(
            ChunkMetric("fast", DiagnosticChunkMetricNames.ProducerWaitOnFullCount),
            2,
            TimeSpan.FromSeconds(5)));
        Assert.IsTrue(enumerator.MoveNext());
        CollectionAssert.AreEqual(new[] { 2 }, enumerator.Current.ToArray());
        Assert.IsTrue(source.SecondWriteCompleted.Wait(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(enumerator.MoveNext());
        CollectionAssert.AreEqual(new[] { 3 }, enumerator.Current.ToArray());
        Assert.IsFalse(enumerator.MoveNext());

        var snapshot = recorder.CreateSnapshot();

        Assert.AreEqual(SourceProfileDiagnosis.EvaluatorBound, snapshot.Diagnosis);
        AssertMetric(snapshot, ChunkMetric("fast", DiagnosticChunkMetricNames.ProducerWaitOnFullCount), 2);
        AssertMetric(snapshot, ChunkMetric("fast", DiagnosticChunkMetricNames.PeakBacklogInChunks), 1);
        AssertMetric(snapshot, ChunkMetric("fast", DiagnosticChunkMetricNames.ChunksConsumed), 3);
        AssertMetric(snapshot, ChunkMetric("fast", DiagnosticChunkMetricNames.RowsConsumed), 3);
    }

    private static SourceExecutionContext CreateContext(
        SourceProfileRecorder recorder,
        MetricSignal? metricSignal = null)
    {
        ISchemaColumn[] columns = [new SchemaColumn("Value", 0, typeof(int))];
        var diagnostics = recorder.CreateDiagnostics();
        if (metricSignal != null)
            diagnostics = new SourceDiagnostics(new SignalingDiagnosticsSink(diagnostics, metricSignal));

        return new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance,
            sourceDiagnostics: diagnostics);
    }

    private static MoveNextResult ThreadPoolMoveNext(IEnumerator<IReadOnlyList<int>> enumerator)
    {
        var result = new MoveNextResult();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                result.SetResult(enumerator.MoveNext());
            }
            catch (Exception exception)
            {
                result.SetException(exception);
            }
        });

        return result;
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

    private sealed class SignalingDiagnosticsSink(
        SourceDiagnostics inner,
        MetricSignal metricSignal) : ISourceDiagnosticsSink
    {
        public IDisposable Measure(string name, SourceDiagnosticOperation operation) =>
            inner.Measure(name, operation);

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
        private readonly Dictionary<(string Name, long Expected), ManualResetEventSlim> _waiters = [];

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
                    waiter.Set();
            }
        }

        public bool WaitFor(string name, long expected, TimeSpan timeout)
        {
            ManualResetEventSlim waiter;
            lock (_gate)
            {
                if (_values.TryGetValue(name, out var current) && current >= expected)
                    return true;

                waiter = new ManualResetEventSlim();
                _waiters[(name, expected)] = waiter;
            }

            try
            {
                return waiter.Wait(timeout);
            }
            finally
            {
                lock (_gate)
                    _waiters.Remove((name, expected));
                waiter.Dispose();
            }
        }
    }

    private sealed class SlowProducerChunkedSource(SourceExecutionContext context, string sourceName)
        : DiagnosticChunkedRowSource<int>(context, sourceName)
    {
        public ManualResetEventSlim ProducerStarted { get; } = new();

        public ManualResetEventSlim AllowProduce { get; } = new();

        protected override void CollectChunks(DiagnosticChunkWriter<int> writer)
        {
            ProducerStarted.Set();
            var token = writer.CancellationToken;
            AllowProduce.Wait(token);
            writer.Write([1]);
        }
    }

    private sealed class FastProducerBoundedChunkedSource(SourceExecutionContext context, string sourceName)
        : DiagnosticChunkedRowSource<int>(context, sourceName, new DiagnosticChunkedRowSourceOptions(1))
    {
        public ManualResetEventSlim BeforeSecondWrite { get; } = new();

        public ManualResetEventSlim SecondWriteCompleted { get; } = new();

        protected override void CollectChunks(DiagnosticChunkWriter<int> writer)
        {
            writer.Write([1]);
            BeforeSecondWrite.Set();
            writer.Write([2]);
            writer.Write([3]);
            SecondWriteCompleted.Set();
        }
    }

    private sealed class MoveNextResult
    {
        private readonly ManualResetEventSlim _completed = new();
        private bool _result;
        private Exception? _exception;

        public bool Result
        {
            get
            {
                if (_exception != null)
                    throw _exception;

                return _result;
            }
        }

        public bool Wait(TimeSpan timeout)
        {
            return _completed.Wait(timeout);
        }

        public void SetResult(bool result)
        {
            _result = result;
            _completed.Set();
        }

        public void SetException(Exception exception)
        {
            _exception = exception;
            _completed.Set();
        }
    }
}
