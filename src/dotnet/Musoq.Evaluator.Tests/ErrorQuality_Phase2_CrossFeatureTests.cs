#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 2: Cross-Feature Interaction Stress Tests.
///     These combine multiple features (CTE + GROUP BY + JOIN + SET ops + APPLY + TABLE/COUPLE)
///     to find errors at the seams.
///     Covers: E-CROSS category.
/// </summary>
[TestClass]
public class ErrorQuality_Phase2_CrossFeatureTests : BasicEntityTestBase
{
    #region Test Setup

    private static ISchemaProvider CreateSchemaProvider()
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

    private static void AssertHasErrorCode(QueryAnalysisResult result, DiagnosticCode expectedCode, string context)
    {
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected error code {expectedCode} ({context}) but query succeeded. IsParsed: {result.IsParsed}");

        if (result.HasErrors)
        {
            var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.IsTrue(
                result.Errors.Any(e => e.Code == expectedCode),
                $"Expected error code {expectedCode} ({context}) but got:\n{errorDetails}");
        }
    }

    private static void AssertHasOneOfErrorCodes(QueryAnalysisResult result, string context,
        params DiagnosticCode[] expectedCodes)
    {
        Assert.IsTrue(result.HasErrors || !result.IsParsed,
            $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but query succeeded");

        if (result.HasErrors)
        {
            var hasExpected = result.Errors.Any(e => expectedCodes.Contains(e.Code));
            if (!hasExpected)
            {
                var errorDetails = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
                Assert.Fail(
                    $"Expected one of [{string.Join(", ", expectedCodes)}] ({context}) but got:\n{errorDetails}");
            }
        }
    }

    private static void AssertNoErrors(QueryAnalysisResult result)
    {
        if (result.HasErrors)
        {
            var errorMessages = string.Join("\n", result.Errors.Select(e => $"  [{e.Code}] {e.Message}"));
            Assert.Fail($"Expected no errors but got:\n{errorMessages}");
        }
    }

    private static void DocumentBehavior(QueryAnalysisResult result, string expectedBehavior, bool shouldHaveErrors)
    {
        if (shouldHaveErrors)
            Assert.IsTrue(result.HasErrors || !result.IsParsed,
                $"Behavior documentation: {expectedBehavior} - but query succeeded");
    }

    #endregion

    // ============================================================================
    // E-CROSS: Cross-Feature Interaction Stress Tests
    // ============================================================================

    #region E-CROSS: CTE + GROUP BY feeding into JOIN

    [TestMethod]
    public void E_CROSS_01_CteWithGroupByFeedingJoin()
    {
        // Arrange — CTE with GROUP BY, then JOIN
        var analyzer = CreateAnalyzer();
        var query = @"WITH Grouped AS (
    SELECT City, Count(1) AS Cnt
    FROM #A.Entities()
    GROUP BY City
)
SELECT g.City, g.Cnt, b.Name
FROM Grouped g
INNER JOIN #B.Entities() b ON g.City = b.City";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should work or produce clear error about column resolution
        // Assert  CTE with GROUP BY feeding INTO JOIN`n        AssertNoErrors(result);
    }

    #endregion

    #region E-CROSS: Multiple CTEs with set operations

    [TestMethod]
    public void E_CROSS_02_MultipleCTEsWithSetOperation()
    {
        // Arrange — Two CTEs, then EXCEPT between them
        var analyzer = CreateAnalyzer();
        var query = @"WITH A AS (SELECT Name FROM #A.Entities()),
     B AS (SELECT Name FROM #B.Entities())
SELECT a.Name FROM A a
EXCEPT (Name)
SELECT b.Name FROM B b";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document: CTE + EXCEPT interaction
        // Assert  multiple CTEs with EXCEPT set operation`n        AssertNoErrors(result);
    }

    #endregion

    #region E-CROSS: GROUP BY on CROSS APPLY result

    [TestMethod]
    public void E_CROSS_04_GroupByOnCrossApplyResult()
    {
        // Arrange — CROSS APPLY followed by GROUP BY
        var analyzer = CreateAnalyzer();
        var query = @"SELECT b.Name, Count(1) AS Cnt
FROM #A.Entities() a
CROSS APPLY #B.Entities() b
GROUP BY b.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document: GROUP BY after CROSS APPLY
        // Assert  GROUP BY on CROSS APPLY result`n        AssertNoErrors(result);
    }

    #endregion

    #region E-CROSS: HAVING with aggregate referencing CROSS APPLY column

    [TestMethod]
    public void E_CROSS_05_HavingWithAggregateOnCrossApply()
    {
        // Arrange — HAVING + CROSS APPLY interaction
        var analyzer = CreateAnalyzer();
        var query = @"SELECT b.Name, Count(1) AS Cnt
FROM #A.Entities() a
CROSS APPLY #B.Entities() b
GROUP BY b.Name
HAVING Count(1) > 0";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document: HAVING after CROSS APPLY + GROUP BY
        // Assert  HAVING with aggregate on CROSS APPLY column`n        AssertNoErrors(result);
    }

    #endregion

    #region E-CROSS: ORDER BY on column from CTE after GROUP BY

    [TestMethod]
    public void E_CROSS_07_OrderByOnCteAfterGroupBy()
    {
        // Arrange — CTE → GROUP BY → ORDER BY
        var analyzer = CreateAnalyzer();
        var query = @"WITH Source AS (
    SELECT Name, Population FROM #A.Entities()
)
SELECT s.Name, Count(1) AS Cnt
FROM Source s
GROUP BY s.Name
ORDER BY Cnt DESC";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document: ORDER BY on aggregate alias from CTE
        // Assert  ORDER BY on aggregate alias from CTE after GROUP BY`n        AssertNoErrors(result);
    }

    #endregion

    #region E-CROSS: CASE expression inside aggregate

    [TestMethod]
    public void E_CROSS_08_CaseInsideAggregate()
    {
        // Arrange — Sum(CASE WHEN ... THEN ... ELSE ... END)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT 
    Sum(CASE WHEN Population > 50 THEN Population ELSE 0 END) AS ConditionalSum
FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — CASE inside aggregate should work
        // Assert  CASE expression inside Sum aggregate`n        AssertNoErrors(result);
    }

    #endregion

    #region E-CROSS: TABLE/COUPLE with CTE

    [TestMethod]
    public void E_CROSS_11_TableCoupleWithCte()
    {
        // Arrange — TABLE/COUPLE definition + CTE
        var analyzer = CreateAnalyzer();
        var query = @"table TypedRow { Id int, Name string };
couple #A.Entities() with table TypedRow as TypedSource;
WITH MyData AS (
    SELECT Id, Name FROM TypedSource()
)
SELECT md.Id, md.Name FROM MyData md";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — TABLE/COUPLE + CTE interaction
        // Assert  TABLE/COUPLE with CTE`n        AssertNoErrors(result);
    }

    #endregion

    #region E-CROSS: Deeply nested CTE chain

    [TestMethod]
    public void E_CROSS_12_DeeplyNestedCteChain()
    {
        // Arrange — 5 levels of CTE chaining
        var analyzer = CreateAnalyzer();
        var query = @"WITH 
    L1 AS (SELECT Name, Population FROM #A.Entities()),
    L2 AS (SELECT l.Name, l.Population FROM L1 l WHERE l.Population > 0),
    L3 AS (SELECT l.Name, l.Population FROM L2 l WHERE l.Population > 10),
    L4 AS (SELECT l.Name, l.Population FROM L3 l WHERE l.Population > 50),
    L5 AS (SELECT l.Name, l.Population, l.Population * 2 AS Doubled FROM L4 l)
SELECT l.Name, l.Population, l.Doubled FROM L5 l ORDER BY l.Population";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Deep CTE chain should work
        // Assert  5-level deep CTE chain`n        AssertNoErrors(result);
    }

    #endregion

    #region E-CROSS: CASE inside JOIN ON condition

    [TestMethod]
    public void E_CROSS_15_CaseInsideJoinOnCondition()
    {
        // Arrange — CASE expression in JOIN ON clause
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name
FROM #A.Entities() a
INNER JOIN #B.Entities() b ON 
    CASE WHEN a.Population > 50 THEN a.Name ELSE a.City END = b.Name";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — CASE in ON condition may or may not be supported
        // Assert  CASE expression inside JOIN ON condition`n        AssertNoErrors(result);
    }

    #endregion
}
