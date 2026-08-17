using System;
using System.Threading;
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
        var source = new StartTrackingDiagnosticChunkedSource(
            CreateSourceContext(SourceDiagnostics.None),
            "items");

        var chunks = source.Chunks;

        Assert.IsFalse(source.ProducerStarted.Wait(TimeSpan.FromMilliseconds(100)));

        using var enumerator = chunks.GetEnumerator();

        Assert.IsTrue(source.ProducerStarted.Wait(TimeSpan.FromSeconds(2)));
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current[0]);
        Assert.IsFalse(enumerator.MoveNext());
    }

    private sealed class StartTrackingDiagnosticChunkedSource(SourceExecutionContext context, string sourceName)
        : DiagnosticChunkedRowSource<int>(context, sourceName)
    {
        public ManualResetEventSlim ProducerStarted { get; } = new();

        protected override void CollectChunks(DiagnosticChunkWriter<int> writer)
        {
            ProducerStarted.Set();
            writer.Write([1]);
        }
    }
}
