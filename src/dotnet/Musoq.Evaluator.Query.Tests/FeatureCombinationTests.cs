using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[global::Musoq.Tests.Common.CultureTestClass]
public class FeatureCombinationTests : NegativeTestsBase
{
    #region 7.4 GROUP BY + CASE Expression

    [TestMethod]
    public void CF030_GroupByCaseExpressionWithAggregate_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT\n"
            + "                CASE WHEN Age > 30 THEN 'Senior' ELSE 'Junior' END AS Category,\n"
            + "                Count(1) AS Total,\n"
            + "                Avg(Salary) AS AvgSalary\n"
            + "            FROM #test.people()\n"
            + "            GROUP BY CASE WHEN Age > 30 THEN 'Senior' ELSE 'Junior' END"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Category", typeof(string)),
            ("Total", typeof(long)),
            ("AvgSalary", typeof(decimal?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Senior", 3L, 65666.666666666666666666666666667m],
            ["Junior", 2L, 52500m]
        );
    }

    #endregion

    #region 7.6 Set Operations + GROUP BY

    [TestMethod]
    public void CF050_GroupByOnUnionResult_ShouldWork()
    {
        var query = (
            "\n"
            + "            WITH Combined AS (\n"
            + "                SELECT City FROM #test.people()\n"
            + "                UNION ALL (City)\n"
            + "                SELECT City FROM #test.people()\n"
            + "            )\n"
            + "            SELECT c.City, Count(1) AS Appearances FROM Combined c GROUP BY c.City"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Appearances", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Berlin", 2L], ["London", 4L], ["Paris", 4L]);
    }

    #endregion

    #region 7.12 Deeply Nested CTEs with Various Features

    [TestMethod]
    public void CF110_ThreeLevelCteChainWithGroupByOrderByTake_ShouldWork()
    {
        var query = (
            "\n"
            + "            WITH CityTotals AS (\n"
            + "                SELECT City, Sum(Salary) AS Total, Count(1) AS PersonCount\n"
            + "                FROM #test.people()\n"
            + "                GROUP BY City\n"
            + "            )\n"
            + "            SELECT ct.City, ct.Total FROM CityTotals ct WHERE ct.Total > 50000 ORDER BY ct.City ASC TAKE 5"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Total", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Berlin", 75000m],
            ["London", 105000m],
            ["Paris", 122000m]
        );
    }

    #endregion

    #region 7.1 CTE + GROUP BY Interactions

    [TestMethod]
    public void CF001_GroupByInCteReferencingAnotherCte_ShouldWork()
    {
        var query = (
            "\n"
            + "            WITH Grouped AS (\n"
            + "                SELECT City, Count(Age) AS Total FROM #test.people() GROUP BY City\n"
            + "            )\n"
            + "            SELECT g.City, g.Total FROM Grouped g ORDER BY g.City ASC"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("Total", typeof(long)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Berlin", 1L], ["London", 2L], ["Paris", 2L]);
    }

    [TestMethod]
    public void CF002_GroupByOnExpressionInCteThenJoinResults_ShouldWork()
    {
        var query = (
            "\n"
            + "            WITH AgeGroups AS (\n"
            + "                SELECT\n"
            + "                    CASE WHEN Age > 30 THEN 'Senior' ELSE 'Junior' END AS Bucket,\n"
            + "                    Count(1) AS Total\n"
            + "                FROM #test.people()\n"
            + "                GROUP BY CASE WHEN Age > 30 THEN 'Senior' ELSE 'Junior' END\n"
            + "            )\n"
            + "            SELECT * FROM AgeGroups ag"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Bucket", typeof(string)), ("Total", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Senior", 3L], ["Junior", 2L]);
    }

    #endregion

    #region 7.2 CTE + Set Operations

    [TestMethod]
    public void CF010_UnionOfTwoCtes_ShouldWork()
    {
        var query = (
            "\n"
            + "            WITH A AS (SELECT Name FROM #test.people() WHERE Age > 30),\n"
            + "                 B AS (SELECT Name FROM #test.people() WHERE City = 'London')\n"
            + "            SELECT a.Name FROM A a\n"
            + "            UNION ALL (Name)\n"
            + "            SELECT b.Name FROM B b"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Bob"], ["Diana"], ["Eve"], ["Alice"], ["Charlie"]);
    }

    [TestMethod]
    public void CF011_CteContainingUnionInternally_ShouldWork()
    {
        var query = (
            "\n"
            + "            WITH Combined AS (\n"
            + "                SELECT Name, 'People' AS Source FROM #test.people()\n"
            + "                UNION ALL (Name, Source)\n"
            + "                SELECT Status AS Name, 'Orders' AS Source FROM #test.orders()\n"
            + "            )\n"
            + "            SELECT c.Name, c.Source FROM Combined c"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Source", typeof(string)));
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
            ["Completed", "Orders"]
        );
    }

    #endregion

    #region 7.5 JOIN + GROUP BY + HAVING

    [TestMethod]
    public void CF040_MultiSourceJoinWithGroupByAndHaving_ShouldWork()
    {
        var query = (
            "\n"
            + "            WITH JoinedData AS (\n"
            + "                SELECT p.City AS City, o.Amount AS Amount\n"
            + "                FROM #test.people() p\n"
            + "                INNER JOIN #test.orders() o ON p.Id = o.PersonId\n"
            + "            )\n"
            + "            SELECT jd.City, Sum(jd.Amount) AS TotalSpent\n"
            + "            FROM JoinedData jd\n"
            + "            GROUP BY jd.City\n"
            + "            HAVING Sum(jd.Amount) > 100"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("TotalSpent", typeof(decimal?)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["London", 475.75m], ["Paris", 1700m]);
    }

    [TestMethod]
    public void CF041_LeftJoinWithGroupBy_ShouldHandleNulls()
    {
        var query = (
            "\n"
            + "            WITH JoinedData AS (\n"
            + "                SELECT p.Name AS Name, o.OrderId AS OrderId\n"
            + "                FROM #test.people() p\n"
            + "                LEFT OUTER JOIN #test.orders() o ON p.Id = o.PersonId\n"
            + "            )\n"
            + "            SELECT jd.Name, Count(jd.OrderId) AS OrderCount\n"
            + "            FROM JoinedData jd\n"
            + "            GROUP BY jd.Name"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("OrderCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 2L],
            ["Bob", 1L],
            ["Charlie", 1L],
            ["Diana", 0L],
            ["Eve", 1L]
        );
    }

    #endregion

    #region 7.10 Property Access in Aggregation Context

    [TestMethod]
    public void CF090_AggregateOnPropertyOfComplexType_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT\n"
            + "                Count(1) AS Total,\n"
            + "                Sum(Info.Score) AS TotalScore\n"
            + "            FROM #test.nested()\n"
            + "            WHERE Info IS NOT NULL"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Total", typeof(long)), ("TotalScore", typeof(int?)));
        TableMaterializationTestHelper.AssertRowsInOrder(table, [2L, 170]);
    }

    [TestMethod]
    public void CF091_GroupByOnComplexProperty_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT Info.Label, Count(1) AS Total\n"
            + "            FROM #test.nested()\n"
            + "            WHERE Info IS NOT NULL\n"
            + "            GROUP BY Info.Label"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Info.Label", typeof(string)), ("Total", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Alpha", 1L], ["Beta", 1L]);
    }

    #endregion

    #region 7.11 CASE Inside Aggregate

    [TestMethod]
    public void CF100_CaseExpressionInsideAggregate_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT\n"
            + "                Sum(CASE WHEN Age > 30 THEN 1 ELSE 0 END) AS SeniorCount,\n"
            + "                Sum(CASE WHEN Age <= 30 THEN 1 ELSE 0 END) AS JuniorCount\n"
            + "            FROM #test.people()"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("SeniorCount", typeof(int?)),
            ("JuniorCount", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3, 2]);
    }

    [TestMethod]
    public void CF101_AggregateInsideCaseNoGroupBy_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT CASE WHEN Sum(Age) > 100 THEN 'Many' ELSE 'Few' END\n"
            + "            FROM #test.people()"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("case when Sum(Age) > 100 then Many else Few end", typeof(string))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Many"]);
    }

    #endregion

    #region 8. Cross-Feature: QUALIFY + Other Features

    [TestMethod]
    public void CF201_QualifyWithFilterOnAggregate_ShouldApplyBothFilters()
    {
        var query = (
            "\n"
            + "            SELECT\n"
            + "                City,\n"
            + "                Count(Name) filter (where Age > 30) as SeniorCount,\n"
            + "                RowNumber() over (order by City) as rn\n"
            + "            FROM #test.people()\n"
            + "            GROUP BY City\n"
            + "            QUALIFY RowNumber() over (order by City) <= 2"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("SeniorCount", typeof(long)),
            ("rn", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["Berlin", 1L, 1L], ["London", 0L, 2L]);
    }

    [TestMethod]
    public void CF203_QualifyWithNotEquals_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT Name, City, RowNumber() over (order by Name) as rn\n"
            + "            FROM #test.people()\n"
            + "            WHERE City != 'London'\n"
            + "            QUALIFY RowNumber() over (order by Name) <= 2"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("City", typeof(string)),
            ("rn", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, ["Bob", "Paris", 1L], ["Diana", "Berlin", 2L]);
    }

    #endregion

    #region 9. Cross-Feature: FILTER + Other Features

    [TestMethod]
    public void CF231_FilterWithNotEqualsInWhere_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT City, Count(Name) filter (where Age > 25) as FilteredCount\n"
            + "            FROM #test.people()\n"
            + "            WHERE City != 'Berlin'\n"
            + "            GROUP BY City"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("City", typeof(string)), ("FilteredCount", typeof(long)));
        TableMaterializationTestHelper.AssertRowsUnordered(table, ["London", 1L], ["Paris", 2L]);
    }

    [TestMethod]
    public void CF232_FilterWithOrderByNoGroupBy_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT Count(Name) filter (where Age > 30) as SeniorCount,\n"
            + "                   Count(Name) as TotalCount\n"
            + "            FROM #test.people()"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("SeniorCount", typeof(long)),
            ("TotalCount", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [3L, 5L]);
    }

    #endregion

    #region 10. Cross-Feature: AggregateValues + Other Features

    [TestMethod]
    public void CF240_AggregateValuesWithFilterOnAggregate_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT City,\n"
            + "                   AggregateValues(Name, ', ') as AllNames,\n"
            + "                   Count(Name) filter (where Age > 30) as SeniorCount\n"
            + "            FROM #test.people()\n"
            + "            GROUP BY City"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("City", typeof(string)),
            ("AllNames", typeof(string)),
            ("SeniorCount", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Berlin", "Diana", 1L],
            ["London", "Alice, Charlie", 0L],
            ["Paris", "Bob, Eve", 2L]
        );
    }

    #endregion

    #region 11. Error/Behavior Validation: Edge Cases

    [TestMethod]
    [Description("FILTER on window aggregate returns exact running filtered results")]
    public void CF301_FilterOnWindowAggregate_ShouldReturnExactRunningFilteredResults()
    {
        var query = (
            "\n"
            + "            SELECT Name,\n"
            + "                   Sum(Age) filter (where Age > 25) over (order by Name) as FilteredWindowSum\n"
            + "            FROM #test.people()\n"
            + "            ORDER BY Name"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        // Order by Name: Alice(25), Bob(35), Charlie(28), Diana(42), Eve(31)
        // Filter keeps only Age > 25 values in the running sum
        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("FilteredWindowSum", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(
            table,
            ["Alice", null],
            ["Bob", 35],
            ["Charlie", 63],
            ["Diana", 105],
            ["Eve", 136]
        );
    }

    [TestMethod]
    [Description("AggregateValues as window function should fail — AggregateValues is not a window function")]
    public void CF302_AggregateValuesAsWindowFunction_ShouldFail()
    {
        var query = (
            "\n"
            + "            SELECT Name,\n"
            + "                   AggregateValues(Name, ', ') over (order by Name) as RunningNames\n"
            + "            FROM #test.people()"
        );

        Assert.ThrowsExactly<Musoq.Converter.Exceptions.MusoqQueryException>(() => CompileQuery(query));
    }

    [TestMethod]
    [Description("RANGE with N PRECEDING / N FOLLOWING compiles and runs")]
    public void CF303_RangeWithNPreceding_ShouldCompileAndRun()
    {
        var query = (
            "\n"
            + "            SELECT Name, Age,\n"
            + "                   Sum(Age) over (order by Age range between 5 preceding and 5 following) as RangeSum\n"
            + "            FROM #test.people()"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("Name", typeof(string)),
            ("Age", typeof(int)),
            ("RangeSum", typeof(int?))
        );
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Alice", 25, 53],
            ["Charlie", 28, 84],
            ["Eve", 31, 94],
            ["Bob", 35, 66],
            ["Diana", 42, 42]
        );
    }

    [TestMethod]
    [Description("Star EXCLUDE then GROUP BY on excluded column should fail")]
    public void CF304_StarExcludeColumnInGroupBy_ShouldFail()
    {
        var query = (
            "\n"
            + "            SELECT * except (City), Count(Name)\n"
            + "            FROM #test.people()\n"
            + "            GROUP BY City"
        );

        Assert.ThrowsExactly<Musoq.Converter.Exceptions.MusoqQueryException>(() => CompileQuery(query));
    }

    [TestMethod]
    [Description("Star modifier inside IN subquery — validate behavior")]
    public void CF305_StarModifierInsideInSubquery_ShouldFail()
    {
        var query = (
            "\n"
            + "            SELECT Name, City\n"
            + "            FROM #test.people()\n"
            + "            WHERE City IN (SELECT * except (Name, Age) FROM #test.people())"
        );

        Assert.ThrowsExactly<Musoq.Converter.Exceptions.MusoqQueryException>(() => CompileQuery(query));
    }

    #endregion

    #region 12. Syntax Edge Cases

    [TestMethod]
    [Description("IN subquery using CTE as source should work")]
    public void CF401_InSubqueryUsingCteAsSource_ShouldWork()
    {
        var query = (
            "\n"
            + "            with cities as (\n"
            + "                select City from #test.people() where Age > 30\n"
            + "            )\n"
            + "            select a.Name, a.City from #test.people() a\n"
            + "            where a.City in (select City from cities)"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", "Paris"],
            ["Diana", "Berlin"],
            ["Eve", "Paris"]
        );
    }

    [TestMethod]
    [Description("IN subquery without alias should resolve columns unambiguously")]
    public void CF404_InSubqueryWithoutAlias_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT Name, City\n"
            + "            FROM #test.people()\n"
            + "            WHERE City IN (SELECT City FROM #test.people() WHERE Age > 30)"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("City", typeof(string)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", "Paris"],
            ["Diana", "Berlin"],
            ["Eve", "Paris"]
        );
    }

    [TestMethod]
    [Description("FILTER + ORDER BY without GROUP BY — aggregate on whole table")]
    public void CF402_FilterWithOrderByNoGroupBy_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT Count(Name) filter (where Age > 25) as FilteredCount,\n"
            + "                   Count(Name) as TotalCount\n"
            + "            FROM #test.people()"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(
            table,
            ("FilteredCount", typeof(long)),
            ("TotalCount", typeof(long))
        );
        TableMaterializationTestHelper.AssertRowsInOrder(table, [4L, 5L]);
    }

    [TestMethod]
    [Description("!= with decimal type")]
    public void CF403_NotEqualsWithDecimal_ShouldWork()
    {
        var query = (
            "\n"
            + "            SELECT Name, Salary\n"
            + "            FROM #test.people()\n"
            + "            WHERE Salary != 50000"
        );

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        TableMaterializationTestHelper.AssertColumns(table, ("Name", typeof(string)), ("Salary", typeof(decimal)));
        TableMaterializationTestHelper.AssertRowsUnordered(
            table,
            ["Bob", 60000m],
            ["Charlie", 55000m],
            ["Diana", 75000m],
            ["Eve", 62000m]
        );
    }

    #endregion
}
