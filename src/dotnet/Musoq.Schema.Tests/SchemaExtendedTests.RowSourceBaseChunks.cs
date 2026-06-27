using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Tests;

public partial class SchemaExtendedTests
{
    [TestMethod]
    public void RowSourceBase_WhenChunksPropertyRead_ShouldNotStartProducer()
    {
        var source = new StartTrackingChunkedSource();

        var chunks = source.Chunks;

        Assert.IsFalse(source.ProducerStarted.Wait(TimeSpan.FromMilliseconds(100)));

        using var enumerator = chunks.GetEnumerator();

        Assert.IsTrue(source.ProducerStarted.Wait(TimeSpan.FromSeconds(2)));
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(42, enumerator.Current[0]);
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void RowSourceBase_WhenConsumerDisposesEarly_ShouldCancelProducer()
    {
        var source = new WaitingChunkedSource();

        using (var enumerator = source.Chunks.GetEnumerator())
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current[0]);
        }

        Assert.IsTrue(source.ProducerCancelled.Wait(TimeSpan.FromSeconds(2)));
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
    public void RowSourceBase_WhenProducerIgnoresCancellation_ShouldNotHangConsumerDispose()
    {
        var source = new NonCooperativeChunkedSource();
        var stopwatch = Stopwatch.StartNew();

        using (var enumerator = source.Chunks.GetEnumerator())
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current[0]);
        }

        stopwatch.Stop();
        source.ReleaseProducer.Set();

        Assert.IsLessThan(TimeSpan.FromSeconds(2), stopwatch.Elapsed);
        Assert.IsTrue(source.ProducerExited.Wait(TimeSpan.FromSeconds(2)));
    }

    private sealed class StartTrackingChunkedSource : RowSourceBase<int>
    {
        public ManualResetEventSlim ProducerStarted { get; } = new();

        protected override void CollectChunks(IChunkWriter<int> writer)
        {
            ProducerStarted.Set();
            writer.Write([42]);
        }
    }

    private sealed class WaitingChunkedSource : RowSourceBase<int>
    {
        public ManualResetEventSlim ProducerCancelled { get; } = new();

        protected override void CollectChunks(IChunkWriter<int> writer)
        {
            var token = writer.CancellationToken;
            writer.Write([1]);

            try
            {
                using var wait = new ManualResetEventSlim();
                wait.Wait(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                ProducerCancelled.Set();
                throw;
            }
        }
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

    private sealed class NonCooperativeChunkedSource : RowSourceBase<int>
    {
        public ManualResetEventSlim ReleaseProducer { get; } = new();
        public ManualResetEventSlim ProducerExited { get; } = new();

        protected override void CollectChunks(IChunkWriter<int> writer)
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
