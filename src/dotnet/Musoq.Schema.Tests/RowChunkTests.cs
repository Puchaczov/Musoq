using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;

namespace Musoq.Schema.Tests;

[TestClass]
public sealed class RowChunkTests
{
    [TestMethod]
    public void RowChunk_WhenCreatedOverList_ShouldExposeNonCopyingWindow()
    {
        var rows = new[] { 10, 20, 30, 40 };
        var chunk = new RowChunk<int>(rows, 1, 2);

        rows[2] = 99;

        Assert.AreSame(rows, chunk.Source);
        Assert.AreEqual(1, chunk.Offset);
        Assert.AreEqual(2, chunk.Count);
        CollectionAssert.AreEqual(new[] { 20, 99 }, chunk.ToArray());
    }

    [TestMethod]
    public void RowChunk_WhenIndexIsOutsideWindow_ShouldThrow()
    {
        var chunk = new RowChunk<int>([1, 2, 3], 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = chunk[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = chunk[1]);
    }

    [TestMethod]
    public void RowChunk_WhenWindowIsOutsideSource_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RowChunk<int>([1, 2, 3], -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RowChunk<int>([1, 2, 3], 2, 2));
    }

    [TestMethod]
    public void RowChunk_WhenOffsetAndCountWouldOverflow_ShouldThrow()
    {
        var rows = new LargeReadOnlyList();

        Assert.Throws<ArgumentOutOfRangeException>(() => new RowChunk<int>(rows, int.MaxValue - 1, 10));
    }

    private sealed class LargeReadOnlyList : IReadOnlyList<int>
    {
        public int Count => int.MaxValue;

        public int this[int index] => index;

        public IEnumerator<int> GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
