using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Diagnostics;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class QueryRowsRuntimeTests
{
    [TestMethod]
    public void QueryEnumerable_WhenEnumeratedToEnd_ShouldFireCompletedOnce()
    {
        var completedCount = 0;
        var rows = new QueryEnumerable<int>(
            _ => [1, 2, 3],
            CancellationToken.None,
            onCompleted: () => completedCount++);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows.ToArray());

        Assert.AreEqual(1, completedCount);
    }

    [TestMethod]
    public void QueryEnumerable_WhenEnumeratedTwice_ShouldThrow()
    {
        var rows = new QueryEnumerable<int>(_ => [1], CancellationToken.None);

        CollectionAssert.AreEqual(new[] { 1 }, rows.ToArray());

        Assert.Throws<InvalidOperationException>(() => rows.ToArray());
    }

    [TestMethod]
    public void QueryEnumerable_WhenDisposedEarly_ShouldCancelAndFireDisposed()
    {
        var disposed = false;
        var tokenCancelledInSourceFinally = false;
        var rows = new QueryEnumerable<int>(
            Rows,
            CancellationToken.None,
            onDisposed: () => disposed = true);

        using (var enumerator = rows.GetEnumerator())
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current);
        }

        Assert.IsTrue(disposed);
        Assert.IsTrue(tokenCancelledInSourceFinally);
        return;

        IEnumerable<int> Rows(CancellationToken token)
        {
            try
            {
                yield return 1;
                yield return 2;
            }
            finally
            {
                tokenCancelledInSourceFinally = token.IsCancellationRequested;
            }
        }
    }

    [TestMethod]
    public void QueryEnumerable_WhenSourceThrows_ShouldFireExceptionHook()
    {
        Exception? captured = null;
        var expected = new InvalidOperationException("boom");
        var rows = new QueryEnumerable<int>(
            _ => ThrowingRows(expected),
            CancellationToken.None,
            onException: ex => captured = ex);

        var actual = Assert.Throws<QueryExecutionException>(() => rows.ToArray());

        Assert.AreSame(expected, actual.InnerException);
        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, actual.Envelope!.Code);
        Assert.AreSame(expected, captured);
    }

    [TestMethod]
    public void QueryEnumerable_WhenEnumeratorAcquisitionThrows_ShouldFireExceptionHook()
    {
        Exception? captured = null;
        var expected = new InvalidOperationException("broken enumerator");
        var rows = new QueryEnumerable<int>(
            _ => new ThrowingGetEnumeratorEnumerable<int>(expected),
            CancellationToken.None,
            onException: ex => captured = ex);

        var actual = Assert.Throws<QueryExecutionException>(() => rows.ToArray());

        Assert.AreSame(expected, actual.InnerException);
        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, actual.Envelope!.Code);
        Assert.AreSame(expected, captured);
    }

    [TestMethod]
    public void QueryShardedEnumerable_ShouldExposeCountAndDeterministicOrder()
    {
        var rows = QueryRows.FromShards(
        [
            new ValueShard<int>([1, 2, 99], 2),
            ValueShard<int>.Empty,
            new ValueShard<int>([3, 4], 2)
        ]);

        Assert.AreEqual(4, rows.Count);
        Assert.AreEqual(3, rows.ShardCount);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, rows.ToArray());
    }

    [TestMethod]
    public void QueryRowShardedEnumerable_ShouldExposeCountAndAppendByDeferredBatch()
    {
        var rows = QueryRows.FromRowShards(
        [
            new RowShard<TestRow>([new TestRow("a"), new TestRow("b"), new TestRow("unused")], 2),
            RowShard<TestRow>.Empty,
            new RowShard<TestRow>([new TestRow("c")], 1)
        ]);
        var table = new Table("result", [new Column("Value", typeof(string), 0)]);

        rows.AddTo(table);

        Assert.AreEqual(3, rows.Count);
        Assert.AreEqual(3, rows.ShardCount);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, rows.Select(static row => (string)row[0]).ToArray());
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual("b", table[1][0]);
        Assert.AreEqual("c", table[2][0]);
    }

    [TestMethod]
    public void TypedPostOperationRows_ShouldApplyDistinctOrderingAndProjection()
    {
        var rows = new[]
        {
            new TestRow("b"),
            new TestRow("a"),
            new TestRow("b"),
            new TestRow(null!)
        };

        var result = TypedPostOperationRows.Project(
                TypedPostOperationRows.Order(
                    TypedPostOperationRows.Distinct(rows),
                    [new TypedRowOrderKey<TestRow>(static row => row[0], false, 2)]),
                static row => (string?)row[0])
            .ToArray();

        CollectionAssert.AreEqual(new string?[] { "a", "b", null }, result);
    }

    [TestMethod]
    public void RowOrdering_WhenNullOrderingIsExplicit_ShouldMatchTypedOrdering()
    {
        var rows = new[]
        {
            new TestRow("b", "b"),
            new TestRow(null!, "null"),
            new TestRow("a", "a")
        };

        AssertEquivalentOrdering(rows, 0, false, 2, ["a", "b", "null"]);
    }

    [TestMethod]
    public void RowOrdering_WhenStringsUseOrdinalComparison_ShouldMatchTypedOrdering()
    {
        var rows = new[]
        {
            new TestRow("b", "b"),
            new TestRow("B", "B"),
            new TestRow("a", "a")
        };

        AssertEquivalentOrdering(rows, 0, false, 0, ["B", "a", "b"]);
    }

    [TestMethod]
    public void RowOrdering_WhenDescending_ShouldMatchTypedOrdering()
    {
        var rows = new[]
        {
            new TestRow(1, "one"),
            new TestRow(3, "three"),
            new TestRow(2, "two")
        };

        AssertEquivalentOrdering(rows, 0, true, 0, ["three", "two", "one"]);
    }

    [TestMethod]
    public void RowOrdering_WhenKeysAreDuplicates_ShouldKeepStableOrderInTypedOrdering()
    {
        var rows = new[]
        {
            new TestRow(1, "first"),
            new TestRow(1, "second"),
            new TestRow(0, "zero"),
            new TestRow(1, "third")
        };

        AssertEquivalentOrdering(rows, 0, false, 0, ["zero", "first", "second", "third"]);
    }

    [TestMethod]
    public void RowOrdering_WhenSkipTakeUsesBoundaries_ShouldMatchTypedOrdering()
    {
        var rows = new[]
        {
            new TestRow(4, "four"),
            new TestRow(1, "one"),
            new TestRow(3, "three"),
            new TestRow(2, "two")
        };

        var tableRows = OrderTableRows(rows, 0, false, 0).Select(static row => row[1]).ToArray();
        var typedRows = OrderTypedRows(rows, 0, false, 0).Select(static row => row[1]).ToArray();

        CollectionAssert.AreEqual(new object[] { "two", "three" }, tableRows.Skip(1).Take(2).ToArray());
        CollectionAssert.AreEqual(tableRows.Skip(1).Take(2).ToArray(), typedRows.Skip(1).Take(2).ToArray());
        CollectionAssert.AreEqual(Array.Empty<object>(), tableRows.Skip(10).Take(2).ToArray());
        CollectionAssert.AreEqual(Array.Empty<object>(), typedRows.Skip(10).Take(2).ToArray());
        CollectionAssert.AreEqual(Array.Empty<object>(), tableRows.Skip(1).Take(0).ToArray());
        CollectionAssert.AreEqual(Array.Empty<object>(), typedRows.Skip(1).Take(0).ToArray());
    }

    [TestMethod]
    public void RowOrdering_WhenDistinctAndOrderAreCombined_ShouldMatchTypedOrdering()
    {
        var rows = new[]
        {
            new TestRow("b"),
            new TestRow("a"),
            new TestRow("b"),
            new TestRow(null!)
        };

        var tableRows = EvaluationHelper.OrderRows(
                rows.Distinct(),
                [new RowOrderKey(static row => row[0], false, 2)])
            .Select(static row => row[0])
            .ToArray();
        var typedRows = TypedPostOperationRows.Order(
                TypedPostOperationRows.Distinct(rows),
                [new TypedRowOrderKey<TestRow>(static row => row[0], false, 2)])
            .Select(static row => row[0])
            .ToArray();

        CollectionAssert.AreEqual(new object?[] { "a", "b", null }, tableRows);
        CollectionAssert.AreEqual(tableRows, typedRows);
    }

    [TestMethod]
    public void MaterializeTable_WhenRowsAreBatchSource_ShouldUseBatchFastPath()
    {
        var batchRows = new TestBatchRows(
        [
            new RowShard<TestRow>([new TestRow("a"), new TestRow("b")], 2)
        ]);

        var table = QueryRows.MaterializeTable(
            "result",
            [new Column("Value", typeof(string), 0)],
            batchRows);

        Assert.IsTrue(batchRows.AddToCalled);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("a", table[0][0]);
        Assert.AreEqual("b", table[1][0]);
    }

    [TestMethod]
    public void MaterializeTable_WhenRowsAreTableBackedWithMatchingShape_ShouldReturnSourceTable()
    {
        var columns = new[] { new Column("Value", typeof(string), 0) };
        var source = new Table("result", columns);
        source.AddDirect(new TestRow("a"));
        var rows = QueryRows.FromTable<TestRow>(source);

        var table = QueryRows.MaterializeTable("result", columns, rows);

        Assert.AreSame(source, table);
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("a", table[0][0]);
    }

    [TestMethod]
    public void QueryTableEnumerable_AddTo_WhenInnerRowsAreBatchSource_ShouldPreserveBatchFastPath()
    {
        var completed = false;
        var batchRows = new TestBatchRows(
        [
            new RowShard<TestRow>([new TestRow("a"), new TestRow("b")], 2)
        ]);
        var rows = new QueryTableEnumerable<TestRow>(
            _ => batchRows,
            CancellationToken.None,
            onCompleted: () => completed = true);
        var table = new Table("result", [new Column("Value", typeof(string), 0)]);

        rows.AddTo(table);

        Assert.IsTrue(batchRows.AddToCalled);
        Assert.IsTrue(completed);
        Assert.AreEqual(2, table.Count);
        Assert.Throws<InvalidOperationException>(() => rows.ToArray());
    }

    [TestMethod]
    public void QueryTableEnumerable_WhenEnumeratorAcquisitionThrows_ShouldFireExceptionHook()
    {
        Exception? captured = null;
        var expected = new InvalidOperationException("broken row enumerator");
        var rows = new QueryTableEnumerable<TestRow>(
            _ => new ThrowingGetEnumeratorEnumerable<TestRow>(expected),
            CancellationToken.None,
            onException: ex => captured = ex);

        var actual = Assert.Throws<QueryExecutionException>(() => rows.ToArray());

        Assert.AreSame(expected, actual.InnerException);
        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, actual.Envelope!.Code);
        Assert.AreSame(expected, captured);
    }

    [TestMethod]
    public void DeferredTable_WhenCreated_ShouldNotEnumerateRowsUntilFirstAccess()
    {
        var factoryCalls = 0;
        var enumeratedRows = 0;
        var table = QueryRows.DeferredTable(
            "result",
            [new Column("Value", typeof(string), 0)],
            Rows,
            CancellationToken.None);

        Assert.AreEqual(0, factoryCalls);
        Assert.AreEqual(0, enumeratedRows);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(2, enumeratedRows);
        Assert.AreEqual("a", table[0][0]);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(2, enumeratedRows);
        return;

        IEnumerable<TestRow> Rows(CancellationToken token)
        {
            factoryCalls++;
            token.ThrowIfCancellationRequested();
            enumeratedRows++;
            yield return new TestRow("a");
            enumeratedRows++;
            yield return new TestRow("b");
        }
    }

    [TestMethod]
    public void DeferredTable_WhenCancelledBeforeFirstAccess_ShouldThrowAtMaterialization()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var table = QueryRows.DeferredTable(
            "result",
            [new Column("Value", typeof(string), 0)],
            Rows,
            cancellation.Token);

        Assert.Throws<OperationCanceledException>(() => _ = table.Count);
        return;

        static IEnumerable<TestRow> Rows(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            yield return new TestRow("a");
        }
    }

    private static IEnumerable<int> ThrowingRows(Exception exception)
    {
        yield return 1;
        throw exception;
    }

    private sealed class ThrowingGetEnumeratorEnumerable<T>(Exception exception) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            throw exception;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private sealed class TestBatchRows(RowShard<TestRow>[] shards) : ITableRowBatchSource<TestRow>
    {
        public bool AddToCalled { get; private set; }

        public void AddTo(Table table)
        {
            AddToCalled = true;
            table.AddDirectDeferred(shards);
        }

        public IEnumerator<TestRow> GetEnumerator()
        {
            foreach (var shard in shards)
            {
                for (var index = 0; index < shard.Count; index++)
                    yield return shard[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private static void AssertEquivalentOrdering(
        TestRow[] rows,
        int keyIndex,
        bool descending,
        int nullOrdering,
        object[] expectedIds)
    {
        var tableRows = OrderTableRows(rows, keyIndex, descending, nullOrdering)
            .Select(static row => row[1])
            .ToArray();
        var typedRows = OrderTypedRows(rows, keyIndex, descending, nullOrdering)
            .Select(static row => row[1])
            .ToArray();

        CollectionAssert.AreEqual(expectedIds, tableRows);
        CollectionAssert.AreEqual(tableRows, typedRows);
    }

    private static IOrderedEnumerable<TestRow> OrderTypedRows(
        IEnumerable<TestRow> rows,
        int keyIndex,
        bool descending,
        int nullOrdering)
    {
        return TypedPostOperationRows.Order(
            rows,
            [new TypedRowOrderKey<TestRow>(row => row[keyIndex], descending, nullOrdering)]);
    }

    private static IOrderedEnumerable<Row> OrderTableRows(
        IEnumerable<TestRow> rows,
        int keyIndex,
        bool descending,
        int nullOrdering)
    {
        return EvaluationHelper.OrderRows(
            rows,
            [new RowOrderKey(row => row[keyIndex], descending, nullOrdering)]);
    }

    private sealed class TestRow : Row
    {
        private readonly object?[] _values;

        public TestRow(params object?[]? values)
        {
            _values = values ?? [null];
        }

        public override int Count => _values.Length;

        public override object this[int columnNumber] => columnNumber switch
        {
            _ when (uint)columnNumber < (uint)_values.Length => _values[columnNumber]!,
            _ => throw new IndexOutOfRangeException()
        };
    }
}
