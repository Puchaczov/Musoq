using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public sealed partial class QueryResultOracleTests
{
    private const string OracleGroupedPivotQuery = """
        pivot #A.Entities()
        on Month in (null as Missing, 'Jan' as Jan, 'Feb' as Feb)
        using Sum(Money) as Amount, Count(*) as Orders
        group by City
        order by City nulls first
        """;
    private const string OracleGlobalPivotQuery = """
        pivot #A.Entities()
        on Month in (null as Missing, 'Jan' as Jan, 'Feb' as Feb)
        using Sum(Money) as Amount, Count(*) as Orders
        """;
    private const string OraclePivotUnpivotQuery = """
        with totals as (
            pivot #A.Entities()
            on Month in (null as Missing, 'Jan' as Jan, 'Feb' as Feb)
            using Sum(Money) as Amount
            group by City
        )
        unpivot totals t
        on Bucket in (t.Missing as Missing, t.Jan as Jan, t.Feb as Feb)
        using Amount
        keep t.City as City
        """;
    private const string OracleNullableUnpivotQuery = """
        unpivot #A.Entities() e
        on Bucket in (e.NullableValue as Optional, null as Missing)
        using Value
        keep e.Id as Id
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
    public void GroupedPivot_ShouldMatchIndependentBucketAccumulators(int seed, int rowCount)
    {
        var input = OraclePivotInputs(seed, rowCount);
        var expected = input
            .GroupBy(row => row.City)
            .OrderBy(group => group.Key, System.StringComparer.Ordinal)
            .Select(group => PivotOracleMeasures(group.ToArray()).Prepend(group.Key).ToArray())
            .ToArray();
        using var query = CreateAndRunVirtualMachine(OracleGroupedPivotQuery, Sources(input));
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("City", typeof(string)),
            ("Missing_Amount", typeof(decimal?)),
            ("Missing_Orders", typeof(long)),
            ("Jan_Amount", typeof(decimal?)),
            ("Jan_Orders", typeof(long)),
            ("Feb_Amount", typeof(decimal?)),
            ("Feb_Orders", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    [TestMethod]
    [DataRow(7, 12)]
    [DataRow(19, 12)]
    [DataRow(43, 12)]
    [DataRow(97, 12)]
    [DataRow(997, 12)]
    [DataRow(2026, 12)]
    [DataRow(7, 0)]
    [DataRow(7, 1)]
    public void GlobalPivot_ShouldMatchIndependentBucketDefaults(int seed, int rowCount)
    {
        var input = OraclePivotInputs(seed, rowCount);
        var expected = PivotOracleMeasures(input);
        using var query = CreateAndRunVirtualMachine(OracleGlobalPivotQuery, Sources(input));
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Missing_Amount", typeof(decimal?)),
            ("Missing_Orders", typeof(long)),
            ("Jan_Amount", typeof(decimal?)),
            ("Jan_Orders", typeof(long)),
            ("Feb_Amount", typeof(decimal?)),
            ("Feb_Orders", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    [TestMethod]
    [DataRow(7, 12)]
    [DataRow(19, 12)]
    [DataRow(43, 12)]
    [DataRow(97, 12)]
    [DataRow(997, 12)]
    [DataRow(2026, 12)]
    [DataRow(7, 0)]
    [DataRow(7, 1)]
    public void PivotThenUnpivot_ShouldMatchIndependentNullPreservingExpansion(int seed, int rowCount)
    {
        var input = OraclePivotInputs(seed, rowCount);
        var expected = ExpectedPivotExpansion(input);
        using var query = CreateAndRunVirtualMachine(OraclePivotUnpivotQuery, Sources(input));
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("City", typeof(string)),
            ("Bucket", typeof(string)),
            ("Amount", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(result, expected);
    }

    [TestMethod]
    [DataRow(7, 12)]
    [DataRow(19, 12)]
    [DataRow(43, 12)]
    [DataRow(97, 12)]
    [DataRow(997, 12)]
    [DataRow(2026, 12)]
    [DataRow(7, 0)]
    [DataRow(7, 1)]
    public void NullableUnpivot_ShouldPreserveDuplicateInputsAndNullValues(int seed, int rowCount)
    {
        var input = OraclePivotInputs(seed, rowCount);
        var expected = input
            .SelectMany(row => new object?[][] { [row.Id, "Optional", row.NullableValue], [row.Id, "Missing", null] })
            .ToArray();
        using var query = CreateAndRunVirtualMachine(OracleNullableUnpivotQuery, Sources(input));
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("Id", typeof(int)),
            ("Bucket", typeof(string)),
            ("Value", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(result, expected);
    }

    private BasicEntity[] OraclePivotInputs(int seed, int count)
    {
        var rows = Inputs(seed);
        for (var index = 0; index < rows.Length; index++)
        {
            rows[index].Month = (index % 3) switch
            {
                0 => null,
                1 => "Jan",
                _ => "Feb",
            };
            rows[index].Money = rows[index].Population;
        }
        rows[0] = new BasicEntity
        {
            Id = 0,
            City = "OnlyJan",
            Month = "Jan",
            Money = 2m,
            NullableValue = 7,
        };
        rows[1] = new BasicEntity
        {
            Id = 0,
            City = "OnlyJan",
            Month = "Jan",
            Money = 2m,
            NullableValue = 7,
        };
        rows[2].City = "A";
        rows[2].Month = null;
        rows[3].City = "OnlyFeb";
        rows[3].Month = "Feb";
        var selected = rows.Take(count).ToArray();
        TestContext.WriteLine(
            "Pivot seed={0}; count={1}; input={2}",
            seed,
            count,
            string.Join(
                ";",
                selected.Select(row =>
                    $"{row.Id},{row.City ?? "<null>"},{row.Month ?? "<null>"},{row.Money},{row.NullableValue?.ToString() ?? "<null>"}"
                )
            )
        );
        return selected;
    }

    private static object?[] PivotOracleMeasures(BasicEntity[] input)
    {
        var values = new List<object?>();
        foreach (var bucket in new string?[] { null, "Jan", "Feb" })
        {
            var matches = input.Where(row => row.Month == bucket).ToArray();
            decimal? amount = null;
            if (matches.Length > 0)
                amount = matches.Sum(row => row.Money);
            values.Add(amount);
            values.Add(matches.LongLength);
        }
        return values.ToArray();
    }

    private static object?[][] ExpectedPivotExpansion(BasicEntity[] input)
    {
        var expected = new List<object?[]>();
        foreach (var group in input.GroupBy(row => row.City))
        {
            var measures = PivotOracleMeasures(group.ToArray());
            expected.Add([group.Key, "Missing", measures[0]]);
            expected.Add([group.Key, "Jan", measures[2]]);
            expected.Add([group.Key, "Feb", measures[4]]);
        }
        return expected.ToArray();
    }
}
