using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    [TestMethod]
    public void RowSourceBase_WhenDiagnosticChunkedSourceExists_KeepsExistingBehavior()
    {
        var source = new ExistingChunkedSource();

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, source.Chunks.SelectMany(static chunk => chunk).ToArray());
    }

    [TestMethod]
    public void DiagnosticChunkedRowSource_WhenEnumerated_ReportsChunksRowsAndBacklog()
    {
        var sink = new CapturingChunkDiagnosticsSink();
        var scheduler = new RecordingProducerScheduler();
        var source = new ReportingDiagnosticChunkedSource(CreateSourceContext(new SourceDiagnostics(sink)), "items")
        {
            ProducerScheduler = scheduler,
        };

        using var enumerator = source.Chunks.GetEnumerator();
        scheduler.RunProducer();
        var rows = new List<int>();
        while (enumerator.MoveNext())
            rows.AddRange(enumerator.Current);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows);
        Assert.AreEqual(3, sink.RowsProduced);
        AssertMetric(sink, ChunkMetric("items", DiagnosticChunkMetricNames.ChunksProduced), 2);
        AssertMetric(sink, ChunkMetric("items", DiagnosticChunkMetricNames.ChunksConsumed), 2);
        AssertMetric(sink, ChunkMetric("items", DiagnosticChunkMetricNames.RowsProduced), 3);
        AssertMetric(sink, ChunkMetric("items", DiagnosticChunkMetricNames.RowsConsumed), 3);
        AssertMetric(sink, ChunkMetric("items", DiagnosticChunkMetricNames.PeakBacklogInChunks), 2);
    }

    [TestMethod]
    public void DiagnosticChunkedRowSource_WhenEnumeratedTwice_ShouldStartFreshProducerEachTime()
    {
        var source = new ReportingDiagnosticChunkedSource(CreateSourceContext(SourceDiagnostics.None), "items");
        var rows = source.Chunks;

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows.SelectMany(static chunk => chunk).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows.SelectMany(static chunk => chunk).ToArray());
    }

    [TestMethod]
    public void DiagnosticChunkedRowSource_WhenProducerFailsBeforeFirstRow_ShouldSurfaceException()
    {
        var source = new FailingDiagnosticChunkedSource(CreateSourceContext(SourceDiagnostics.None), "items");

        var exception = Assert.Throws<InvalidOperationException>(() => source.Chunks.ToArray());
        StringAssert.Contains(exception.Message, "producer failed");
    }

    [TestMethod]
    public Task DiagnosticChunkedRowSource_WhenConsumerDisposesEarly_ShouldCancelProducer()
    {
        var producer = new ControlledChunkProducer(cooperates: true);
        var source = new GatedDiagnosticChunkedSource(CreateSourceContext(SourceDiagnostics.None), "items", producer);
        return producer.VerifyConsumerDisposalAsync(source.Chunks);
    }

    private static SourceExecutionContext CreateSourceContext(SourceDiagnostics diagnostics)
    {
        ISchemaColumn[] columns = [new SchemaColumn("Value", 0, typeof(int))];

        return new SourceExecutionContext(
            "queryId",
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            CancellationToken.None,
            columns,
            new Dictionary<string, string>(),
            NullLogger.Instance,
            sourceDiagnostics: diagnostics
        );
    }

    private static void AssertMetric(CapturingChunkDiagnosticsSink sink, string name, long expected)
    {
        Assert.AreEqual(expected, GetMetric(sink, name), name);
    }

    private static long GetMetric(CapturingChunkDiagnosticsSink sink, string name)
    {
        Assert.IsTrue(sink.Metrics.TryGetValue(name, out var value), $"Metric '{name}' was not recorded.");
        return value;
    }

    private static string ChunkMetric(string sourceName, string metricName) =>
        DiagnosticChunkMetricNames.ForSource(sourceName, metricName);

    private sealed class ExistingChunkedSource : RowSourceBase<int>
    {
        protected override void CollectChunks(IChunkWriter<int> writer)
        {
            writer.Write(new[] { 1, 2 });
            writer.Write(new[] { 3 });
        }
    }

    private sealed class ReportingDiagnosticChunkedSource(SourceExecutionContext context, string sourceName)
        : DiagnosticChunkedRowSource<int>(context, sourceName)
    {
        protected override void CollectChunks(DiagnosticChunkWriter<int> writer)
        {
            writer.Write(new[] { 1, 2 });
            writer.Write(new[] { 3 });
        }
    }

    private sealed class FailingDiagnosticChunkedSource(SourceExecutionContext context, string sourceName)
        : DiagnosticChunkedRowSource<int>(context, sourceName)
    {
        protected override void CollectChunks(DiagnosticChunkWriter<int> writer)
        {
            throw new InvalidOperationException("producer failed");
        }
    }

    private sealed class GatedDiagnosticChunkedSource(
        SourceExecutionContext context,
        string sourceName,
        ControlledChunkProducer producer
    ) : DiagnosticChunkedRowSource<int>(context, sourceName, new DiagnosticChunkedRowSourceOptions(1))
    {
        protected override void CollectChunks(DiagnosticChunkWriter<int> writer) => producer.PublishAndWait(writer);
    }

    private sealed class CapturingChunkDiagnosticsSink : ISourceDiagnosticsSink
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, long> _metrics = new(StringComparer.Ordinal);
        private long _rowsProduced;

        public long RowsProduced
        {
            get
            {
                lock (_gate)
                {
                    return _rowsProduced;
                }
            }
        }

        public IReadOnlyDictionary<string, long> Metrics
        {
            get
            {
                lock (_gate)
                {
                    return new Dictionary<string, long>(_metrics, StringComparer.Ordinal);
                }
            }
        }

        public IDisposable Measure(string name, SourceDiagnosticOperation operation)
        {
            return new NoOpDisposable();
        }

        public void AddRowsProduced(long count)
        {
            lock (_gate)
            {
                _rowsProduced += count;
            }
        }

        public void AddBytesRead(long bytes) { }

        public void AddMetric(string name, long value)
        {
            lock (_gate)
            {
                _metrics[name] = _metrics.GetValueOrDefault(name) + value;
            }
        }
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
