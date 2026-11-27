using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using Musoq.Schema.Diagnostics;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    [TestMethod]
    public void DiagnosticChunkedRowSourceOptions_DefaultCapacity_ShouldBeBounded()
    {
        Assert.AreEqual(4, new DiagnosticChunkedRowSourceOptions().CapacityInChunks);
    }

    [TestMethod]
    public void DiagnosticChunkedRowSource_WhenChunksPropertyRead_ShouldNotStartProducer()
    {
        var scheduler = new RecordingProducerScheduler();
        var source = new StartTrackingDiagnosticChunkedSource(CreateSourceContext(SourceDiagnostics.None), "items")
        {
            ProducerScheduler = scheduler,
        };

        var chunks = source.Chunks;

        Assert.AreEqual(0, scheduler.QueuedCount);

        using var enumerator = chunks.GetEnumerator();

        Assert.AreEqual(1, scheduler.QueuedCount);
        scheduler.RunProducer();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current[0]);
        Assert.IsFalse(enumerator.MoveNext());
    }

    private sealed class StartTrackingDiagnosticChunkedSource(SourceExecutionContext context, string sourceName)
        : DiagnosticChunkedRowSource<int>(context, sourceName)
    {
        protected override void CollectChunks(DiagnosticChunkWriter<int> writer) => writer.Write([1]);
    }
}
