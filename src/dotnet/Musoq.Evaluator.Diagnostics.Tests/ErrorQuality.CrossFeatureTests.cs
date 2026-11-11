using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 2: Cross-Feature Interaction Stress Tests.
///     These combine multiple features (CTE + GROUP BY + JOIN + SET ops + APPLY + TABLE/COUPLE)
///     to find errors at the seams.
///     Covers: E-CROSS category.
/// </summary>
[global::Musoq.Tests.Common.CultureTestClass]
public class ErrorQualityCrossFeatureTests : BasicEntityTestBase
{
    // ============================================================================
    // E-CROSS: Cross-Feature Interaction Stress Tests
    // ============================================================================

    #region E-CROSS: CTE + GROUP BY feeding into JOIN

    [TestMethod]
    public void E_CROSS_01_CteWithGroupByFeedingJoin()
    {

        var analyzer = CreateAnalyzer();
        var query = @"WITH Grouped AS (
    SELECT City, Count(1) AS Cnt
    FROM #A.Entities()
    GROUP BY City
)
SELECT g.City, g.Cnt, b.Name
FROM Grouped g
INNER JOIN #B.Entities() b ON g.City = b.City";

        var result = analyzer.Analyze(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_CROSS_01_CteWithGroupByFeedingJoin));
    }

    #endregion

    #region E-CROSS: Multiple CTEs with set operations

    [TestMethod]
    public void E_CROSS_02_MultipleCTEsWithSetOperation()
    {

        var analyzer = CreateAnalyzer();
        var query = @"WITH A AS (SELECT Name FROM #A.Entities()),
     B AS (SELECT Name FROM #B.Entities())
SELECT a.Name FROM A a
EXCEPT (Name)
SELECT b.Name FROM B b";

        var result = analyzer.Analyze(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_CROSS_02_MultipleCTEsWithSetOperation));
    }

    #endregion

    #region E-CROSS: GROUP BY on CROSS APPLY result

    [TestMethod]
    public void E_CROSS_04_GroupByOnCrossApplyResult()
    {

        var analyzer = CreateAnalyzer();
        var query = @"SELECT b.Name, Count(1) AS Cnt
FROM #A.Entities() a
CROSS APPLY #B.Entities() b
GROUP BY b.Name";

        var result = analyzer.Analyze(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_CROSS_04_GroupByOnCrossApplyResult));
    }

    #endregion

    #region E-CROSS: HAVING with aggregate referencing CROSS APPLY column

    [TestMethod]
    public void E_CROSS_05_HavingWithAggregateOnCrossApply()
    {

        var analyzer = CreateAnalyzer();
        var query = @"SELECT b.Name, Count(1) AS Cnt
FROM #A.Entities() a
CROSS APPLY #B.Entities() b
GROUP BY b.Name
HAVING Count(1) > 0";

        var result = analyzer.Analyze(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_CROSS_05_HavingWithAggregateOnCrossApply));
    }

    #endregion

    #region E-CROSS: ORDER BY on column from CTE after GROUP BY

    [TestMethod]
    public void E_CROSS_07_OrderByOnCteAfterGroupBy()
    {

        var analyzer = CreateAnalyzer();
        var query = @"WITH Source AS (
    SELECT Name, Population FROM #A.Entities()
)
SELECT s.Name, Count(1) AS Cnt
FROM Source s
GROUP BY s.Name
ORDER BY Cnt DESC";

        var result = analyzer.Analyze(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_CROSS_07_OrderByOnCteAfterGroupBy));
    }

    #endregion

    #region E-CROSS: CASE expression inside aggregate

    [TestMethod]
    public void E_CROSS_08_CaseInsideAggregate()
    {

        var analyzer = CreateAnalyzer();
        var query = @"SELECT
    Sum(CASE WHEN Population > 50 THEN Population ELSE 0 END) AS ConditionalSum
FROM #A.Entities()";

        var result = analyzer.Analyze(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_CROSS_08_CaseInsideAggregate));
    }

    #endregion

    #region E-CROSS: TABLE/COUPLE with CTE

    [TestMethod]
    public void E_CROSS_11_TableCoupleWithCte()
    {

        var analyzer = CreateAnalyzer();
        var query = @"table TypedRow { Id: int, Name: string };
couple #A.Entities with table TypedRow as TypedSource;
WITH MyData AS (
    SELECT Id, Name FROM TypedSource()
)
SELECT md.Id, md.Name FROM MyData md";

        var result = analyzer.Analyze(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_CROSS_11_TableCoupleWithCte));
    }

    #endregion

    #region E-CROSS: Deeply nested CTE chain

    [TestMethod]
    public void E_CROSS_12_DeeplyNestedCteChain()
    {

        var analyzer = CreateAnalyzer();
        var query = @"WITH
    L1 AS (SELECT Name, Population FROM #A.Entities()),
    L2 AS (SELECT l.Name, l.Population FROM L1 l WHERE l.Population > 0),
    L3 AS (SELECT l.Name, l.Population FROM L2 l WHERE l.Population > 10),
    L4 AS (SELECT l.Name, l.Population FROM L3 l WHERE l.Population > 50),
    L5 AS (SELECT l.Name, l.Population, l.Population * 2 AS Doubled FROM L4 l)
SELECT l.Name, l.Population, l.Doubled FROM L5 l ORDER BY l.Population";

        var result = analyzer.Analyze(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_CROSS_12_DeeplyNestedCteChain));
    }

    #endregion

    #region E-CROSS: CASE inside JOIN ON condition

    [TestMethod]
    public void E_CROSS_15_CaseInsideJoinOnCondition()
    {

        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
INNER JOIN #B.Entities() b ON
    CASE WHEN a.Population > 50 THEN a.Name ELSE a.City END = b.Name";

        var result = analyzer.Analyze(query);


        Assert.IsTrue(result.IsParsed);
        DiagnosticContractTestAssertions.AssertNoErrors(result, nameof(E_CROSS_15_CaseInsideJoinOnCondition));
    }

    #endregion

    #region Test Setup

    private static BasicSchemaProvider<BasicEntity> CreateSchemaProvider()
    {
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("Warsaw", "Poland", 100) { Money = 1000.50m }] },
            { "#B", [new BasicEntity("Berlin", "Germany", 200) { Money = 2000.75m }] }
        };
        return new BasicSchemaProvider<BasicEntity>(sources);
    }

    private static QueryAnalyzer CreateAnalyzer()
    {
        return new QueryAnalyzer(CreateSchemaProvider());
    }

    #endregion
}
