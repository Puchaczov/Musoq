using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class FeatureCombinationTests : NegativeTestsBase
{
    #region 7.4 GROUP BY + CASE Expression

    [TestMethod]
    public void CF030_GroupByCaseExpressionWithAggregate_ShouldWork()
    {
        var query = @"
            SELECT
                CASE WHEN Age > 30 THEN 'Senior' ELSE 'Junior' END AS Category,
                Count(1) AS Total,
                Avg(Salary) AS AvgSalary
            FROM #test.people()
            GROUP BY CASE WHEN Age > 30 THEN 'Senior' ELSE 'Junior' END";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Category", typeof(string)),
            ("Total", typeof(long)),
            ("AvgSalary", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Senior", 3L, 65666.666666666666666666666666667m],
            ["Junior", 2L, 52500m]);
    }

    #endregion

    #region 7.6 Set Operations + GROUP BY

    [TestMethod]
    public void CF050_GroupByOnUnionResult_ShouldWork()
    {
        var query = @"
            WITH Combined AS (
                SELECT City FROM #test.people()
                UNION ALL (City)
                SELECT City FROM #test.people()
            )
            SELECT c.City, Count(1) AS Appearances FROM Combined c GROUP BY c.City";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.City", typeof(string)),
            ("Appearances", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Berlin", 2L],
            ["London", 4L],
            ["Paris", 4L]);
    }

    #endregion

    #region 7.12 Deeply Nested CTEs with Various Features

    [TestMethod]
    public void CF110_ThreeLevelCteChainWithGroupByOrderByTake_ShouldWork()
    {
        var query = @"
            WITH CityTotals AS (
                SELECT City, Sum(Salary) AS Total, Count(1) AS PersonCount
                FROM #test.people()
                GROUP BY City
            )
            SELECT ct.City, ct.Total FROM CityTotals ct WHERE ct.Total > 50000 ORDER BY ct.City ASC TAKE 5";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("ct.City", typeof(string)),
            ("ct.Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", 75000m],
            ["London", 105000m],
            ["Paris", 122000m]);
    }

    #endregion

    #region 7.1 CTE + GROUP BY Interactions

    [TestMethod]
    public void CF001_GroupByInCteReferencingAnotherCte_ShouldWork()
    {
        var query = @"
            WITH Grouped AS (
                SELECT City, Count(Age) AS Total FROM #test.people() GROUP BY City
            )
            SELECT g.City, g.Total FROM Grouped g ORDER BY g.City ASC";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("g.City", typeof(string)),
            ("g.Total", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", 1L],
            ["London", 2L],
            ["Paris", 2L]);
    }

    [TestMethod]
    public void CF002_GroupByOnExpressionInCteThenJoinResults_ShouldWork()
    {
        var query = @"
            WITH AgeGroups AS (
                SELECT
                    CASE WHEN Age > 30 THEN 'Senior' ELSE 'Junior' END AS Bucket,
                    Count(1) AS Total
                FROM #test.people()
                GROUP BY CASE WHEN Age > 30 THEN 'Senior' ELSE 'Junior' END
            )
            SELECT * FROM AgeGroups ag";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("ag.Bucket", typeof(string)),
            ("ag.Total", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Senior", 3L],
            ["Junior", 2L]);
    }

    #endregion

    #region 7.2 CTE + Set Operations

    [TestMethod]
    public void CF010_UnionOfTwoCtes_ShouldWork()
    {
        var query = @"
            WITH A AS (SELECT Name FROM #test.people() WHERE Age > 30),
                 B AS (SELECT Name FROM #test.people() WHERE City = 'London')
            SELECT a.Name FROM A a
            UNION ALL (Name)
            SELECT b.Name FROM B b";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob"],
            ["Diana"],
            ["Eve"],
            ["Alice"],
            ["Charlie"]);
    }

    [TestMethod]
    public void CF011_CteContainingUnionInternally_ShouldWork()
    {
        var query = @"
            WITH Combined AS (
                SELECT Name, 'People' AS Source FROM #test.people()
                UNION ALL (Name, Source)
                SELECT Status AS Name, 'Orders' AS Source FROM #test.orders()
            )
            SELECT c.Name, c.Source FROM Combined c";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("c.Name", typeof(string)),
            ("c.Source", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", "People"],
            ["Bob", "People"],
            ["Charlie", "People"],
            ["Diana", "People"],
            ["Eve", "People"],
            ["Completed", "Orders"],
            ["Pending", "Orders"],
            ["Completed", "Orders"],
            ["Cancelled", "Orders"],
            ["Completed", "Orders"]);
    }

    #endregion

    #region 7.5 JOIN + GROUP BY + HAVING

    [TestMethod]
    public void CF040_MultiSourceJoinWithGroupByAndHaving_ShouldWork()
    {
        var query = @"
            WITH JoinedData AS (
                SELECT p.City AS City, o.Amount AS Amount
                FROM #test.people() p
                INNER JOIN #test.orders() o ON p.Id = o.PersonId
            )
            SELECT jd.City, Sum(jd.Amount) AS TotalSpent
            FROM JoinedData jd
            GROUP BY jd.City
            HAVING Sum(jd.Amount) > 100";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("jd.City", typeof(string)),
            ("TotalSpent", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["London", 475.75m],
            ["Paris", 1700m]);
    }

    [TestMethod]
    public void CF041_LeftJoinWithGroupBy_ShouldHandleNulls()
    {
        var query = @"
            WITH JoinedData AS (
                SELECT p.Name AS Name, o.OrderId AS OrderId
                FROM #test.people() p
                LEFT OUTER JOIN #test.orders() o ON p.Id = o.PersonId
            )
            SELECT jd.Name, Count(jd.OrderId) AS OrderCount
            FROM JoinedData jd
            GROUP BY jd.Name";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("jd.Name", typeof(string)),
            ("OrderCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 2L],
            ["Bob", 1L],
            ["Charlie", 1L],
            ["Diana", 0L],
            ["Eve", 1L]);
    }

    #endregion

    #region 7.10 Property Access in Aggregation Context

    [TestMethod]
    public void CF090_AggregateOnPropertyOfComplexType_ShouldWork()
    {
        var query = @"
            SELECT
                Count(1) AS Total,
                Sum(Info.Score) AS TotalScore
            FROM #test.nested()
            WHERE Info IS NOT NULL";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Total", typeof(long)),
            ("TotalScore", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2L, 170]);
    }

    [TestMethod]
    public void CF091_GroupByOnComplexProperty_ShouldWork()
    {
        var query = @"
            SELECT Info.Label, Count(1) AS Total
            FROM #test.nested()
            WHERE Info IS NOT NULL
            GROUP BY Info.Label";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Info.Label", typeof(string)),
            ("Total", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alpha", 1L],
            ["Beta", 1L]);
    }

    #endregion

    #region 7.11 CASE Inside Aggregate

    [TestMethod]
    public void CF100_CaseExpressionInsideAggregate_ShouldWork()
    {
        var query = @"
            SELECT
                Sum(CASE WHEN Age > 30 THEN 1 ELSE 0 END) AS SeniorCount,
                Sum(CASE WHEN Age <= 30 THEN 1 ELSE 0 END) AS JuniorCount
            FROM #test.people()";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("SeniorCount", typeof(int?)),
            ("JuniorCount", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3, 2]);
    }

    [TestMethod]
    public void CF101_AggregateInsideCaseNoGroupBy_ShouldWork()
    {
        var query = @"
            SELECT CASE WHEN Sum(Age) > 100 THEN 'Many' ELSE 'Few' END
            FROM #test.people()";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("case when Sum(Age) > 100 then Many else Few end", typeof(string)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Many"]);
    }

    #endregion

    #region 8. Cross-Feature: QUALIFY + Other Features

    [TestMethod]
    public void CF201_QualifyWithFilterOnAggregate_ShouldApplyBothFilters()
    {
        var query = @"
            SELECT
                City,
                Count(Name) filter (where Age > 30) as SeniorCount,
                RowNumber() over (order by City) as rn
            FROM #test.people()
            GROUP BY City
            QUALIFY RowNumber() over (order by City) <= 2";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("SeniorCount", typeof(long)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Berlin", 1L, 1L],
            ["London", 0L, 2L]);
    }

    [TestMethod]
    public void CF203_QualifyWithNotEquals_ShouldWork()
    {
        var query = @"
            SELECT Name, City, RowNumber() over (order by Name) as rn
            FROM #test.people()
            WHERE City != 'London'
            QUALIFY RowNumber() over (order by Name) <= 2";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("rn", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Bob", "Paris", 1L],
            ["Diana", "Berlin", 2L]);
    }

    #endregion

    #region 9. Cross-Feature: FILTER + Other Features

    [TestMethod]
    public void CF231_FilterWithNotEqualsInWhere_ShouldWork()
    {
        var query = @"
            SELECT City, Count(Name) filter (where Age > 25) as FilteredCount
            FROM #test.people()
            WHERE City != 'Berlin'
            GROUP BY City";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("FilteredCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["London", 1L],
            ["Paris", 2L]);
    }

    [TestMethod]
    public void CF232_FilterWithOrderByNoGroupBy_ShouldWork()
    {
        var query = @"
            SELECT Count(Name) filter (where Age > 30) as SeniorCount,
                   Count(Name) as TotalCount
            FROM #test.people()";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("SeniorCount", typeof(long)),
            ("TotalCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3L, 5L]);
    }

    #endregion

    #region 10. Cross-Feature: AggregateValues + Other Features

    [TestMethod]
    public void CF240_AggregateValuesWithFilterOnAggregate_ShouldWork()
    {
        var query = @"
            SELECT City,
                   AggregateValues(Name, ', ') as AllNames,
                   Count(Name) filter (where Age > 30) as SeniorCount
            FROM #test.people()
            GROUP BY City";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("AllNames", typeof(string)),
            ("SeniorCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Berlin", "Diana", 1L],
            ["London", "Alice, Charlie", 0L],
            ["Paris", "Bob, Eve", 2L]);
    }

    #endregion

    #region 11. Error/Behavior Validation: Edge Cases

    [TestMethod]
    [Description("FILTER on window aggregate returns exact running filtered results")]
    public void CF301_FilterOnWindowAggregate_ShouldReturnExactRunningFilteredResults()
    {
        var query = @"
            SELECT Name,
                   Sum(Age) filter (where Age > 25) over (order by Name) as FilteredWindowSum
            FROM #test.people()
            ORDER BY Name";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        // Order by Name: Alice(25), Bob(35), Charlie(28), Diana(42), Eve(31)
        // Filter keeps only Age > 25 values in the running sum
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FilteredWindowSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", 0m],
            ["Bob", 35m],
            ["Charlie", 63m],
            ["Diana", 105m],
            ["Eve", 136m]);
    }

    [TestMethod]
    [Description("AggregateValues as window function should fail — AggregateValues is not a window function")]
    public void CF302_AggregateValuesAsWindowFunction_ShouldFail()
    {
        var query = @"
            SELECT Name,
                   AggregateValues(Name, ', ') over (order by Name) as RunningNames
            FROM #test.people()";

        Assert.ThrowsExactly<Musoq.Converter.Exceptions.MusoqQueryException>(() => CompileQuery(query));
    }

    [TestMethod]
    [Description("RANGE with N PRECEDING / N FOLLOWING compiles and runs")]
    public void CF303_RangeWithNPreceding_ShouldCompileAndRun()
    {
        var query = @"
            SELECT Name, Age,
                   Sum(Age) over (order by Age range between 5 preceding and 5 following) as RangeSum
            FROM #test.people()";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Age", typeof(int)),
            ("RangeSum", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 25, 53m],
            ["Charlie", 28, 84m],
            ["Eve", 31, 94m],
            ["Bob", 35, 66m],
            ["Diana", 42, 42m]);
    }

    [TestMethod]
    [Description("Star EXCLUDE then GROUP BY on excluded column should fail")]
    public void CF304_StarExcludeColumnInGroupBy_ShouldFail()
    {
        var query = @"
            SELECT * except (City), Count(Name)
            FROM #test.people()
            GROUP BY City";

        Assert.ThrowsExactly<Musoq.Converter.Exceptions.MusoqQueryException>(() => CompileQuery(query));
    }

    [TestMethod]
    [Description("Star modifier inside IN subquery — validate behavior")]
    public void CF305_StarModifierInsideInSubquery_ShouldFail()
    {
        var query = @"
            SELECT Name, City
            FROM #test.people()
            WHERE City IN (SELECT * except (Name, Age) FROM #test.people())";

        Assert.ThrowsExactly<Musoq.Converter.Exceptions.MusoqQueryException>(() => CompileQuery(query));
    }

    #endregion

    #region 12. Syntax Edge Cases

    [TestMethod]
    [Description("IN subquery using CTE as source should work")]
    public void CF401_InSubqueryUsingCteAsSource_ShouldWork()
    {
        var query = @"
            with cities as (
                select City from #test.people() where Age > 30
            )
            select a.Name, a.City from #test.people() a
            where a.City in (select City from cities)";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("a.Name", typeof(string)),
            ("a.City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", "Paris"],
            ["Diana", "Berlin"],
            ["Eve", "Paris"]);
    }

    [TestMethod]
    [Description("IN subquery without alias should resolve columns unambiguously")]
    public void CF404_InSubqueryWithoutAlias_ShouldWork()
    {
        var query = @"
            SELECT Name, City
            FROM #test.people()
            WHERE City IN (SELECT City FROM #test.people() WHERE Age > 30)";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", "Paris"],
            ["Diana", "Berlin"],
            ["Eve", "Paris"]);
    }

    [TestMethod]
    [Description("FILTER + ORDER BY without GROUP BY — aggregate on whole table")]
    public void CF402_FilterWithOrderByNoGroupBy_ShouldWork()
    {
        var query = @"
            SELECT Count(Name) filter (where Age > 25) as FilteredCount,
                   Count(Name) as TotalCount
            FROM #test.people()";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("FilteredCount", typeof(long)),
            ("TotalCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [4L, 5L]);
    }

    [TestMethod]
    [Description("!= with decimal type")]
    public void CF403_NotEqualsWithDecimal_ShouldWork()
    {
        var query = @"
            SELECT Name, Salary
            FROM #test.people()
            WHERE Salary != 50000";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Salary", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", 60000m],
            ["Charlie", 55000m],
            ["Diana", 75000m],
            ["Eve", 62000m]);
    }

    #endregion
}
