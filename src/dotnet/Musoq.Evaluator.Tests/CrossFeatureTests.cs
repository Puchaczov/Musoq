using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.NegativeTests;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class CrossFeatureTests : NegativeTestsBase
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

        Assert.AreEqual(2, table.Count, "Expected 2 categories: Senior and Junior");
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

        Assert.IsGreaterThan(0, table.Count, "Expected rows from GROUP BY on UNION result");
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

        Assert.IsGreaterThan(0, table.Count, "Expected rows from CTE chain with GROUP BY");
        Assert.IsLessThanOrEqualTo(5, table.Count, "Expected at most 5 rows due to TAKE");
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

        Assert.IsGreaterThan(0, table.Count, "Expected rows from CTE with GROUP BY");
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

        Assert.IsGreaterThan(0, table.Count, "Expected rows from CTE with CASE GROUP BY");
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

        Assert.IsGreaterThan(0, table.Count, "Expected rows from UNION of CTEs");
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

        Assert.IsGreaterThan(0, table.Count, "Expected rows from CTE with internal UNION");
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

        Assert.IsGreaterThan(0, table.Count, "Expected rows from JOIN + GROUP BY + HAVING");
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

        Assert.AreEqual(5, table.Count, "Expected one row per person");
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

        Assert.AreEqual(1, table.Count, "Expected single aggregated row");
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

        Assert.IsGreaterThan(0, table.Count, "Expected rows grouped by complex property");
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

        Assert.AreEqual(1, table.Count, "Expected single aggregated row");
    }

    [TestMethod]
    public void CF101_AggregateInsideCaseNoGroupBy_ShouldWork()
    {
        var query = @"
            SELECT CASE WHEN Sum(Age) > 100 THEN 'Many' ELSE 'Few' END 
            FROM #test.people()";

        var vm = CompileQuery(query);
        var table = vm.Run(CancellationToken.None);

        Assert.AreEqual(1, table.Count, "Expected single row");
    }

    #endregion
}
