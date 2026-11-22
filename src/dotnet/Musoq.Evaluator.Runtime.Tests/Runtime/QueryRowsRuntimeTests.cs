using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Diagnostics;

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
            onCompleted: () => completedCount++
        );

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
        var rows = new QueryEnumerable<int>(Rows, CancellationToken.None, onDisposed: () => disposed = true);

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
            onException: ex => captured = ex
        );

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
            onException: ex => captured = ex
        );

        var actual = Assert.Throws<QueryExecutionException>(() => rows.ToArray());

        Assert.AreSame(expected, actual.InnerException);
        Assert.AreEqual(DiagnosticCode.MQ9002_InternalExecutionError, actual.Envelope!.Code);
        Assert.AreSame(expected, captured);
    }

    [TestMethod]
    public void QueryShardedEnumerable_ShouldExposeCountAndDeterministicOrder()
    {
        var rows = QueryRows.FromShards([
            new ValueShard<int>([1, 2, 99], 2),
            ValueShard<int>.Empty,
            new ValueShard<int>([3, 4], 2),
        ]);

        Assert.AreEqual(4, rows.Count);
        Assert.AreEqual(3, rows.ShardCount);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, rows.ToArray());
    }

    [TestMethod]
    public void QueryRowShardedEnumerable_ShouldExposeCountAndAppendByDeferredBatch()
    {
        var rows = QueryRows.FromRowShards([
            new RowShard<TestRow>([new TestRow("a"), new TestRow("b"), new TestRow("unused")], 2),
            RowShard<TestRow>.Empty,
            new RowShard<TestRow>([new TestRow("c")], 1),
        ]);
        var table = new Table("result", [new Column("Value", typeof(string), 0)]);

        rows.AddTo(table);

        Assert.AreEqual(3, rows.Count);
        Assert.AreEqual(3, rows.ShardCount);
        CollectionAssert.AreEqual(
            new[] { "a", "b", "c" },
            rows.Select(static row => (string)row.ReadValue(0)).ToArray()
        );
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("a", table[0].ReadValue(0));
        Assert.AreEqual("b", table[1].ReadValue(0));
        Assert.AreEqual("c", table[2].ReadValue(0));
    }

    [TestMethod]
    public void TypedPostOperationRows_ShouldApplyDistinctOrderingAndProjection()
    {
        var rows = new[] { new TestRow("b"), new TestRow("a"), new TestRow("b"), new TestRow(null!) };

        var result = TypedPostOperationRows
            .Project(
                TypedPostOperationRows.Order(
                    TypedPostOperationRows.Distinct(rows),
                    [new TypedRowOrderKey<TestRow>(static row => row.ReadValue(0), false, 2)]
                ),
                static row => (string?)row.ReadValue(0)
            )
            .ToArray();

        CollectionAssert.AreEqual(new string?[] { "a", "b", null }, result);
    }

    [TestMethod]
    public void RowOrdering_WhenNullOrderingIsExplicit_ShouldMatchTypedOrdering()
    {
        var rows = new[] { new TestRow("b", "b"), new TestRow(null!, "null"), new TestRow("a", "a") };

        AssertEquivalentOrdering(rows, 0, false, 2, ["a", "b", "null"]);
    }

    [TestMethod]
    public void RowOrdering_WhenStringsUseOrdinalComparison_ShouldMatchTypedOrdering()
    {
        var rows = new[] { new TestRow("b", "b"), new TestRow("B", "B"), new TestRow("a", "a") };

        AssertEquivalentOrdering(rows, 0, false, 0, ["B", "a", "b"]);
    }

    [TestMethod]
    public void RowOrdering_WhenDescending_ShouldMatchTypedOrdering()
    {
        var rows = new[] { new TestRow(1, "one"), new TestRow(3, "three"), new TestRow(2, "two") };

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
            new TestRow(1, "third"),
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
            new TestRow(2, "two"),
        };

        var tableRows = OrderTableRows(rows, 0, false, 0).Select(static row => row.ReadValue(1)).ToArray();
        var typedRows = OrderTypedRows(rows, 0, false, 0).Select(static row => row.ReadValue(1)).ToArray();

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
        var rows = new[] { new TestRow("b"), new TestRow("a"), new TestRow("b"), new TestRow(null!) };

        var tableRows = EvaluationHelper
            .OrderRows(rows.Distinct(), [new RowOrderKey(static row => row.ReadValue(0), false, 2)])
            .Select(static row => row.ReadValue(0))
            .ToArray();
        var typedRows = TypedPostOperationRows
            .Order(
                TypedPostOperationRows.Distinct(rows),
                [new TypedRowOrderKey<TestRow>(static row => row.ReadValue(0), false, 2)]
            )
            .Select(static row => row.ReadValue(0))
            .ToArray();

        CollectionAssert.AreEqual(new object?[] { "a", "b", null }, tableRows);
        CollectionAssert.AreEqual(tableRows, typedRows);
    }

    [TestMethod]
    public void MaterializeTable_WhenRowsAreBatchSource_ShouldUseBatchFastPath()
    {
        var batchRows = new TestBatchRows([new RowShard<TestRow>([new TestRow("a"), new TestRow("b")], 2)]);

        var table = QueryRows.MaterializeTable("result", [new Column("Value", typeof(string), 0)], batchRows);

        Assert.IsTrue(batchRows.AddToCalled);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("a", table[0].ReadValue(0));
        Assert.AreEqual("b", table[1].ReadValue(0));
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
        Assert.AreEqual("a", table[0].ReadValue(0));
    }

    [TestMethod]
    public void QueryTableEnumerable_AddTo_WhenInnerRowsAreBatchSource_ShouldPreserveBatchFastPath()
    {
        var completed = false;
        var batchRows = new TestBatchRows([new RowShard<TestRow>([new TestRow("a"), new TestRow("b")], 2)]);
        var rows = new QueryTableEnumerable<TestRow>(
            _ => batchRows,
            CancellationToken.None,
            onCompleted: () => completed = true
        );
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
            onException: ex => captured = ex
        );

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
            CancellationToken.None
        );

        Assert.AreEqual(0, factoryCalls);
        Assert.AreEqual(0, enumeratedRows);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(2, enumeratedRows);
        Assert.AreEqual("a", table[0].ReadValue(0));

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
    [DataRow(false)]
    [DataRow(true)]
    public void DeferredTable_ShouldReportFactoryAndEnumerationFailuresAtFirstAccess(bool failInFactory)
    {
        var failure = new InvalidOperationException("Source failed");
        using var table = QueryRows.DeferredTable(
            "result",
            [new Column("Value", typeof(string), 0)],
            _ => failInFactory ? throw failure : Rows(),
            CancellationToken.None
        );

        var error = Assert.Throws<QueryExecutionException>(() => _ = table.Count);
        Assert.AreSame(failure, error.InnerException);
        Assert.AreEqual("RowEnumeration", error.ExecutionPhase);

        IEnumerable<TestRow> Rows()
        {
            yield return new TestRow("first");
            throw failure;
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
            cancellation.Token
        );

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
        object[] expectedIds
    )
    {
        var tableRows = OrderTableRows(rows, keyIndex, descending, nullOrdering)
            .Select(static row => row.ReadValue(1))
            .ToArray();
        var typedRows = OrderTypedRows(rows, keyIndex, descending, nullOrdering)
            .Select(static row => row.ReadValue(1))
            .ToArray();

        CollectionAssert.AreEqual(expectedIds, tableRows);
        CollectionAssert.AreEqual(tableRows, typedRows);
    }

    private static IOrderedEnumerable<TestRow> OrderTypedRows(
        IEnumerable<TestRow> rows,
        int keyIndex,
        bool descending,
        int nullOrdering
    )
    {
        return TypedPostOperationRows.Order(
            rows,
            [new TypedRowOrderKey<TestRow>(row => row.ReadValue(keyIndex), descending, nullOrdering)]
        );
    }

    private static IOrderedEnumerable<Row> OrderTableRows(
        IEnumerable<TestRow> rows,
        int keyIndex,
        bool descending,
        int nullOrdering
    )
    {
        return EvaluationHelper.OrderRows(
            rows,
            [new RowOrderKey(row => row.ReadValue(keyIndex), descending, nullOrdering)]
        );
    }

    [TestMethod]
    public void OptionalTableProjection_RespectsCovariantSliceAndCustomChunkOrder()
    {
        object[] items = new string[] { "outside", "first", "skip", "last", "outside" };
        IReadOnlyList<object>[] chunks =
        [
            new Musoq.Schema.DataSources.RowChunk<object>(items, 1, 3),
            new List<object> { "tail" },
        ];
        var rows = TableProjectionRows.ProjectOptionalRowsSerial<object, TestRow>(
            chunks,
            value => Equals(value, "skip") ? null! : new TestRow(value),
            default
        );
        using var table = QueryRows.MaterializeTable("result", [new Column("Value", typeof(string), 0)], rows);
        CollectionAssert.AreEqual(
            new[] { "first", "last", "tail" },
            table.Select(row => (string)row.ReadValue(0)).ToArray()
        );
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void OptionalTableProjection_ObservesCancellationInsideARejectedChunk(bool useArray)
    {
        using var cancellation = new CancellationTokenSource();
        IReadOnlyList<int> chunk = useArray ? new int[8192] : new List<int>(new int[8192]);
        var reads = 0;
        var rows = TableProjectionRows.ProjectOptionalRowsSerial<int, TestRow>(
            new[] { chunk },
            _ =>
            {
                if (++reads == 1)
                    cancellation.Cancel();
                return null!;
            },
            cancellation.Token
        );
        using var table = new Table("result", [new Column("Value", typeof(int), 0)]);
        Assert.Throws<OperationCanceledException>(() => ((ITableRowBatchSource<TestRow>)rows).AddTo(table));
        Assert.IsTrue(reads is > 0 and <= 1024);
        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void ConcurrentAppendsToNewTable_RetainEveryRow()
    {
        using var table = new Table("result", [new Column("Value", typeof(int), 0)]);
        System.Threading.Tasks.Parallel.For(0, 1024, index => table.AddUnchecked(new TestRow(index)));
        CollectionAssert.AreEquivalent(
            Enumerable.Range(0, 1024).ToArray(),
            table.Select(row => (int)row.ReadValue(0)).ToArray()
        );
    }

    [TestMethod]
    public void OneToOneTableProjection_ConsumesLazyChunksInOrder()
    {
        var table = new Table("result", [new Column("Value", typeof(int), 0)]);
        var rows = TableProjectionRows.ProjectRowsSerial<int, TestRow>(Chunks(), value => new TestRow(value), default);
        ((ITableRowBatchSource<TestRow>)rows).AddTo(table);
        Assert.AreEqual(3, table.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, table.Select(row => (int)row.ReadValue(0)).ToArray());

        IEnumerable<IReadOnlyList<int>> Chunks()
        {
            yield return new[] { 1, 2 };
            Assert.AreEqual(2, table.Count);
            yield return Array.Empty<int>();
            yield return new Musoq.Schema.DataSources.RowChunk<int>(new[] { 99, 3, 99 }, 1, 1);
        }
    }

    [TestMethod]
    public void OneToOneTableProjection_PreservesFirstProjectionErrorBeforeAccessingAnotherChunk()
    {
        var table = new Table("result", [new Column("Value", typeof(int), 0)]);
        IReadOnlyList<int>[] chunks = [new[] { 1 }, new UnreadableChunk()];
        var rows = TableProjectionRows.ProjectRowsSerial<int, TestRow>(
            chunks,
            _ => throw new FormatException("project"),
            default
        );
        Assert.Throws<FormatException>(() => ((ITableRowBatchSource<TestRow>)rows).AddTo(table));
        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void OneToOneTableProjection_UsesSingleGuardedSourceReadAndPreservesArraySlices()
    {
        var source = new StoredSource();
        var chunks = Musoq.Schema.DataSources.DataSourceLifecycle.Read(source, "schema", "rows", "r", "r:1");
        using var lease = new System.IO.MemoryStream();
        using var table = QueryRows.DeferredTable<TestRow>(
            "result",
            [new Column("Value", typeof(int), 0)],
            token =>
                TableProjectionRows.ProjectRowsSerial<int, TestRow>(
                    chunks,
                    value =>
                    {
                        Assert.IsTrue(lease.CanRead, "The query lease must remain active throughout projection.");
                        return new TestRow(value);
                    },
                    token
                ),
            default
        );
        Assert.IsTrue(table.TryAttachLifetimeLease(lease));
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, table.Select(row => (int)row.ReadValue(0)).ToArray());
        Assert.IsFalse(lease.CanRead);
        Assert.AreEqual(1, source.Reads);
    }

    private sealed class StoredSource : Musoq.Schema.DataSources.RowSource<int>
    {
        public int Reads { get; private set; }
        public override IEnumerable<IReadOnlyList<int>> Chunks
        {
            get
            {
                Reads++;
                return new IReadOnlyList<int>[]
                {
                    new Musoq.Schema.DataSources.RowChunk<int>(new[] { 1, 2, 3 }, 1, 2),
                    Array.Empty<int>(),
                    new[] { 4 },
                };
            }
        }
    }

    private sealed class UnreadableChunk : IReadOnlyList<int>
    {
        public int Count => throw new InvalidOperationException();
        public int this[int index] => throw new InvalidOperationException();

        public IEnumerator<int> GetEnumerator() => throw new InvalidOperationException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [TestMethod]
    public void SmallOrderedBuffer_PreservesStableTiesAcrossOffsetAndNullKeys()
    {
        (int? Key, int Id)[] input = [(4, 0), (4, 1), (3, 2), (2, 3), (2, 4), (null, 5), (null, 6), (2, 7)];
        var comparer = Comparer<(int? Key, int Id)>.Create((a, b) => Nullable.Compare(a.Key, b.Key));
        var selected = new EvaluationHelper.SmallBoundedTopRecordList<(int? Key, int Id)>(1, 3, comparer);
        foreach (var row in input)
            selected.Add(row);
        CollectionAssert.AreEqual(new[] { 6, 3, 4 }, selected.Select(row => row.Id).ToArray());
    }

    [TestMethod]
    public void SmallOrderedBuffer_WhenEachRecordRanksFirst_PreservesBothRetainedRecords()
    {
        var comparer = Comparer<(decimal Amount, string Name)>.Create(
            (left, right) => right.Amount.CompareTo(left.Amount)
        );
        var selected = new EvaluationHelper.SmallBoundedTopRecordList<(decimal Amount, string Name)>(1, 2, comparer);

        selected.Add((1m, "first"));
        selected.Add((2m, "second"));
        selected.Add((3m, "third"));
        selected.Add((4m, "fourth"));

        CollectionAssert.AreEqual(new[] { (3m, "third"), (2m, "second") }, selected.ToArray());
    }

    [TestMethod]
    [DataRow(new int[] { }, DisplayName = "Empty")]
    [DataRow(new[] { 1 }, DisplayName = "Singleton")]
    [DataRow(new[] { 1, 1, 2, 2, 3 }, DisplayName = "Ordered with ties")]
    [DataRow(new[] { 4, 3, 2, 2, 1 }, DisplayName = "Reversed with ties")]
    [DataRow(new[] { 3, 1, 5, 0, 1, 4 }, DisplayName = "Shuffled with ties")]
    [DataRow(new[] { 2, 1, 2, 3, 4 }, DisplayName = "Inversion at the start")]
    [DataRow(new[] { 1, 1, 3, 2, 4, 5 }, DisplayName = "Inversion in the middle")]
    [DataRow(new[] { 1, 2, 2, 3, 2 }, DisplayName = "Inversion at the end")]
    [DataRow(new[] { 2, 3, 4, 5, 6, 7, 1 }, DisplayName = "A record displaced from start to end")]
    [DataRow(new[] { 2, 1, 4, 3, 6, 5, 8, 7, 10, 9, 12, 11, 14, 13, 16, 15, 18, 17 }, DisplayName = "Many inversions")]
    public void SortRecordsIfUnordered_MatchesSortUsingStableComparer(int[] keys)
    {
        var records = keys.Select((key, ordinal) => (Key: key, Ordinal: ordinal)).ToList();
        var comparer = Comparer<(int Key, int Ordinal)>.Create(
            static (left, right) =>
            {
                var byKey = left.Key.CompareTo(right.Key);
                return byKey != 0 ? byKey : left.Ordinal.CompareTo(right.Ordinal);
            }
        );
        var expected = records.ToList();
        expected.Sort(comparer);

        EvaluationHelper.SortRecordsIfUnordered(records, comparer);

        CollectionAssert.AreEqual(expected, records);
    }

    [TestMethod]
    [DataRow(16)]
    [DataRow(17)]
    public void SortRecordsIfUnordered_WhenOrdered_ChecksEachAdjacentPairOnce(int count)
    {
        var records = Enumerable.Range(0, count).ToList();
        var comparisons = new List<(int, int)>();
        var comparer = Comparer<int>.Create(
            (left, right) =>
            {
                comparisons.Add((left, right));
                return left.CompareTo(right);
            }
        );
        var adjacentPairs = Enumerable.Range(0, count - 1).Select(index => (index, index + 1)).ToArray();

        EvaluationHelper.SortRecordsIfUnordered(records, comparer);

        CollectionAssert.AreEquivalent(adjacentPairs, comparisons);
    }

    [TestMethod]
    [DataRow(0, 2)]
    [DataRow(5, 4)]
    [DataRow(11, 21)]
    public void SmallOrderedBuffer_MatchesStableSortAsInputGrows(int skip, int take)
    {
        var input = Enumerable.Range(0, 75).Select(i => (Key: (i * 7) % 11, Id: i)).ToArray();
        var comparer = Comparer<(int Key, int Id)>.Create((a, b) => b.Key.CompareTo(a.Key));
        foreach (var count in new[] { 0, skip, skip + take - 1, skip + take, input.Length })
        {
            var buffer = new EvaluationHelper.SmallBoundedTopRecordList<(int Key, int Id)>(skip, take, comparer);
            foreach (var row in input.Take(count))
                buffer.Add(row);
            var expected = input.Take(count).OrderByDescending(row => row.Key).Skip(skip).Take(take).ToArray();
            CollectionAssert.AreEqual(expected, buffer.ToArray());
        }
    }

    private sealed class TestRow : Row
    {
        private readonly object?[] _values;

        public TestRow(params object?[]? values)
        {
            _values = values ?? [null];
        }

        public override int Count => _values.Length;

        public override void VisitColumns<TVisitor>(ref TVisitor visitor)
        {
            for (var index = 0; index < _values.Length; index++)
                FixtureColumnVisit.Apply(index, _values[index], ref visitor);
        }

        public override void VisitColumn<TVisitor>(int columnIndex, ref TVisitor visitor) =>
            FixtureColumnVisit.Apply(columnIndex, _values[columnIndex], ref visitor);
    }
}
