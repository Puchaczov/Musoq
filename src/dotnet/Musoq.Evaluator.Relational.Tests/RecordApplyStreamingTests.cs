using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tests.Schema.Generic;
using Musoq.Plugins.Attributes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class RecordApplyStreamingTests : GenericEntityTestBase
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void OuterApply_RecognizesStreamingBehindEnumerableAndPreservesOrdinality(bool streaming)
    {
        var trace = new List<string>();
        var children = Children(trace, 2);
        var parents = new[]
        {
            new Parent { Children = streaming ? ApplyRows.Stream(children) : children },
            new Parent(),
        };
        using var query = CreateAndRunVirtualMachine(
            "select c.Number, c.Ordinal from #schema.first() p outer apply p.Children c with ordinality",
            parents
        );
        trace.Clear();
        using var result = query.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertRowsInOrder(result, [0, 0], [1, 1], [null, null]);
        string[] expected = streaming
            ? ["yield0", "read0", "yield1", "read1", "dispose"]
            : ["yield0", "yield1", "dispose", "read0", "read1"];
        CollectionAssert.AreEqual(expected, trace);
    }

    [TestMethod]
    public void UnmarkedIterator_ReadsOneCompleteChunkBeforeFirstRowAndDisposesOnEarlyExit()
    {
        var trace = new List<string>();
        using (
            var batches = new RecordApplyRows<Child>(Children(trace, RowChunking.DefaultChunkSize + 1)).GetEnumerator()
        )
        {
            Assert.IsTrue(batches.MoveNext());
            Assert.AreEqual(RowChunking.DefaultChunkSize, trace.Count);
            Assert.AreEqual(RowChunking.DefaultChunkSize, batches.Current.Count);
            Assert.AreEqual(0, batches.Current[0].Number);
        }
        Assert.AreEqual("dispose", trace[^1]);
        Assert.DoesNotContain($"yield{RowChunking.DefaultChunkSize}", trace);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ThrowingProducer_PreservesOptedInAndBufferedFailureBoundaries(bool streaming)
    {
        var trace = new List<string>();
        var rows = Children(trace, 1, fail: true);
        using var batches = new RecordApplyRows<Child>(streaming ? ApplyRows.Stream(rows) : rows).GetEnumerator();
        if (streaming)
        {
            Assert.IsTrue(batches.MoveNext());
            Assert.AreEqual(0, batches.Current[0].Number);
        }
        Assert.ThrowsExactly<InvalidOperationException>(() => batches.MoveNext());
        CollectionAssert.AreEqual(
            streaming ? new[] { "yield0", "read0", "dispose" } : new[] { "yield0", "dispose" },
            trace
        );
    }

    [TestMethod]
    public void CrossApply_CancellationDisposesMarkedProducerBeforeItsSecondRecord()
    {
        using var cancellation = new CancellationTokenSource();
        var trace = new List<string>();
        var marked = ApplyRows.Stream(Children(trace, 2, cancel: cancellation));
        Assert.AreSame(marked, ApplyRows.Stream(marked));
        using var query = CreateAndRunVirtualMachine(
            "select c.Number from #schema.first() p cross apply p.Children c",
            new[] { new Parent { Children = marked } }
        );
        trace.Clear();
        Assert.Throws<OperationCanceledException>(() => _ = query.Run(cancellation.Token).Count);
        CollectionAssert.AreEqual(new[] { "yield0", "dispose" }, trace);
    }

    [TestMethod]
    public void LargeList_ValidatesItsNextViewAfterTheFirstChunkIsConsumed()
    {
        var list = Enumerable.Range(0, RowChunking.DefaultChunkSize + 2).ToList();
        using var batches = new RecordApplyRows<int>(list).GetEnumerator();
        Assert.IsTrue(batches.MoveNext());
        Assert.AreEqual(RowChunking.DefaultChunkSize, batches.Current.Count);
        list.RemoveAt(list.Count - 1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => batches.MoveNext());
    }

    [TestMethod]
    public void DerivedList_PreservesOriginalViewValidation()
    {
        var list = new ShortViewList { 7 };
        using var batches = new RecordApplyRows<int>(list).GetEnumerator();
        var error = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => batches.MoveNext());
        Assert.AreEqual("count", error.ParamName);
    }

    private sealed class ShortViewList : List<int>, IReadOnlyList<int>
    {
        int IReadOnlyCollection<int>.Count => 0;
    }

    [TestMethod]
    public void StreamingBatch_CancelsWhenTheEnclosingLoopSkipsItsFirstIndexCheck()
    {
        using var cancellation = new CancellationTokenSource();
        var trace = new List<string>();
        var source = ApplyRows.Stream(Children(trace, 2, cancel: cancellation));
        using var batches = new RecordApplyRows<Child>(source, cancellation.Token).GetEnumerator();
        Assert.Throws<OperationCanceledException>(() => batches.MoveNext());
        CollectionAssert.AreEqual(new[] { "yield0", "dispose" }, trace);
    }

    private static IEnumerable<Child> Children(
        List<string> trace,
        int count,
        bool fail = false,
        CancellationTokenSource? cancel = null
    )
    {
        try
        {
            for (var i = 0; i < count; i++)
            {
                trace.Add($"yield{i}");
                cancel?.Cancel();
                yield return new Child(i, trace);
            }
            if (fail)
                throw new InvalidOperationException("Producer failed");
        }
        finally
        {
            trace.Add("dispose");
        }
    }

    public sealed class Parent
    {
        [BindablePropertyAsTable]
        public IEnumerable<Child>? Children { get; init; }
    }

    public sealed class Child(int number, List<string> trace)
    {
        public int Number
        {
            get
            {
                trace.Add($"read{number}");
                return number;
            }
        }
    }
}
