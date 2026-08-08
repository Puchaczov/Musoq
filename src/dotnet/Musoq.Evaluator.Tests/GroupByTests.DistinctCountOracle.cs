using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

public partial class GroupByTests
{
    [TestMethod]
    public void CountDistinct_GroupedNullableExpression_ShouldMatchIndependentOracle()
    {
        var query = "select City, Count(distinct NullableValue) as DistinctValues from #A.Entities() group by City";
        var rows = new[]
        {
            new BasicEntity { City = "North", NullableValue = 1 },
            new BasicEntity { City = "North", NullableValue = 1 },
            new BasicEntity { City = "North", NullableValue = 2 },
            new BasicEntity { City = "North", NullableValue = null },
            new BasicEntity { City = "South", NullableValue = 1 },
            new BasicEntity { City = "South", NullableValue = 3 },
            new BasicEntity { City = "South", NullableValue = 3 },
            new BasicEntity { City = null, NullableValue = 4 },
            new BasicEntity { City = null, NullableValue = 4 },
            new BasicEntity { City = null, NullableValue = null }
        };

        var table = CreateAndRunVirtualMachine(query, Sources(rows)).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("DistinctValues", typeof(long)));
        AssertGroupedCountDistinct(table, rows, row => row.City, row => row.NullableValue);
    }

    [TestMethod]
    public void CountDistinct_GroupedDecimalExpression_ShouldMatchIndependentOracle()
    {
        var query = "select Country, Count(distinct Population) as DistinctPopulations from #A.Entities() group by Country";
        var rows = new[]
        {
            new BasicEntity { Country = "US", City = "East", Population = 10m },
            new BasicEntity { Country = "US", City = "West", Population = 10m },
            new BasicEntity { Country = "US", City = "East", Population = 20m },
            new BasicEntity { Country = "US", City = "West", Population = 30m },
            new BasicEntity { Country = "CA", City = "East", Population = 10m },
            new BasicEntity { Country = "CA", City = "East", Population = 20m },
            new BasicEntity { Country = "CA", City = "West", Population = 30m },
            new BasicEntity { Country = null, City = "Unknown", Population = 10m },
            new BasicEntity { Country = null, City = "Unknown", Population = 10m }
        };

        var table = CreateAndRunVirtualMachine(query, Sources(rows)).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("DistinctPopulations", typeof(long)));
        AssertGroupedCountDistinct(table, rows, row => row.Country, row => row.Population);
    }

    [TestMethod]
    public void CountDistinct_GroupedByTwoKeys_ShouldMatchIndependentOracle()
    {
        var query = "select Country, City, Count(distinct Name) as DistinctNames from #A.Entities() group by Country, City";
        var rows = new[]
        {
            new BasicEntity { Country = "US", City = "East", Name = "Alice" },
            new BasicEntity { Country = "US", City = "East", Name = "Alice" },
            new BasicEntity { Country = "US", City = "East", Name = "Bob" },
            new BasicEntity { Country = "US", City = "West", Name = "Alice" },
            new BasicEntity { Country = "CA", City = "East", Name = "Alice" },
            new BasicEntity { Country = "CA", City = "East", Name = "Carol" },
            new BasicEntity { Country = null, City = "Unknown", Name = null },
            new BasicEntity { Country = null, City = "Unknown", Name = "Nobody" },
            new BasicEntity { Country = null, City = "Unknown", Name = "Nobody" }
        };

        var table = CreateAndRunVirtualMachine(query, Sources(rows)).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("City", typeof(string)),
            ("DistinctNames", typeof(long)));
        AssertGroupedCountDistinctByTwoKeys(table, rows, row => row.Country, row => row.City, row => row.Name);
    }

    [TestMethod]
    public void CountDistinct_AllNullValues_ShouldReturnZero()
    {
        var query = "select Count(distinct Name) from #A.Entities()";
        var rows = new[]
        {
            new BasicEntity { Name = null },
            new BasicEntity { Name = null },
            new BasicEntity { Name = null }
        };

        var table = CreateAndRunVirtualMachine(query, Sources(rows)).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0L]);
    }

    [TestMethod]
    public void CountDistinct_EmptyInput_ShouldReturnZero()
    {
        var query = "select Count(distinct Name) from #A.Entities()";
        var rows = Array.Empty<BasicEntity>();

        var table = CreateAndRunVirtualMachine(query, Sources(rows)).Run(TestContext.CancellationToken);

        TableMaterializationTestHelper.AssertColumns(table, ("Count(Name)", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [0L]);
    }

    private static Dictionary<string, IEnumerable<BasicEntity>> Sources(IEnumerable<BasicEntity> rows)
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", rows }
        };
    }

    private static void AssertGroupedCountDistinct(
        Table table,
        IEnumerable<BasicEntity> sourceRows,
        Func<BasicEntity, object?> groupSelector,
        Func<BasicEntity, object?> distinctSelector)
    {
        var expected = sourceRows
            .GroupBy(groupSelector)
            .Select(group => new object?[]
            {
                group.Key,
                group.Select(distinctSelector).Where(static value => value is not null).Distinct().LongCount()
            })
            .ToArray();

        TableMaterializationTestHelper.AssertRowsUnordered(table, expected);
    }

    private static void AssertGroupedCountDistinctByTwoKeys(
        Table table,
        IEnumerable<BasicEntity> sourceRows,
        Func<BasicEntity, object?> firstGroupSelector,
        Func<BasicEntity, object?> secondGroupSelector,
        Func<BasicEntity, object?> distinctSelector)
    {
        var expected = sourceRows
            .GroupBy(row => (First: firstGroupSelector(row), Second: secondGroupSelector(row)))
            .Select(group => new object?[]
            {
                group.Key.First,
                group.Key.Second,
                group.Select(distinctSelector).Where(static value => value is not null).Distinct().LongCount()
            })
            .ToArray();

        TableMaterializationTestHelper.AssertRowsUnordered(table, expected);
    }
}
