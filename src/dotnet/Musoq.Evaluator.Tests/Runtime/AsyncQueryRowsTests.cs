using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class AsyncQueryRowsTests
{
    [TestMethod]
    public async Task MaterializeTableAsync_ShouldStreamRowsAndPreserveCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var rows = CreateRows(cancellation.Token);

        var table = await QueryRows.MaterializeTableAsync(
            "result",
            [new Column("Value", typeof(int), 0)],
            rows,
            cancellation.Token);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, table[0][0]);
        Assert.AreEqual(2, table[1][0]);
    }

    [TestMethod]
    public async Task MaterializeChunkedTableAsync_ShouldMaterializeProviderChunks()
    {
        var table = await QueryRows.MaterializeChunkedTableAsync(
            "result",
            [new Column("Value", typeof(int), 0)],
            CreateChunks(),
            CancellationToken.None);

        Assert.AreEqual(3, table.Count);
        Assert.AreEqual(3, table[2][0]);
    }

    private static async IAsyncEnumerable<Row> CreateRows(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();
        yield return new ValueRow(1);
        await Task.Yield();
        yield return new ValueRow(2);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async IAsyncEnumerable<IReadOnlyList<ValueRow>> CreateChunks()
    {
        await Task.Yield();
        yield return [new ValueRow(1), new ValueRow(2)];
        await Task.Yield();
        yield return [new ValueRow(3)];
    }

    private sealed class ValueRow(int value) : Row
    {
        public override int Count => 1;

        public override object this[int columnNumber] => columnNumber == 0
            ? value
            : throw new System.IndexOutOfRangeException();
    }
}
