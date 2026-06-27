using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class ProjectionRowsRuntimeTests
{
    static ProjectionRowsRuntimeTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void TypedProjectionRows_ProjectValuesSerial_WhenCancelled_ShouldThrow()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => TypedProjectionRows
            .ProjectValuesSerial(
                Enumerable.Range(0, 10),
                static _ => true,
                static value => value,
                cancellation.Token)
            .ToArray());
    }

    [TestMethod]
    public void TableProjectionRows_ProjectRowsSerial_WhenCancelled_ShouldThrow()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => TableProjectionRows
            .ProjectRowsSerial(
                Enumerable.Range(0, 10),
                static _ => true,
                static value => new TestRow([value]),
                cancellation.Token)
            .ToArray());
    }

    [TestMethod]
    public void ProjectRowsParallel_ShouldPreserveDeterministicShardOrder()
    {
        var rows = Enumerable.Range(0, 10_000).ToArray();

        var shards = EvaluationHelper.ProjectRowsParallel<int, TestRow>(
            rows,
            maxDegreeOfParallelism: 4,
            static value => value % 3 == 0,
            static value => new TestRow([value * 2]),
            CancellationToken.None);

        var projected = QueryRows
            .FromRowShards(shards)
            .Select(static row => (int)row[0])
            .ToArray();
        var expected = rows
            .Where(static value => value % 3 == 0)
            .Select(static value => value * 2)
            .ToArray();

        CollectionAssert.AreEqual(expected, projected);
    }

    [TestMethod]
    public void GetParallelProjectionRowsOrEmpty_WithReusableChunksAboveThreshold_ShouldReturnRows()
    {
        IReadOnlyList<IReadOnlyList<int>> chunks =
        [
            Enumerable.Range(0, 2_048).ToArray(),
            Enumerable.Range(2_048, 2_048).ToArray()
        ];

        var rows = EvaluationHelper.GetParallelProjectionRowsOrEmpty(chunks, threshold: 4_096);

        Assert.AreEqual(4_096, rows.Count);
        Assert.AreEqual(0, rows[0]);
        Assert.AreEqual(4_095, rows[^1]);
    }

    [TestMethod]
    public void GetParallelProjectionRowsOrEmpty_WithIteratorChunksAboveThreshold_ShouldReturnEmpty()
    {
        var chunks = CreateChunks();

        var rows = EvaluationHelper.GetParallelProjectionRowsOrEmpty(chunks, threshold: 4_096);

        Assert.AreEqual(0, rows.Count);

        static IEnumerable<IReadOnlyList<int>> CreateChunks()
        {
            yield return Enumerable.Range(0, 4_096).ToArray();
        }
    }

    [TestMethod]
    public void ProjectRowsParallel_WhenPredicateRejectsAllRows_ShouldPublishEmptyShard()
    {
        var shards = EvaluationHelper.ProjectRowsParallel<int, TestRow>(
            [1, 2, 3],
            maxDegreeOfParallelism: 4,
            static _ => false,
            static value => new TestRow([value]),
            CancellationToken.None);

        Assert.HasCount(1, shards);
        Assert.AreEqual(0, shards[0].Count);
    }

    [TestMethod]
    public void ProjectRowsParallel_WithoutPredicate_ShouldFilterNullProjectedRows()
    {
        var shards = EvaluationHelper.ProjectRowsParallel<int, TestRow>(
            [1, 2, 3],
            maxDegreeOfParallelism: 4,
            static value => value == 2 ? null! : new TestRow([value]),
            CancellationToken.None);

        var projected = QueryRows
            .FromRowShards(shards)
            .Select(static row => (int)row[0])
            .ToArray();

        CollectionAssert.AreEqual(new[] { 1, 3 }, projected);
    }

    [TestMethod]
    public void ProjectValuesParallel_WhenRowsAreEmpty_ShouldReturnNoShards()
    {
        var shards = TypedProjectionRows.ProjectValuesParallel(
            Array.Empty<int>(),
            maxDegreeOfParallelism: 4,
            static _ => true,
            static value => value,
            CancellationToken.None);

        Assert.AreEqual(0, shards.Length);
    }

    [TestMethod]
    public void ProjectChunkedValuesParallel_ShouldProcessChunksConcurrently()
    {
        using var bothWorkersStarted = new CountdownEvent(2);
        var startedWorkers = 0;

        var projected = TypedProjectionRows.ProjectChunkedValuesParallel(
                CreateRangeChunks(2_048, 0, 2_048),
                maxDegreeOfParallelism: 2,
                static _ => true,
                value =>
                {
                    if (value is 0 or 2_048)
                    {
                        Interlocked.Increment(ref startedWorkers);
                        bothWorkersStarted.Signal();
                        if (!bothWorkersStarted.Wait(TimeSpan.FromSeconds(5)))
                            throw new TimeoutException("Expected two chunk workers to run concurrently.");
                    }

                    return value;
                },
                CancellationToken.None)
            .Take(2)
            .ToArray();

        CollectionAssert.AreEqual(new[] { 0, 1 }, projected);
        Assert.AreEqual(2, startedWorkers);
    }

    [TestMethod]
    public void ProjectChunkedValuesParallel_WhenBelowThreshold_ShouldProjectOnCallerThread()
    {
        var callerThread = Environment.CurrentManagedThreadId;
        var projectionThreads = new ConcurrentQueue<int>();

        var projected = TypedProjectionRows.ProjectChunkedValuesParallel(
                CreateSingleValueChunks(0, 1, 2),
                maxDegreeOfParallelism: 4,
                static _ => true,
                value =>
                {
                    projectionThreads.Enqueue(Environment.CurrentManagedThreadId);
                    return value;
                },
                CancellationToken.None)
            .ToArray();

        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, projected);
        Assert.IsTrue(projectionThreads.All(thread => thread == callerThread));
    }

    [TestMethod]
    public void ProjectChunkedValuesParallel_WhenChunksCompleteOutOfOrder_ShouldPreserveSourceOrder()
    {
        using var secondChunkStarted = new ManualResetEventSlim();

        var projected = TypedProjectionRows.ProjectChunkedValuesParallel(
                CreateRangeChunks(2_048, 0, 2_048),
                maxDegreeOfParallelism: 2,
                static _ => true,
                value =>
                {
                    if (value == 0 && !secondChunkStarted.Wait(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException("Expected second chunk to start before first chunk completes.");

                    if (value == 2_048)
                        secondChunkStarted.Set();

                    return value;
                },
                CancellationToken.None)
            .ToArray();

        CollectionAssert.AreEqual(Enumerable.Range(0, 4_096).ToArray(), projected);
    }

    [TestMethod]
    public void ProjectChunkedValuesParallel_WhenConsumerStopsEarly_ShouldDisposeSourceEnumerator()
    {
        var chunks = new TrackingChunkEnumerable();
        using var enumerator = TypedProjectionRows.ProjectChunkedValuesParallel(
                chunks,
                maxDegreeOfParallelism: 2,
                static _ => true,
                static value => value,
                CancellationToken.None)
            .GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(0, enumerator.Current);

        enumerator.Dispose();

        Assert.IsTrue(chunks.Disposed);
    }

    [TestMethod]
    public void ProjectChunkedValuesParallel_WhenProjectionThrows_ShouldPropagateException()
    {
        var expected = new InvalidOperationException("projection failed");

        var actual = Assert.Throws<InvalidOperationException>(() => TypedProjectionRows
            .ProjectChunkedValuesParallel(
                CreateSingleValueChunks(0, 1),
                maxDegreeOfParallelism: 2,
                static _ => true,
                value => value == 1 ? throw expected : value,
                CancellationToken.None)
            .ToArray());

        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public void ProjectChunkedValuesParallel_WhenSourceThrows_ShouldPropagateException()
    {
        var expected = new InvalidOperationException("source failed");

        var actual = Assert.Throws<InvalidOperationException>(() => TypedProjectionRows
            .ProjectChunkedValuesParallel(
                ThrowingChunks(expected),
                maxDegreeOfParallelism: 2,
                static _ => true,
                static value => value,
                CancellationToken.None)
            .ToArray());

        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public void ProjectChunkedValuesParallel_WhenRowsAreRejectedOrChunksAreEmpty_ShouldReturnAcceptedRows()
    {
        var projected = TypedProjectionRows.ProjectChunkedValuesParallel(
                new IReadOnlyList<int>[]
                {
                    Array.Empty<int>(),
                    new[] { 0, 1, 2 },
                    Array.Empty<int>(),
                    new[] { 3, 4 }
                },
                maxDegreeOfParallelism: 2,
                static value => value % 2 == 0,
                static value => value * 10,
                CancellationToken.None)
            .ToArray();

        CollectionAssert.AreEqual(new[] { 0, 20, 40 }, projected);
    }

    [TestMethod]
    public void ProjectChunkedRowsParallel_ShouldProjectTableRowsInSourceOrder()
    {
        var projected = EvaluationHelper.ProjectChunkedRowsParallel<int, TestRow>(
                CreateSingleValueChunks(0, 1, 2, 3),
                maxDegreeOfParallelism: 2,
                static value => value % 2 == 1,
                static value => new TestRow([value * 10]),
                CancellationToken.None)
            .Select(static row => (int)row[0])
            .ToArray();

        CollectionAssert.AreEqual(new[] { 10, 30 }, projected);
    }

    private static IEnumerable<IReadOnlyList<int>> CreateSingleValueChunks(params int[] values)
    {
        foreach (var value in values)
            yield return [value];
    }

    private static IEnumerable<IReadOnlyList<int>> CreateRangeChunks(int chunkSize, params int[] starts)
    {
        foreach (var start in starts)
            yield return Enumerable.Range(start, chunkSize).ToArray();
    }

    private static IEnumerable<IReadOnlyList<int>> ThrowingChunks(Exception exception)
    {
        yield return [0];
        throw exception;
    }

    private sealed class TrackingChunkEnumerable : IEnumerable<IReadOnlyList<int>>
    {
        public bool Disposed { get; private set; }

        public IEnumerator<IReadOnlyList<int>> GetEnumerator()
        {
            try
            {
                var value = 0;
                while (true)
                    yield return [value++];
            }
            finally
            {
                Disposed = true;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
