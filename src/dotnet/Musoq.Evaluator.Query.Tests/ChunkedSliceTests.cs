using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.DataSources;
using MusoqApi = Musoq.Converter.Musoq;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class ChunkedSliceTests
{
    public sealed record Input(int Id);

    public sealed record Output(int Id);

    [TestMethod]
    public void SkipAcrossChunks_StopsAfterTakingFromArraySlice()
    {
        var result = MusoqApi
            .CompileAndRun<Input, Output>("select Id from #A.entities() skip 3 take 2", Chunks())
            .ToArray();
        CollectionAssert.AreEqual(new[] { new Output(3), new Output(4) }, result);

        static IEnumerable<IReadOnlyList<Input>> Chunks()
        {
            yield return new Input[] { new(0), new(1) };
            yield return Array.Empty<Input>();
            yield return new RowChunk<Input>(new Input[] { new(-1), new(2), new(3), new(4) }, 1, 3);
            throw new InvalidOperationException("Query read beyond TAKE.");
        }
    }

    [TestMethod]
    public void SkipReadsCustomIndexersAndCountsOnlyFilteredMatches()
    {
        var observed = new ReadingChunk(0, 1, 2, 3, 4);
        var result = MusoqApi
            .CompileAndRun<Input, Output>("select Id from #A.entities() skip 3 take 1", [observed])
            .Single();
        Assert.AreEqual(3, result.Id);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, observed.Reads);
        var filtered = MusoqApi
            .CompileAndRun<Input, Output>(
                "select Id from #A.entities() where Id >= 2 skip 1 take 1",
                new[] { new Input[] { new(0), new(1), new(2), new(3) } }
            )
            .Single();
        Assert.AreEqual(3, filtered.Id);
    }

    [TestMethod]
    public void SkipAcrossArrays_ObservesCancellationBeforeRequestingAnotherChunk()
    {
        using var cancellation = new System.Threading.CancellationTokenSource();
        Assert.Throws<OperationCanceledException>(() =>
            MusoqApi
                .CompileAndRun<Input, Output>(
                    "select Id from #A.entities() skip 100 take 1",
                    Chunks(),
                    cancellation.Token
                )
                .ToArray()
        );

        IEnumerable<IReadOnlyList<Input>> Chunks()
        {
            yield return new Input[] { new(1) };
            cancellation.Cancel();
            yield return new Input[] { new(2) };
            throw new InvalidOperationException("Read past cancellation.");
        }
    }

    public sealed record CountOutput(long Total);

    public sealed record NullableInput(int Id, int? Value);

    public sealed record KeyCount(int Id, long Total);

    [TestMethod]
    public void CountValueAndGroupedCount_PreserveNullAndGroupSemantics()
    {
        NullableInput[][] rows =
        [
            [new(1, null), new(1, 4)],
            [new(2, null)],
        ];
        var nonNull = MusoqApi
            .CompileAndRun<NullableInput, CountOutput>("select Count(Value) as Total from #A.entities()", rows)
            .Single();
        Assert.AreEqual(1L, nonNull.Total);
        var groups = MusoqApi
            .CompileAndRun<NullableInput, KeyCount>(
                "select Id, Count(*) as Total from #A.entities() group by Id order by Id",
                rows
            )
            .ToArray();
        CollectionAssert.AreEqual(new[] { new KeyCount(1, 2), new KeyCount(2, 1) }, groups);
    }

    [TestMethod]
    public void CountAll_MixedChunksCountsSlicesAndVisitsEveryCustomRow()
    {
        var custom = new ReadingChunk(4, 5);
        IReadOnlyList<Input>[] chunks =
        [
            Array.Empty<Input>(),
            new Input[] { new(1) },
            new RowChunk<Input>(new Input[] { new(-1), new(2), new(3), new(-1) }, 1, 2),
            custom,
        ];
        const string sql = "select Count(*) as Total from #A.entities()";
        Assert.AreEqual(5L, MusoqApi.CompileAndRun<Input, CountOutput>(sql, chunks).Single().Total);
        CollectionAssert.AreEqual(new[] { 0, 1 }, custom.Reads);
        Assert.AreEqual(0L, MusoqApi.CompileAndRun<Input, CountOutput>(sql, [Array.Empty<Input>()]).Single().Total);
        Assert.AreEqual(2L, MusoqApi.CompileAndRun<Input, CountOutput>(sql + " where Id >= 4", chunks).Single().Total);
    }

    private sealed class ReadingChunk(params int[] ids) : IReadOnlyList<Input>
    {
        public List<int> Reads { get; } = [];
        public int Count => ids.Length;
        public Input this[int index]
        {
            get
            {
                Reads.Add(index);
                return new Input(ids[index]);
            }
        }

        public IEnumerator<Input> GetEnumerator() => Enumerable.Range(0, Count).Select(i => this[i]).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
