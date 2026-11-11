using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public sealed class QueryResultOracleTests : BasicEntityTestBase
{
    [TestMethod]
    [DataRow(7)]
    [DataRow(19)]
    [DataRow(43)]
    [DataRow(97)]
    [DataRow(997)]
    [DataRow(2026)]
    public void ProjectionFilterAndCase_ShouldMatchIndependentRows(int seed)
    {
        var input = Inputs(seed);
        const string sql =
            "select e.Id as Id, case when e.Population = 0 then 'zero' else 'positive' end as Sign from #A.Entities() e where e.Population >= 0 order by e.Id";
        var expected = input
            .Where(row => row.Population >= 0)
            .OrderBy(row => row.Id)
            .Select(row => new object?[] { row.Id, row.Population == 0 ? "zero" : "positive" })
            .ToArray();

        using var query = CreateAndRunVirtualMachine(sql, Sources(input));
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int)), ("Sign", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    [TestMethod]
    [DataRow(7)]
    [DataRow(19)]
    [DataRow(43)]
    [DataRow(97)]
    [DataRow(997)]
    [DataRow(2026)]
    public void GroupedCountAndSum_ShouldMatchIndependentAccumulators(int seed)
    {
        var input = Inputs(seed);
        var expected = new List<object?[]>();
        foreach (var city in input.Select(row => row.City).Distinct())
        {
            long count = 0;
            decimal total = 0;
            foreach (var row in input.Where(row => row.City == city))
            {
                count++;
                total += row.Population;
            }
            expected.Add([city, count, total]);
        }
        using var query = CreateAndRunVirtualMachine(
            "select e.City as City, Count(e.Id) as Total, Sum(e.Population) as Population from #A.Entities() e group by e.City",
            Sources(input)
        );
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("City", typeof(string)),
            ("Total", typeof(long)),
            ("Population", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(result, expected.ToArray());
    }

    [TestMethod]
    [DataRow(7)]
    [DataRow(19)]
    [DataRow(43)]
    [DataRow(97)]
    [DataRow(997)]
    [DataRow(2026)]
    public void LeftJoin_ShouldPreserveEveryMatchAndUnmatchedLeftRow(int seed)
    {
        var left = Inputs(seed);
        var right = RightInputs();
        var expected = new List<object?[]>();
        foreach (var row in left.Where(row => row.City is not null).OrderBy(row => row.Id))
        {
            var matches = right.Where(other => other.City == row.City).OrderBy(other => other.Id).ToArray();
            if (matches.Length == 0)
                expected.Add([row.Id, null]);
            else
                expected.AddRange(matches.Select(other => new object?[] { row.Id, other.Id }));
        }
        using var query = CreateAndRunVirtualMachine(
            "select a.Id as LeftId, b.Id as RightId from #A.Entities() a left join #B.Entities() b on a.City = b.City where a.City is not null order by a.Id, b.Id",
            Sources(left, right)
        );
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("LeftId", typeof(int)), ("RightId", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected.ToArray());
    }

    [TestMethod]
    [DataRow(7)]
    [DataRow(19)]
    [DataRow(43)]
    [DataRow(97)]
    [DataRow(997)]
    [DataRow(2026)]
    public void IntersectByKey_ShouldKeepLeftMultiplicityWithoutMultiplyingByRightDuplicates(int seed)
    {
        var input = Inputs(seed);
        var left = input.Concat([input[0]]).ToArray();
        var right = RightInputs();
        var expected = left.Where(row => row.City is not null && right.Any(other => other.City == row.City))
            .Select(row => new object?[] { row.City, row.Id })
            .ToArray();
        const string sql =
            "select a.City as City, a.Id as Id from #A.Entities() a where a.City is not null intersect (City) select b.City as City, b.Id as Id from #B.Entities() b";
        using var query = CreateAndRunVirtualMachine(sql, Sources(left, right));
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("City", typeof(string)), ("Id", typeof(int)));
        TableMaterializationTestHelper.AssertRowsUnordered(result, expected);
    }

    [TestMethod]
    [DataRow(7)]
    [DataRow(19)]
    [DataRow(43)]
    [DataRow(97)]
    [DataRow(997)]
    [DataRow(2026)]
    public void RowNumber_ShouldRankWithinNullablePartitionsUsingAllOrderKeys(int seed)
    {
        var input = Inputs(seed);
        var expected = input
            .OrderBy(row => row.Id)
            .Select(row =>
                new object?[]
                {
                    row.City,
                    row.Id,
                    1L
                        + input.LongCount(other =>
                            other.City == row.City
                            && (
                                other.Population < row.Population
                                || other.Population == row.Population && other.Id < row.Id
                            )
                        ),
                }
            )
            .ToArray();
        using var query = CreateAndRunVirtualMachine(
            "select e.City as City, e.Id as Id, RowNumber() over (partition by e.City order by e.Population, e.Id) as Rank from #A.Entities() e order by e.Id",
            Sources(input)
        );
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            result,
            ("City", typeof(string)),
            ("Id", typeof(int)),
            ("Rank", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    [TestMethod]
    [DataRow(7)]
    [DataRow(19)]
    [DataRow(43)]
    [DataRow(97)]
    [DataRow(997)]
    [DataRow(2026)]
    public void NullCase_ShouldPreserveNonNullValuesAndReplaceOnlyNulls(int seed)
    {
        var input = Inputs(seed);
        var expected = input
            .OrderBy(row => row.Id)
            .Select(row => new object?[] { row.Id, row.NullableValue ?? -99 })
            .ToArray();
        using var query = CreateAndRunVirtualMachine(
            "select e.Id as Id, case when e.NullableValue is null then -99 else e.NullableValue end as Value from #A.Entities() e order by e.Id",
            Sources(input)
        );
        using var result = query.Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(result, ("Id", typeof(int)), ("Value", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(result, expected);
    }

    [TestMethod]
    [DataRow(7)]
    [DataRow(19)]
    [DataRow(43)]
    [DataRow(97)]
    [DataRow(997)]
    [DataRow(2026)]
    public void DuplicationAndPermutation_ShouldPreserveTheSpecifiedAggregateRelations(int seed)
    {
        var input = Inputs(seed);
        decimal total = 0;
        foreach (var row in input)
            total += row.Population;
        const string sql = "select Count(e.Id) as Total, Sum(e.Population) as Population from #A.Entities() e";
        using var first = Execute(sql, input);
        using var reversed = Execute(sql, input.Reverse().ToArray());
        using var doubled = Execute(sql, input.Concat(input).ToArray());

        TableMaterializationTestHelper.AssertColumns(first, ("Total", typeof(long)), ("Population", typeof(decimal?)));
        TableMaterializationTestHelper.AssertColumns(
            reversed,
            ("Total", typeof(long)),
            ("Population", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertColumns(
            doubled,
            ("Total", typeof(long)),
            ("Population", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(first, [(long)input.Length, total]);
        TableMaterializationTestHelper.AssertRowsInOrder(reversed, [(long)input.Length, total]);
        TableMaterializationTestHelper.AssertRowsInOrder(doubled, [2L * input.Length, 2m * total]);
    }

    private Table Execute(string sql, BasicEntity[] input)
    {
        using var query = CreateAndRunVirtualMachine(sql, Sources(input));
        return TableMaterializationTestHelper.Materialize(query.Run(TestContext.CancellationToken));
    }

    [TestMethod]
    public void EmptyInput_ShouldKeepScalarAggregateDefaultsAndNoGroups()
    {
        using var scalar = CreateAndRunVirtualMachine(
            "select Count(e.Id) as Total, Sum(e.Population) as Population from #A.Entities() e",
            Sources([])
        );
        using var scalarResult = scalar.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            scalarResult,
            ("Total", typeof(long)),
            ("Population", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(scalarResult, [0L, null]);

        using var grouped = CreateAndRunVirtualMachine(
            "select e.City as City, Count(e.Id) as Total, Sum(e.Population) as Population from #A.Entities() e group by e.City",
            Sources([])
        );
        using var groupedResult = grouped.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(
            groupedResult,
            ("City", typeof(string)),
            ("Total", typeof(long)),
            ("Population", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(groupedResult);
    }

    private BasicEntity[] Inputs(int seed)
    {
        var random = new Random(seed);
        string?[] cities = ["A", "B", "C", null];
        var rows = Enumerable
            .Range(0, 12)
            .Select(index => new BasicEntity
            {
                Id = index,
                City = cities[index % cities.Length],
                Population = index < 4 ? index - 2 : random.Next(-3, 4),
                NullableValue = index % 3 == 0 ? null : random.Next(-2, 3),
            })
            .ToArray();
        TestContext.WriteLine(
            "Seed={0}; input={1}",
            seed,
            string.Join(
                ";",
                rows.Select(row =>
                    $"{row.Id},{row.City ?? "<null>"},{row.Population},{row.NullableValue?.ToString() ?? "<null>"}"
                )
            )
        );
        return rows;
    }

    private static BasicEntity[] RightInputs() =>
        [
            new() { Id = 100, City = "A" },
            new() { Id = 101, City = "A" },
            new() { Id = 102, City = "B" },
            new() { Id = 103, City = "D" },
        ];

    private static Dictionary<string, IEnumerable<BasicEntity>> Sources(
        BasicEntity[] left,
        BasicEntity[]? right = null
    ) => new() { ["#A"] = left, ["#B"] = right ?? [] };
}
