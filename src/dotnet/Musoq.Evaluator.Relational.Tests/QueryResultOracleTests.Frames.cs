using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class QueryResultOracleTests
{
    private const string OracleRowsFrameQuery = """
        select e.Id as Id,
               Sum(e.Population) over (
                   partition by e.City order by e.Id rows between 1 preceding and 1 following
               ) as Sliding,
               Sum(e.Population) over (
                   partition by e.City order by e.Id rows between unbounded preceding and current row
               ) as Running,
               Count(e.NullableValue) over (
                   partition by e.City order by e.Id rows between 1 preceding and 1 following
               ) as Present
        from #A.Entities() e
        order by e.Id
        """;

    [TestMethod]
    [DataRow(7, 12)]
    [DataRow(19, 12)]
    [DataRow(43, 12)]
    [DataRow(97, 12)]
    [DataRow(997, 12)]
    [DataRow(2026, 12)]
    [DataRow(7, 0)]
    [DataRow(7, 1)]
    public void RowsFrames_ShouldMatchIndependentPartitionSlices(int seed, int rowCount)
    {
        var input = OracleFrameInputs(seed, rowCount);
        var expected = ExpectedRowsFrames(input);
        using var query = CreateAndRunVirtualMachine(OracleRowsFrameQuery, Sources(input));
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Id", typeof(int)),
            ("Sliding", typeof(decimal?)),
            ("Running", typeof(decimal?)),
            ("Present", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    [TestMethod]
    [DataRow(7, 12, "first")]
    [DataRow(19, 12, "first")]
    [DataRow(43, 12, "first")]
    [DataRow(97, 12, "first")]
    [DataRow(997, 12, "first")]
    [DataRow(2026, 12, "first")]
    [DataRow(7, 12, "last")]
    [DataRow(19, 12, "last")]
    [DataRow(43, 12, "last")]
    [DataRow(97, 12, "last")]
    [DataRow(997, 12, "last")]
    [DataRow(2026, 12, "last")]
    [DataRow(7, 0, "first")]
    [DataRow(7, 1, "last")]
    public void RangeFrames_ShouldMatchIndependentNumericAndNullPeerSets(int seed, int rowCount, string nullPlacement)
    {
        var input = OracleFrameInputs(seed, rowCount);
        var expected = ExpectedRangeFrames(input);
        var sql = $"""
            select e.Id as Id,
                   Sum(e.Population) over (
                       partition by e.City order by e.NullableValue nulls {nullPlacement}
                       range between 2 preceding and current row
                   ) as Bounded,
                   Sum(e.Population) over (
                       partition by e.City order by e.NullableValue nulls {nullPlacement}
                       range between current row and current row
                   ) as Peers,
                   Count(e.NullableValue) over (
                       partition by e.City order by e.NullableValue nulls {nullPlacement}
                       range between current row and current row
                   ) as Present
            from #A.Entities() e
            order by e.Id
            """;
        using var query = CreateAndRunVirtualMachine(sql, Sources(input));
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Id", typeof(int)),
            ("Bounded", typeof(decimal?)),
            ("Peers", typeof(decimal?)),
            ("Present", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    private BasicEntity[] OracleFrameInputs(int seed, int rowCount)
    {
        var rows = Inputs(seed);
        rows[0].City = rows[1].City = rows[2].City = rows[3].City = rows[4].City = "A";
        rows[0].NullableValue = rows[1].NullableValue = null;
        rows[2].NullableValue = rows[3].NullableValue = 1;
        rows[4].NullableValue = 3;
        var selected = rows.Take(rowCount).ToArray();
        TestContext.WriteLine(
            "Frame seed={0}; count={1}; input={2}",
            seed,
            rowCount,
            string.Join(
                ";",
                selected.Select(row =>
                    $"{row.Id},{row.City ?? "<null>"},{row.Population},{row.NullableValue?.ToString() ?? "<null>"}"
                )
            )
        );
        return selected;
    }

    private static object?[][] ExpectedRowsFrames(BasicEntity[] input)
    {
        var expected = new List<object?[]>();
        foreach (var partition in input.GroupBy(row => row.City))
        {
            var ordered = partition.OrderBy(row => row.Id).ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                var first = Math.Max(0, index - 1);
                var last = Math.Min(ordered.Length - 1, index + 1);
                var frame = ordered.Skip(first).Take(last - first + 1).ToArray();
                expected.Add([
                    ordered[index].Id,
                    frame.Sum(row => row.Population),
                    ordered.Take(index + 1).Sum(row => row.Population),
                    frame.LongCount(row => row.NullableValue.HasValue),
                ]);
            }
        }
        return expected.OrderBy(row => (int)row[0]!).ToArray();
    }

    private static object?[][] ExpectedRangeFrames(BasicEntity[] input)
    {
        return input
            .OrderBy(row => row.Id)
            .Select(row =>
            {
                var partition = input.Where(other => other.City == row.City).ToArray();
                var peers = partition.Where(other => other.NullableValue == row.NullableValue).ToArray();
                var bounded = peers;
                if (row.NullableValue.HasValue)
                    bounded = partition
                        .Where(other =>
                            other.NullableValue.HasValue
                            && other.NullableValue.Value >= row.NullableValue.Value - 2
                            && other.NullableValue.Value <= row.NullableValue.Value
                        )
                        .ToArray();
                return new object?[]
                {
                    row.Id,
                    bounded.Sum(other => other.Population),
                    peers.Sum(other => other.Population),
                    peers.LongCount(other => other.NullableValue.HasValue),
                };
            })
            .ToArray();
    }
}
