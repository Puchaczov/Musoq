using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionWindowQueryTests
{
    [TestMethod]
    public void DecimalBatchFrames_WhenInputsHaveFixedScale_ShouldMatchIndependentSerialResultsAndBits()
    {
        const string query = """
            select Position, Partition, Sum(Value) over (partition by Partition order by Position
                rows between 64 preceding and current row) as Total
            from #schema.first() order by Position
            """;
        var input = Enumerable.Range(0, 500).Select(static position => new BatchDecimalRow(position, position % 8,
            position % 8 == 7 || position % 11 == 0 ? null : new decimal(position % 97 + 1, 0, 0, false, 2))).ToArray();
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Partition", typeof(int)),
            ("Total", typeof(decimal?)));
        Assert.HasCount(500, rows);
        foreach (var partition in input.GroupBy(static row => row.Partition))
        {
            var values = partition.OrderBy(static row => row.Position).ToArray();
            for (var position = 0; position < values.Length; position++)
            {
                decimal? expected = null;
                for (var frame = Math.Max(0, position - 64); frame <= position; frame++)
                {
                    if (values[frame].Value is not { } value)
                        continue;
                    expected = expected.HasValue ? checked(expected.Value + value) : value;
                }
                var row = rows[values[position].Position];
                Assert.AreEqual(values[position].Position, row.GetValue<int>(0));
                Assert.AreEqual(partition.Key, row.GetValue<int>(1));
                var actual = row.GetValue<decimal?>(2);
                Assert.AreEqual(expected, actual);
                if (expected.HasValue)
                    CollectionAssert.AreEqual(decimal.GetBits(expected.Value), decimal.GetBits(actual!.Value));
            }
        }
    }

    [TestMethod]
    public void DecimalBatchFrames_WhenInputsRequireFallback_ShouldPreserveScaleNegativesAndLimits()
    {
        const string query = """
            select Position, Sum(Value) over (order by Position rows between current row and current row) as Total
            from #schema.first() order by Position
            """;
        NumericRow<decimal>[] input = [new(1, 1.00m), new(2, 2.0m), new(3, -3m), new(4, decimal.MaxValue), new(5, null)];
        using var compiled = CreateAndRunVirtualMachine(query, input);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();

        TableMaterializationTestHelper.AssertColumns(result, ("Position", typeof(int)), ("Total", typeof(decimal?)));
        Assert.HasCount(5, rows);
        for (var position = 0; position < input.Length; position++)
        {
            var expected = input[position].Value;
            var actual = rows[position].GetValue<decimal?>(1);
            Assert.AreEqual(position + 1, rows[position].GetValue<int>(0));
            Assert.AreEqual(expected, actual);
            if (expected.HasValue)
                CollectionAssert.AreEqual(decimal.GetBits(expected.Value), decimal.GetBits(actual!.Value));
        }
    }

    public sealed record BatchDecimalRow(int Position, int Partition, decimal? Value);
}
