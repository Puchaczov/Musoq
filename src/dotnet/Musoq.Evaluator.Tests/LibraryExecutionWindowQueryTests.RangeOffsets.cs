using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod] public void ByteRangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<byte>();
    [TestMethod] public void SByteRangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<sbyte>();
    [TestMethod] public void Int16RangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<short>();
    [TestMethod] public void UInt16RangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<ushort>();
    [TestMethod] public void Int32RangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<int>();
    [TestMethod] public void UInt32RangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<uint>();
    [TestMethod] public void Int64RangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<long>();
    [TestMethod] public void UInt64RangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<ulong>();
    [TestMethod] public void DecimalRangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<decimal>();
    [TestMethod] public void SingleRangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<float>();
    [TestMethod] public void DoubleRangeOffsets_ShouldUseConcreteKeys() => AssertRangeOffsets<double>();

    private void AssertRangeOffsets<T>() where T : struct, INumber<T>
    {
        int?[] keys = [null, 1, 2, 2, 4, null, 7];
        int?[] values = [10, 1, 2, null, 4, null, 7];
        var input = keys.Select((key, index) => new RangeOffsetRow<T>(index,
            key.HasValue ? T.CreateChecked(key.Value) : null, values[index], key)).ToArray();
        foreach (var descending in new[] { false, true })
            foreach (var nullsFirst in new[] { false, true })
                AssertRangeOffsetOrder(input, descending, nullsFirst);
    }

    private void AssertRangeOffsetOrder<T>(RangeOffsetRow<T>[] input, bool descending, bool nullsFirst)
        where T : struct, INumber<T>
    {
        var order = "Key " + (descending ? "desc" : "asc") + (nullsFirst ? " nulls first" : " nulls last");
        var query = $"select Position, Sum(Value) over (order by {order} range between 2 preceding and 1 following) as Total, " +
            $"Avg(Value) over (order by {order} range between 2 preceding and 1 following) as Average, " +
            $"Count(Value) over (order by {order} range between 2 preceding and 1 following) as Present " +
            "from #schema.first() order by Position";
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var table = compiled.Run(TestContext.CancellationToken);
        var rows = table.ToArray();
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)), ("Total", typeof(int?)),
            ("Average", typeof(decimal?)), ("Present", typeof(long)));
        Assert.HasCount(input.Length, rows);
        foreach (var source in input)
        {
            var lower = source.ReferenceKey - (descending ? 1 : 2);
            var upper = source.ReferenceKey + (descending ? 2 : 1);
            var frame = input.Where(candidate => source.ReferenceKey.HasValue
                    ? candidate.ReferenceKey.HasValue && candidate.ReferenceKey >= lower && candidate.ReferenceKey <= upper
                    : !candidate.ReferenceKey.HasValue)
                .Where(candidate => candidate.Value.HasValue).Select(candidate => candidate.Value!.Value).ToArray();
            var row = rows[source.Position];
            Assert.AreEqual(source.Position, row.GetValue<int>(0));
            Assert.AreEqual(frame.Length == 0 ? (int?)null : frame.Sum(), row.GetValue<int?>(1));
            Assert.AreEqual(frame.Length == 0 ? (decimal?)null : frame.Average(value => (decimal)value), row.GetValue<decimal?>(2));
            Assert.AreEqual((long)frame.Length, row.GetValue<long>(3));
        }
    }

    [TestMethod]
    public void OffsetRangeKeyReads_ShouldAllocateNoBoxesOrInvocationStorage()
    {
        var (allocated, checksum) = AllocationTestThread.Run(MeasureRangeOffsets);
        Assert.AreEqual(20_000, checksum);
        Assert.AreEqual(0L, allocated);
    }

    private static (long Allocated, int Checksum) MeasureRangeOffsets()
    {
        int[] keys = [1, 2, 3];
        int[] indices = [0, 1, 2];
        _ = ConsumeRangeOffsets(keys, indices);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var checksum = ConsumeRangeOffsets(keys, indices);
        return (GC.GetAllocatedBytesForCurrentThread() - before, checksum);
    }

    private static int ConsumeRangeOffsets(int[] keys, int[] indices)
    {
        var checksum = 0;
        for (var iteration = 0; iteration < 10_000; iteration++)
            checksum += WindowFunctionHelpers.ResolveRangeFrameStart(keys, keys, indices, 0, 3, 1, -1, false, true) +
                WindowFunctionHelpers.ResolveRangeFrameEnd(keys, keys, indices, 0, 3, 1, 1, false, true);
        return checksum;
    }

    public sealed record RangeOffsetRow<T>(int Position, T? Key, int? Value, int? ReferenceKey) where T : struct;
}
