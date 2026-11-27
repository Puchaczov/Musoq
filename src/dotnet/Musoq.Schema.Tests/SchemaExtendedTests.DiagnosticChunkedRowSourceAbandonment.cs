using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    [TestMethod]
    public Task DiagnosticChunkedRowSource_WhenProducerIgnoresCancellation_ShouldReportAbandonment()
    {
        var sink = new CapturingChunkDiagnosticsSink();
        var producer = new ControlledChunkProducer(cooperates: false);
        var source = new GatedDiagnosticChunkedSource(
            CreateSourceContext(new SourceDiagnostics(sink)),
            "items",
            producer
        )
        {
            ProducerShutdownWait = TimeSpan.Zero,
        };

        return producer.VerifyConsumerDisposalAsync(
            source.Chunks,
            () => AssertMetric(sink, ChunkMetric("items", DiagnosticChunkMetricNames.ProducerAbandonedCount), 1)
        );
    }
}
