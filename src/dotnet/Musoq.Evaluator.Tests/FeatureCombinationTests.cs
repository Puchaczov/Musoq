using System;
using System.Linq;
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

        Assert.AreEqual(2, table.Count, "Expected 2 categories: Senior and Junior");

        var senior = table.Single(r => (string)r.Values[0] == "Senior");
        Assert.AreEqual(3, Convert.ToInt32(senior.Values[1]), "Bob(35), Diana(42), Eve(31)");

        var junior = table.Single(r => (string)r.Values[0] == "Junior");
        Assert.AreEqual(2, Convert.ToInt32(junior.Values[1]), "Alice(25), Charlie(28)");
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

        // Cities: Berlin(2), London(4), Paris(4) from doubled source
        Assert.AreEqual(3, table.Count);

        var berlin = table.Single(r => (string)r.Values[0] == "Berlin");
        Assert.AreEqual(2, Convert.ToInt32(berlin.Values[1]));

        var london = table.Single(r => (string)r.Values[0] == "London");
        Assert.AreEqual(4, Convert.ToInt32(london.Values[1]));

        var paris = table.Single(r => (string)r.Values[0] == "Paris");
        Assert.AreEqual(4, Convert.ToInt32(paris.Values[1]));
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

        // City totals: Berlin=75000, London=105000, Paris=122000. All > 50000. ORDER BY City ASC, TAKE 5.
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Berlin", (string)table[0].Values[0]);
        Assert.AreEqual(75000m, Convert.ToDecimal(table[0].Values[1]));
        Assert.AreEqual("London", (string)table[1].Values[0]);
        Assert.AreEqual(105000m, Convert.ToDecimal(table[1].Values[1]));
        Assert.AreEqual("Paris", (string)table[2].Values[0]);
        Assert.AreEqual(122000m, Convert.ToDecimal(table[2].Values[1]));
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

        // ORDER BY City ASC: Berlin(1), London(2), Paris(2)
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("Berlin", (string)table[0].Values[0]);
        Assert.AreEqual(1, Convert.ToInt32(table[0].Values[1]));
        Assert.AreEqual("London", (string)table[1].Values[0]);
        Assert.AreEqual(2, Convert.ToInt32(table[1].Values[1]));
        Assert.AreEqual("Paris", (string)table[2].Values[0]);
        Assert.AreEqual(2, Convert.ToInt32(table[2].Values[1]));
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

        // Senior(3), Junior(2)
        Assert.AreEqual(2, table.Count);

        var senior = table.Single(r => (string)r.Values[0] == "Senior");
        Assert.AreEqual(3, Convert.ToInt32(senior.Values[1]));

        var junior = table.Single(r => (string)r.Values[0] == "Junior");
        Assert.AreEqual(2, Convert.ToInt32(junior.Values[1]));
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

        // A (Age>30): Bob, Diana, Eve → 3 rows. B (City='London'): Alice, Charlie → 2 rows. UNION ALL: 5.
        Assert.AreEqual(5, table.Count);
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

        // 5 people + 5 order statuses = 10 rows
        Assert.AreEqual(10, table.Count);
        Assert.AreEqual(5, table.Count(r => (string)r.Values[1] == "People"));
        Assert.AreEqual(5, table.Count(r => (string)r.Values[1] == "Orders"));
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

        // London: Alice(250.50+150.00)+Charlie(75.25)=475.75, Paris: Bob(500.00)+Eve(1200.00)=1700.00
        Assert.AreEqual(2, table.Count);

        var london = table.Single(r => (string)r.Values[0] == "London");
        Assert.AreEqual(475.75m, Convert.ToDecimal(london.Values[1]));

        var paris = table.Single(r => (string)r.Values[0] == "Paris");
        Assert.AreEqual(1700.00m, Convert.ToDecimal(paris.Values[1]));
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

        var alice = table.Single(r => (string)r.Values[0] == "Alice");
        Assert.AreEqual(2, Convert.ToInt32(alice.Values[1]), "Alice has orders 100 and 101");

        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        Assert.AreEqual(1, Convert.ToInt32(bob.Values[1]), "Bob has order 102");

        var charlie = table.Single(r => (string)r.Values[0] == "Charlie");
        Assert.AreEqual(1, Convert.ToInt32(charlie.Values[1]), "Charlie has order 103");

        var diana = table.Single(r => (string)r.Values[0] == "Diana");
        Assert.AreEqual(0, Convert.ToInt32(diana.Values[1]), "Diana has no orders");

        var eve = table.Single(r => (string)r.Values[0] == "Eve");
        Assert.AreEqual(1, Convert.ToInt32(eve.Values[1]), "Eve has order 104");
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
        Assert.AreEqual(2, Convert.ToInt32(table[0].Values[0]), "2 nested rows have Info IS NOT NULL");
        Assert.AreEqual(170m, Convert.ToDecimal(table[0].Values[1]), "Sum(Score) = 90 + 80");
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

        // Alpha(1), Beta(1)
        Assert.AreEqual(2, table.Count);

        var alpha = table.Single(r => (string)r.Values[0] == "Alpha");
        Assert.AreEqual(1, Convert.ToInt32(alpha.Values[1]));

        var beta = table.Single(r => (string)r.Values[0] == "Beta");
        Assert.AreEqual(1, Convert.ToInt32(beta.Values[1]));
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
        Assert.AreEqual(3m, Convert.ToDecimal(table[0].Values[0]), "Bob(35), Diana(42), Eve(31) are seniors");
        Assert.AreEqual(2m, Convert.ToDecimal(table[0].Values[1]), "Alice(25), Charlie(28) are juniors");
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
        // Sum(Age) = 25+35+28+42+31 = 161 > 100 → 'Many'
        Assert.AreEqual("Many", (string)table[0].Values[0]);
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

        // Cities ordered: Berlin, London, Paris. QUALIFY keeps rn <= 2 → Berlin(rn=1), London(rn=2)
        Assert.AreEqual(2, table.Count);

        var berlin = table.Single(r => (string)r.Values[0] == "Berlin");
        Assert.AreEqual(1, Convert.ToInt32(berlin.Values[1]), "Diana(42) is the only senior in Berlin");

        var london = table.Single(r => (string)r.Values[0] == "London");
        Assert.AreEqual(0, Convert.ToInt32(london.Values[1]), "No one in London is over 30");
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

        // Non-London: Bob(Paris), Diana(Berlin), Eve(Paris). Ordered by Name: Bob(1), Diana(2), Eve(3). QUALIFY keeps <= 2.
        Assert.AreEqual(2, table.Count);

        var bob = table.Single(r => (string)r.Values[0] == "Bob");
        Assert.AreEqual("Paris", (string)bob.Values[1]);
        Assert.AreEqual(1, Convert.ToInt32(bob.Values[2]));

        var diana = table.Single(r => (string)r.Values[0] == "Diana");
        Assert.AreEqual("Berlin", (string)diana.Values[1]);
        Assert.AreEqual(2, Convert.ToInt32(diana.Values[2]));
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

        // Non-Berlin people: Alice(London,25), Bob(Paris,35), Charlie(London,28), Eve(Paris,31)
        // Grouped by City: London, Paris
        Assert.AreEqual(2, table.Count);

        var london = table.Single(r => (string)r.Values[0] == "London");
        Assert.AreEqual(1, Convert.ToInt32(london.Values[1]), "Only Charlie(28) passes Age > 25 in London");

        var paris = table.Single(r => (string)r.Values[0] == "Paris");
        Assert.AreEqual(2, Convert.ToInt32(paris.Values[1]), "Both Bob(35) and Eve(31) pass Age > 25 in Paris");
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

        Assert.AreEqual(1, table.Count);
        var seniorCount = Convert.ToInt32(table[0].Values[0]);
        var totalCount = Convert.ToInt32(table[0].Values[1]);
        Assert.AreEqual(3, seniorCount, "Bob(35), Diana(42), Eve(31) have Age > 30");
        Assert.AreEqual(5, totalCount, "All 5 people counted");
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

        // 3 cities: Berlin(Diana), London(Alice,Charlie), Paris(Bob,Eve)
        Assert.AreEqual(3, table.Count);

        var london = table.Single(r => (string)r.Values[0] == "London");
        Assert.AreEqual(0, Convert.ToInt32(london.Values[2]), "No seniors in London (Alice=25, Charlie=28)");

        var paris = table.Single(r => (string)r.Values[0] == "Paris");
        Assert.AreEqual(2, Convert.ToInt32(paris.Values[2]), "Bob(35) and Eve(31) are seniors in Paris");

        var berlin = table.Single(r => (string)r.Values[0] == "Berlin");
        Assert.AreEqual(1, Convert.ToInt32(berlin.Values[2]), "Diana(42) is the only senior in Berlin");
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

        // Order by Age: Alice(25), Charlie(28), Eve(31), Bob(35), Diana(42)
        Assert.AreEqual(5, table.Count);
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

        // Age > 30: Bob(35,Paris), Diana(42,Berlin), Eve(31,Paris) → cities = {Paris, Berlin}
        // People in Paris or Berlin: Bob, Diana, Eve
        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(r => new[] { "Bob", "Diana", "Eve" }.Contains((string)r.Values[0])));
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

        // Same IN logic: cities = {Paris, Berlin}, matching people = Bob, Diana, Eve
        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(table.All(r => new[] { "Bob", "Diana", "Eve" }.Contains((string)r.Values[0])));
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

        Assert.AreEqual(1, table.Count);
        var filtered = Convert.ToInt32(table[0].Values[0]);
        var total = Convert.ToInt32(table[0].Values[1]);
        Assert.AreEqual(4, filtered, "Bob(35), Charlie(28), Diana(42), Eve(31) have Age > 25");
        Assert.AreEqual(5, total, "All 5 people counted");
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

        // Alice has Salary=50000, excluded. Remaining: Bob(60000), Charlie(55000), Diana(75000), Eve(62000)
        Assert.AreEqual(4, table.Count);
        Assert.IsFalse(table.Any(r => (string)r.Values[0] == "Alice"), "Alice (Salary=50000) should be excluded");
    }

    #endregion
}
