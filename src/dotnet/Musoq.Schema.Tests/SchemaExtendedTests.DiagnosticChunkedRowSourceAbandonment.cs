using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    [TestMethod]
    public void DiagnosticChunkedRowSource_WhenProducerIgnoresCancellation_ShouldReportAbandonment()
    {
        var sink = new CapturingChunkDiagnosticsSink();
        var source = new NonCooperativeDiagnosticChunkedSource(
            CreateSourceContext(new SourceDiagnostics(sink)),
            "items");

        using (var enumerator = source.Chunks.GetEnumerator())
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current[0]);
        }

        AssertMetric(sink, ChunkMetric("items", DiagnosticChunkMetricNames.ProducerAbandonedCount), 1);

        source.ReleaseProducer.Set();
        Assert.IsTrue(source.ProducerExited.Wait(TimeSpan.FromSeconds(2)));
    }

    private sealed class NonCooperativeDiagnosticChunkedSource(SourceExecutionContext context, string sourceName)
        : DiagnosticChunkedRowSource<int>(context, sourceName, new DiagnosticChunkedRowSourceOptions(1))
    {
        public ManualResetEventSlim ReleaseProducer { get; } = new();
        public ManualResetEventSlim ProducerExited { get; } = new();

        protected override void CollectChunks(DiagnosticChunkWriter<int> writer)
        {
            try
            {
                writer.Write([1]);
                ReleaseProducer.Wait();
            }
            finally
            {
                ProducerExited.Set();
            }
        }
    }
}
