using System;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod] public void Int32ExtremaFrames_ShouldMatchIndependentSerialResults() => AssertExtremaFrames<int>();
    [TestMethod] public void DecimalExtremaFrames_ShouldMatchIndependentSerialResults() => AssertExtremaFrames<decimal>();
    [TestMethod] public void DoubleExtremaFrames_ShouldMatchIndependentSerialResults() => AssertExtremaFrames<double>();

    private void AssertExtremaFrames<T>() where T : struct, INumber<T>
    {
        const string query = """
            select Position, Min(Value) over (partition by Partition order by Position
                rows between 64 preceding and current row) as Minimum,
                Max(Value) over (partition by Partition order by Position
                    rows between 2 preceding and 3 following) as Maximum,
                Min(Value) over (partition by Partition order by Value
                    range between unbounded preceding and current row) as PeerMinimum
            from #schema.first() order by Position
            """;
        var random = new Random(68019);
        var input = Enumerable.Range(0, 500).Select(position => new ExtremaRow<T>(position, position % 8,
            position % 8 == 0 || random.Next(5) == 0 ? null : T.CreateChecked(random.Next(-30, 31)))).ToArray();
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)),
            ("Minimum", typeof(T?)), ("Maximum", typeof(T?)), ("PeerMinimum", typeof(T?)));
        Assert.HasCount(input.Length, rows);
        foreach (var partition in input.GroupBy(static row => row.Partition))
        {
            var ordered = partition.OrderBy(static row => row.Position).ToArray();
            T? peerMinimum = null;
            foreach (var row in ordered)
            {
                if (row.Value is { } value && (!peerMinimum.HasValue || value < peerMinimum.Value))
                    peerMinimum = value;
            }
            for (var position = 0; position < ordered.Length; position++)
            {
                T? minimum = null;
                T? maximum = null;
                for (var frame = Math.Max(0, position - 64); frame <= position; frame++)
                {
                    if (ordered[frame].Value is { } value && (!minimum.HasValue || value < minimum.Value))
                        minimum = value;
                }
                for (var frame = Math.Max(0, position - 2); frame <= Math.Min(ordered.Length - 1, position + 3); frame++)
                {
                    if (ordered[frame].Value is { } value && (!maximum.HasValue || value > maximum.Value))
                        maximum = value;
                }
                var actual = rows[ordered[position].Position];
                Assert.AreEqual(ordered[position].Position, actual.GetValue<int>(0));
                Assert.AreEqual(minimum, actual.GetValue<T?>(1));
                Assert.AreEqual(maximum, actual.GetValue<T?>(2));
                Assert.AreEqual(ordered[position].Value.HasValue ? peerMinimum : null, actual.GetValue<T?>(3));
            }
        }
    }

    [TestMethod]
    public void DoubleExtremaFrames_WhenValuesContainNaNAndSignedZero_ShouldPreserveSerialComparisons()
    {
        const string query = "select Position, Min(Value) over (order by Position rows between 1 preceding and current row) as Minimum, Max(Value) over (order by Position rows between 1 preceding and current row) as Maximum from #schema.first() order by Position";
        NumericRow<double>[] input = [new(0, -0d), new(1, 0d), new(2, double.NaN), new(3, 1d), new(4, 2d)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)),
            ("Minimum", typeof(double?)), ("Maximum", typeof(double?)));
        Assert.HasCount(5, rows);
        double[] minima = [-0d, -0d, 0d, double.NaN, 1d];
        double[] maxima = [-0d, -0d, 0d, double.NaN, 2d];
        for (var index = 0; index < rows.Length; index++)
        {
            Assert.AreEqual(index, rows[index].GetValue<int>(0));
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(minima[index]),
                BitConverter.DoubleToInt64Bits(rows[index].GetValue<double?>(1)!.Value));
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(maxima[index]),
                BitConverter.DoubleToInt64Bits(rows[index].GetValue<double?>(2)!.Value));
        }
    }

    public sealed record ExtremaRow<T>(int Position, int Partition, T? Value) where T : struct;
}
