using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class ChunkEnumeratorTests
{
    [TestMethod]
    public void EnumerateAllTest()
    {
        using var tokenSource = new CancellationTokenSource();
        using var readChunks = new BlockingCollection<IReadOnlyList<string>>
        {
            new List<string>(),
            new List<string> { "a", "ab", "abc", "abcd" },
            new List<string>(),
            new List<string> { "x", "xs" },
            new List<string>()
        };
        readChunks.CompleteAdding();


        var enumerator =
            new ChunkEnumerator<string>(readChunks,
                tokenSource.Token);
        using var disposableEnumerator = enumerator;

        Assert.IsTrue(enumerator.MoveNext());
        CollectionAssert.AreEqual(new[] { "a", "ab", "abc", "abcd" }, enumerator.Current.ToArray());
        Assert.IsTrue(enumerator.MoveNext());
        CollectionAssert.AreEqual(new[] { "x", "xs" }, enumerator.Current.ToArray());
        Assert.IsFalse(enumerator.MoveNext());
    }

    [TestMethod]
    public void MoveNext_WhenTokenCancelled_ShouldNotConsumeBufferedChunks()
    {
        using var tokenSource = new CancellationTokenSource();
        using var readChunks = new BlockingCollection<IReadOnlyList<string>>
        {
            new List<string> { "a" }
        };

        using var enumerator = new ChunkEnumerator<string>(
            readChunks,
            tokenSource.Token);

        tokenSource.Cancel();

        Assert.Throws<OperationCanceledException>(() => enumerator.MoveNext());
        Assert.AreEqual(1, readChunks.Count);
    }

    [TestMethod]
    public void Current_WhenEnumerationHasNotStarted_ShouldThrowInvalidOperationException()
    {
        using var readChunks = new BlockingCollection<IReadOnlyList<string>>();
        using var enumerator = new ChunkEnumerator<string>(
            readChunks,
            CancellationToken.None);

        Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
    }

    [TestMethod]
    public void Current_WhenEnumerationFinished_ShouldThrowInvalidOperationException()
    {
        using var readChunks = new BlockingCollection<IReadOnlyList<string>>
        {
            new List<string> { "a" }
        };
        readChunks.CompleteAdding();
        using var enumerator = new ChunkEnumerator<string>(
            readChunks,
            CancellationToken.None);

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());

        Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
    }

    [TestMethod]
    public void Current_WhenEnumeratorDisposed_ShouldThrowInvalidOperationException()
    {
        using var readChunks = new BlockingCollection<IReadOnlyList<string>>
        {
            new List<string> { "a" }
        };
        using var enumerator = new ChunkEnumerator<string>(
            readChunks,
            CancellationToken.None);

        Assert.IsTrue(enumerator.MoveNext());
        enumerator.Dispose();

        Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
    }
}
