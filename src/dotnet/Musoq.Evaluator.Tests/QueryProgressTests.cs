using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class QueryProgressTests
{
    [TestMethod]
    public void WrapChunks_WhenNoHandlerIsConfigured_ReturnsOriginalEnumerable()
    {
        var chunks = CreateChunks(2, 3);
        var context = new QueryRunContext(CancellationToken.None, queryId: "query");

        var wrapped = QueryProgressRuntime.WrapChunks(chunks, context, "source");

        Assert.AreSame(chunks, wrapped);
    }

    [TestMethod]
    public void Progress_IsPublishedAtRowThreshold_AndCompletesOnce()
    {
        var events = new List<QueryProgressEventArgs>();
        var context = CreateContext(
            (_, args) => events.Add(args),
            new QueryProgressOptions
            {
                RowsPerUpdate = 4,
                MinimumInterval = TimeSpan.FromDays(1)
            });
        var wrapped = QueryProgressRuntime.WrapChunks(
            CreateChunks(2, 2, 1),
            context,
            "source");

        _ = wrapped.ToArray();
        context.CompleteQueryProgress();
        context.CompleteQueryProgress();

        Assert.AreEqual(3, events.Count);
        Assert.AreEqual(4, events[0].QueryRowsProcessed);
        Assert.AreEqual(4, events[0].SourceRowsProcessed);
        Assert.IsFalse(events[0].IsFinal);
        Assert.AreEqual(5, events[1].QueryRowsProcessed);
        Assert.AreEqual(5, events[1].SourceRowsProcessed);
        Assert.IsFalse(events[1].IsFinal);
        Assert.AreEqual(5, events[2].QueryRowsProcessed);
        Assert.IsNull(events[2].SourceContextId);
        Assert.IsNull(events[2].SourceRowsProcessed);
        Assert.IsTrue(events[2].IsFinal);
        Assert.AreEqual(1, events[0].Sequence);
        Assert.AreEqual(2, events[1].Sequence);
        Assert.AreEqual(3, events[2].Sequence);
    }

    [TestMethod]
    public void Progress_FlushesPartiallyConsumedSourceOnDisposal()
    {
        var events = new List<QueryProgressEventArgs>();
        var context = CreateContext(
            (_, args) => events.Add(args),
            new QueryProgressOptions
            {
                RowsPerUpdate = 100,
                MinimumInterval = TimeSpan.FromDays(1)
            });
        var wrapped = QueryProgressRuntime.WrapChunks(CreateChunks(3, 3), context, "source");

        using (var enumerator = wrapped.GetEnumerator())
        {
            Assert.IsTrue(enumerator.MoveNext());
        }

        context.CompleteQueryProgress();

        Assert.AreEqual(2, events.Count);
        Assert.AreEqual(3, events[0].QueryRowsProcessed);
        Assert.IsFalse(events[0].IsFinal);
        Assert.AreEqual(3, events[1].QueryRowsProcessed);
        Assert.IsTrue(events[1].IsFinal);
    }

    [TestMethod]
    public void Progress_UsesInjectedClockForTimeThreshold()
    {
        var clock = new ManualTimeProvider();
        var events = new List<QueryProgressEventArgs>();
        var context = CreateContext(
            (_, args) => events.Add(args),
            new QueryProgressOptions
            {
                RowsPerUpdate = 100,
                MinimumInterval = TimeSpan.FromSeconds(5),
                TimeProvider = clock
            });
        var wrapped = QueryProgressRuntime.WrapChunks(CreateChunks(2, 2), context, "source");

        using var enumerator = wrapped.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(0, events.Count);

        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, events.Count);

        context.CompleteQueryProgress();
        Assert.AreEqual(2, events.Count);
    }

    [TestMethod]
    public void Progress_IsIsolatedBetweenRuns()
    {
        var first = new List<QueryProgressEventArgs>();
        var second = new List<QueryProgressEventArgs>();
        var options = new QueryProgressOptions
        {
            RowsPerUpdate = 100,
            MinimumInterval = TimeSpan.FromDays(1)
        };
        var firstContext = CreateContext((_, args) => first.Add(args), options, "first");
        var secondContext = CreateContext((_, args) => second.Add(args), options, "second");

        _ = QueryProgressRuntime.WrapChunks(CreateChunks(2), firstContext, "source").ToArray();
        _ = QueryProgressRuntime.WrapChunks(CreateChunks(3), secondContext, "source").ToArray();
        firstContext.CompleteQueryProgress();
        secondContext.CompleteQueryProgress();

        Assert.AreEqual(2, first.Count);
        Assert.AreEqual(2, first[0].QueryRowsProcessed);
        Assert.AreEqual("first", first[0].QueryId);
        Assert.IsTrue(first[1].IsFinal);
        Assert.AreEqual(2, second.Count);
        Assert.AreEqual(3, second[0].QueryRowsProcessed);
        Assert.AreEqual("second", second[0].QueryId);
        Assert.IsTrue(second[1].IsFinal);
    }

    [TestMethod]
    public void Progress_ConcurrentEnumeratorsRemainMonotonic()
    {
        var events = new ConcurrentQueue<QueryProgressEventArgs>();
        var context = CreateContext(
            (_, args) => events.Enqueue(args),
            new QueryProgressOptions
            {
                RowsPerUpdate = 1,
                MinimumInterval = TimeSpan.FromDays(1)
            });
        var wrapped = QueryProgressRuntime.WrapChunks(CreateChunks(1, 1, 1), context, "source");

        Parallel.For(0, 4, workerIndex =>
        {
            foreach (var chunk in wrapped)
            {
            }
        });
        context.CompleteQueryProgress();

        var snapshots = events.ToArray();
        Assert.IsTrue(snapshots.Length >= 2 && snapshots.Length <= 13);
        Assert.IsTrue(snapshots.Zip(snapshots.Skip(1), (left, right) => right.Sequence > left.Sequence).All(static value => value));
        Assert.AreEqual(12, snapshots[^1].QueryRowsProcessed);
        Assert.IsTrue(snapshots[^1].IsFinal);
    }

    [TestMethod]
    public void Progress_HandlerExceptionsPropagateWithoutPoisoningCompletion()
    {
        var events = new List<QueryProgressEventArgs>();
        var throwOnce = true;
        var context = CreateContext(
            (_, args) =>
            {
                events.Add(args);
                if (throwOnce)
                {
                    throwOnce = false;
                    throw new InvalidOperationException("progress handler failure");
                }
            },
            new QueryProgressOptions
            {
                RowsPerUpdate = 100,
                MinimumInterval = TimeSpan.FromDays(1)
            });
        var wrapped = QueryProgressRuntime.WrapChunks(CreateChunks(1), context, "source");

        using var enumerator = wrapped.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        Assert.Throws<InvalidOperationException>(() => context.CompleteQueryProgress());
        context.CompleteQueryProgress();

        Assert.AreEqual(2, events.Count);
        Assert.IsTrue(events[^1].IsFinal);
    }

    private static QueryRunContext CreateContext(
        QueryProgressEventHandler handler,
        QueryProgressOptions options,
        string queryId = "query")
    {
        return new QueryRunContext(
            CancellationToken.None,
            queryId: queryId,
            queryProgress: handler,
            queryProgressOptions: options);
    }

    private static IReadOnlyList<IReadOnlyList<int>> CreateChunks(params int[] counts)
    {
        return counts
            .Select(count => (IReadOnlyList<int>)Enumerable.Range(0, count).ToArray())
            .ToArray();
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan duration)
        {
            _timestamp += (long)(duration.TotalSeconds * TimestampFrequency);
        }
    }
}
