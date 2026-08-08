using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CountDistinctCompositionTests : BasicEntityTestBase
{
    [TestMethod]
    public void QualifiedDistinctCount_GroupedByQualifiedColumn_ShouldReturnOneRowPerGroup()
    {
        const string query = """
                             select a.City, Count(distinct a.Name) as UniqueNames
                             from #A.Entities() a
                             group by a.City
                             order by a.City
                             """;

        var table = Run(query, CreateCompositionSources());

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("UniqueNames", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 3L],
            ["NY", 2L]);
    }

    [TestMethod]
    public void DistinctCount_KeywordsAndFunctionSyntax_ShouldBeCaseInsensitive()
    {
        const string query = """
                             SELECT A.City, COUNT(DISTINCT A.Name) AS UNIQUENAMES
                             FROM #A.ENTITIES() A
                             GROUP BY A.City
                             ORDER BY A.City
                             """;

        var table = Run(query, CreateCompositionSources());

        Assert.AreEqual(2, table.Columns.Count());
        Assert.AreEqual(typeof(string), table.Columns.ElementAt(0).ColumnType);
        Assert.AreEqual(typeof(long), table.Columns.ElementAt(1).ColumnType);
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 3L],
            ["NY", 2L]);
    }

    [TestMethod]
    public void DistinctCount_WithWhere_ShouldFilterRowsBeforeDistinct()
    {
        const string query = """
                             select a.City, Count(distinct a.Name) as UniqueNames
                             from #A.Entities() a
                             where a.Population >= 200
                             group by a.City
                             order by a.City
                             """;

        var table = Run(query, CreateCompositionSources());

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 3L],
            ["NY", 1L]);
    }

    [TestMethod]
    public void DistinctCount_WithFilterClause_ShouldFilterWithinEachGroup()
    {
        const string query = """
                             select Country,
                                    Count(distinct Name) filter (where Population > 200) as UniqueNames
                             from #A.Entities()
                             group by Country
                             order by Country
                             """;

        var table = Run(query, CreateCompositionSources());

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("UniqueNames", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["CA", 3L],
            ["US", 1L]);
    }

    [TestMethod]
    public void DistinctCount_WithHaving_ShouldFilterGroupsAfterAggregation()
    {
        const string query = """
                             select Country, Count(distinct Name) as UniqueNames
                             from #A.Entities()
                             group by Country
                             having Count(distinct Name) > 2
                             """;

        var table = Run(query, CreateCompositionSources());

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("UniqueNames", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["CA", 3L]);
    }

    [TestMethod]
    public void DistinctCount_OrderByAliasAndEquivalentAggregate_ShouldOrderGroups()
    {
        const string query = """
                             select Country, Count(distinct Name) as UniqueNames
                             from #A.Entities()
                             group by Country
                             order by UniqueNames desc, Count(distinct Name) desc
                             """;

        var table = Run(query, CreateCompositionSources());

        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["CA", 3L],
            ["US", 2L]);
    }

    [TestMethod]
    public void MultipleDistinctAndOrdinaryAggregates_ShouldKeepEachAccumulatorIndependent()
    {
        const string query = """
                             select Country,
                                    Count(distinct Name) as UniqueNames,
                                    Count(Name) as Names,
                                    Count(distinct City) as UniqueCities,
                                    Sum(Population) as TotalPopulation
                             from #A.Entities()
                             group by Country
                             order by Country
                             """;

        var table = Run(query, CreateCompositionSources());

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("UniqueNames", typeof(long)),
            ("Names", typeof(long)),
            ("UniqueCities", typeof(long)),
            ("TotalPopulation", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["CA", 3L, 4L, 1L, 2000m],
            ["US", 2L, 4L, 1L, 1250m]);
    }

    [TestMethod]
    public void DistinctCount_InsideCte_ShouldRemainGroupedByProjectedColumn()
    {
        const string query = """
                             with named as (
                                 select Country, Name from #A.Entities()
                             )
                             select Country, Count(distinct Name) as UniqueNames
                             from named
                             group by Country
                             order by Country
                             """;

        var table = Run(query, CreateCompositionSources());

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Country", typeof(string)),
            ("UniqueNames", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["CA", 3L],
            ["US", 2L]);
    }

    [TestMethod]
    public void DistinctCount_AfterJoin_ShouldNotCountJoinMultiplicity()
    {
        const string query = """
                             select a.City, Count(distinct b.Name) as UniqueNames
                             from #A.Entities() a
                             inner join #B.Entities() b on a.Country = b.Country
                             group by a.City
                             order by a.City
                             """;

        var table = Run(query, CreateJoinSources());

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.City", typeof(string)),
            ("UniqueNames", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 2L],
            ["NY", 2L]);
    }

    [TestMethod]
    public void DistinctCount_InPivotMeasures_ShouldDeduplicateInsideEachBucket()
    {
        const string query = """
                             pivot #A.Entities()
                             on Month in ('Jan' as Jan, 'Feb' as Feb)
                             using Count(distinct Name) as Customers
                             group by City
                             order by City
                             """;

        var table = Run(query, CreateCompositionSources());

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("Jan", typeof(long)),
            ("Feb", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["LA", 2L, 2L],
            ["NY", 2L, 1L]);
    }

    private Table Run(string query, IDictionary<string, IEnumerable<BasicEntity>> sources)
    {
        return CreateAndRunVirtualMachine(query, sources).Run();
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateCompositionSources()
    {
        return CreateSingleSource(
            new BasicEntity { City = "NY", Country = "US", Name = "Alice", Population = 100m, Month = "Jan" },
            new BasicEntity { City = "NY", Country = "US", Name = "Alice", Population = 150m, Month = "Feb" },
            new BasicEntity { City = "NY", Country = "US", Name = "Bob", Population = 250m, Month = "Jan" },
            new BasicEntity { City = "NY", Country = "US", Name = "Bob", Population = 250m, Month = "Jan" },
            new BasicEntity { City = "NY", Country = "US", Name = null, Population = 500m, Month = "Feb" },
            new BasicEntity { City = "LA", Country = "CA", Name = "Carol", Population = 300m, Month = "Jan" },
            new BasicEntity { City = "LA", Country = "CA", Name = "Carol", Population = 350m, Month = "Feb" },
            new BasicEntity { City = "LA", Country = "CA", Name = "Dave", Population = 400m, Month = "Feb" },
            new BasicEntity { City = "LA", Country = "CA", Name = "Eve", Population = 450m, Month = "Jan" },
            new BasicEntity { City = "LA", Country = "CA", Name = null, Population = 500m, Month = "Feb" });
    }

    private static IDictionary<string, IEnumerable<BasicEntity>> CreateJoinSources()
    {
        return new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", CreateCompositionSources()["#A"] },
            {
                "#B", [
                    new BasicEntity { Country = "US", Name = "Alice" },
                    new BasicEntity { Country = "US", Name = "Alice" },
                    new BasicEntity { Country = "US", Name = "Bob" },
                    new BasicEntity { Country = "CA", Name = "Carol" },
                    new BasicEntity { Country = "CA", Name = "Dave" },
                    new BasicEntity { Country = "CA", Name = "Carol" }
                ]
            }
        };
    }
}
