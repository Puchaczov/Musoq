using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    [TestMethod]
    public void RowSourceBase_WhenChunksPropertyRead_ShouldNotStartProducer()
    {
        var scheduler = new RecordingProducerScheduler();
        var source = new StartTrackingChunkedSource { ProducerScheduler = scheduler };

        var chunks = source.Chunks;

        Assert.AreEqual(0, scheduler.QueuedCount);

        using var enumerator = chunks.GetEnumerator();

        Assert.AreEqual(1, scheduler.QueuedCount);
        scheduler.RunProducer();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(42, enumerator.Current[0]);
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public Task RowSourceBase_WhenConsumerDisposesEarly_ShouldCancelProducer()
    {
        var producer = new ControlledChunkProducer(cooperates: true);
        var source = new GatedChunkedSource(producer);
        return producer.VerifyConsumerDisposalAsync(source.Chunks);
    }

    [TestMethod]
    public void RowSourceBase_WhenChunksAreEnumeratedTwice_ShouldStartFreshProducerEachTime()
    {
        var source = new RepeatableChunkedSource();
        var chunks = source.Chunks;

        CollectionAssert.AreEqual(new[] { 1, 2 }, chunks.SelectMany(static chunk => chunk).ToArray());
        CollectionAssert.AreEqual(new[] { 2, 4 }, chunks.SelectMany(static chunk => chunk).ToArray());

        Assert.AreEqual(2, source.ProducerStarts);
    }

    [TestMethod]
    public void RowSourceBase_WhenProducerWritesLargeArray_ShouldExposeFixedSizeRowChunkViews()
    {
        var source = new LargeArrayChunkedSource();

        var chunks = source.Chunks.ToArray();

        Assert.HasCount(2, chunks);
        Assert.IsInstanceOfType<RowChunk<int>>(chunks[0]);
        Assert.IsInstanceOfType<RowChunk<int>>(chunks[1]);
        Assert.AreEqual(RowChunking.DefaultChunkSize, chunks[0].Count);
        Assert.AreEqual(1, chunks[1].Count);
        Assert.AreSame(source.Rows, ((RowChunk<int>)chunks[0]).Source);
        Assert.AreSame(source.Rows, ((RowChunk<int>)chunks[1]).Source);
    }

    [TestMethod]
    public void RowSourceBase_WhenProducerFails_ShouldSurfaceException()
    {
        var source = new FailingChunkedSource();

        var exception = Assert.Throws<InvalidOperationException>(() => source.Chunks.ToArray());

        StringAssert.Contains(exception.Message, "row source failed");
    }

    [TestMethod]
    public void RowSourceBase_WhenProducerFailsAfterBufferedChunk_ShouldSurfaceException()
    {
        var source = new FailingAfterChunkedSource();

        using var enumerator = source.Chunks.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current[0]);

        var exception = Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        StringAssert.Contains(exception.Message, "row source failed after chunk");
    }

    [TestMethod]
    public Task RowSourceBase_WhenProducerIgnoresCancellation_ShouldNotHangConsumerDispose()
    {
        var producer = new ControlledChunkProducer(cooperates: false);
        var source = new GatedChunkedSource(producer);
        return producer.VerifyConsumerDisposalAsync(source.Chunks);
    }

    private sealed class StartTrackingChunkedSource : RowSourceBase<int>
    {
        protected override void CollectChunks(IChunkWriter<int> writer) => writer.Write([42]);
    }

    private sealed class GatedChunkedSource(ControlledChunkProducer producer) : RowSourceBase<int>
    {
        protected override void CollectChunks(IChunkWriter<int> writer) => producer.PublishAndWait(writer);
    }

    private sealed class FailingChunkedSource : RowSourceBase<int>
    {
        protected override void CollectChunks(IChunkWriter<int> writer)
        {
            throw new InvalidOperationException("row source failed");
        }
    }

    private sealed class FailingAfterChunkedSource : RowSourceBase<int>
    {
        protected override void CollectChunks(IChunkWriter<int> writer)
        {
            writer.Write([1]);
            throw new InvalidOperationException("row source failed after chunk");
        }
    }

    private sealed class RepeatableChunkedSource : RowSourceBase<int>
    {
        private int _producerStarts;

        public int ProducerStarts => _producerStarts;

        protected override void CollectChunks(IChunkWriter<int> writer)
        {
            var value = Interlocked.Increment(ref _producerStarts);
            writer.Write([value, value * 2]);
        }
    }

    private sealed class LargeArrayChunkedSource : RowSourceBase<int>
    {
        public int[] Rows { get; } = Enumerable.Range(0, RowChunking.DefaultChunkSize + 1).ToArray();

        protected override void CollectChunks(IChunkWriter<int> writer)
        {
            writer.Write(Rows);
        }
    }
}
