#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 1: SQL Dialect Confusion.
///     Tests queries where users from other SQL dialects (PostgreSQL, MySQL, SQL Server, SQLite)
///     try syntax that Musoq doesn't support. The error messages should suggest
///     Musoq-specific alternatives.
///     Covers: P-LIMIT, P-AGG, P-JOIN, P-SUB, P-SET, P-WIN, P-MISC categories.
/// </summary>
[TestClass]
public class ErrorQuality_Phase1_DialectConfusionTests : BasicEntityTestBase
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
    // P-LIMIT: LIMIT / OFFSET instead of TAKE / SKIP
    // People coming from PostgreSQL, MySQL, SQL Server will try these reflexively.
    // Expected hint: Suggest TAKE and SKIP keywords with correct syntax.
    // ============================================================================

    #region P-LIMIT: LIMIT / OFFSET instead of TAKE / SKIP

    [TestMethod]
    public void P_LIMIT_01_LimitInsteadOfTake()
    {
        // Arrange — LIMIT instead of TAKE (MySQL/PostgreSQL style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() LIMIT 5";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should produce a parser error, ideally suggesting TAKE
        AssertHasOneOfErrorCodes(result, "LIMIT should suggest TAKE",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_LIMIT_02_OffsetInsteadOfSkip()
    {
        // Arrange — OFFSET instead of SKIP (MySQL/PostgreSQL style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() OFFSET 3";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should produce a parser error, ideally suggesting SKIP
        AssertHasOneOfErrorCodes(result, "OFFSET should suggest SKIP",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_LIMIT_03_LimitWithOffset_MySqlPostgresStyle()
    {
        // Arrange — LIMIT 5 OFFSET 3 (MySQL/PostgreSQL style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() LIMIT 5 OFFSET 3";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should produce a parser error, ideally suggesting TAKE/SKIP
        AssertHasOneOfErrorCodes(result, "LIMIT/OFFSET should suggest TAKE/SKIP",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_LIMIT_04_OffsetFetch_SqlServerStyle()
    {
        // Arrange — OFFSET..FETCH (SQL Server style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name OFFSET 3 ROWS FETCH NEXT 5 ROWS ONLY";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should produce a parser error
        AssertHasOneOfErrorCodes(result, "OFFSET..FETCH should suggest TAKE/SKIP",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_LIMIT_05_Top_SqlServerStyle()
    {
        // Arrange — TOP (SQL Server style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT TOP 5 Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, TOP is not a keyword. The parser treats it as an identifier,
        // so 'SELECT TOP 5 Name' parses as 'SELECT (TOP + 5) AS Name'. This is valid syntax
        // even though it's semantically different from SQL Server's TOP N.
        // Users should use TAKE instead: SELECT Name FROM #A.Entities() TAKE 5
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_LIMIT_06_First_FirebirdStyle()
    {
        // Arrange — FIRST (Firebird style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT FIRST 5 Name FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, FIRST is not a keyword. The parser treats it as an identifier,
        // so 'SELECT FIRST 5 Name' parses as 'SELECT (FIRST + 5) AS Name'. This is valid syntax
        // even though it's semantically different from Firebird's FIRST N.
        // Users should use TAKE instead: SELECT Name FROM #A.Entities() TAKE 5
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_LIMIT_07_Rownum_OracleStyle()
    {
        // Arrange — ROWNUM (Oracle style)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE ROWNUM <= 5";

        // Act — This will parse ROWNUM as a column name, so it goes to semantic analysis
        var result = analyzer.Analyze(query);

        // Assert — Should error at semantic level (unknown column ROWNUM)
        AssertHasOneOfErrorCodes(result, "ROWNUM is not a Musoq concept",
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    // ============================================================================
    // P-AGG: COUNT(*) and aggregate syntax
    // Expected hint: COUNT(*) → suggest Count(1). COUNT(DISTINCT x) → suggest GROUP BY + Count.
    // ============================================================================

    #region P-AGG: COUNT(*) and aggregate syntax

    [TestMethod]
    public void P_AGG_01_CountStar()
    {
        // Arrange — COUNT(*) syntax from standard SQL
        var analyzer = CreateAnalyzer();
        var query = "SELECT COUNT(*) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — COUNT(*) parses but cannot resolve the SetCount aggregation method.
        // In Musoq, use Count(1) instead of COUNT(*).
        AssertHasOneOfErrorCodes(result, "COUNT(*) should suggest Count(1)",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_AGG_02_CountStarWithAlias()
    {
        // Arrange — COUNT(*) AS Total syntax from standard SQL
        var analyzer = CreateAnalyzer();
        var query = "SELECT COUNT(*) AS Total FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — COUNT(*) parses but cannot resolve the SetCount aggregation method.
        // In Musoq, use Count(1) AS Total instead.
        AssertHasOneOfErrorCodes(result, "COUNT(*) should suggest Count(1)",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_AGG_03_CountStarLowercase()
    {
        // Arrange — count(*) lowercase syntax from standard SQL
        var analyzer = CreateAnalyzer();
        var query = "SELECT count(*) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — count(*) parses but cannot resolve the SetCount aggregation method.
        // In Musoq, use Count(1) instead.
        AssertHasOneOfErrorCodes(result, "count(*) should suggest Count(1)",
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_AGG_04_CountDistinct()
    {
        // Arrange — COUNT(DISTINCT column) is now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT COUNT(DISTINCT Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should succeed now that COUNT(DISTINCT) is supported
        Assert.IsFalse(result.HasErrors, "COUNT(DISTINCT) should now be supported");
        Assert.IsTrue(result.IsParsed, "Query should be parsed successfully");
    }

    [TestMethod]
    public void P_AGG_05_SumDistinct()
    {
        // Arrange — SUM(DISTINCT Value) is now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT SUM(DISTINCT Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should succeed now that SUM(DISTINCT) is supported
        Assert.IsFalse(result.HasErrors, "SUM(DISTINCT) should now be supported");
        Assert.IsTrue(result.IsParsed, "Query should be parsed successfully");
    }

    [TestMethod]
    public void P_AGG_06_AvgDistinct()
    {
        // Arrange — AVG(DISTINCT Value) is now supported
        var analyzer = CreateAnalyzer();
        var query = "SELECT AVG(DISTINCT Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should succeed now that AVG(DISTINCT) is supported
        Assert.IsFalse(result.HasErrors, "AVG(DISTINCT) should now be supported");
        Assert.IsTrue(result.IsParsed, "Query should be parsed successfully");
    }

    #endregion

    // ============================================================================
    // P-JOIN: JOIN syntax variations
    // Expected hints: CROSS JOIN → CROSS APPLY. FULL OUTER JOIN → not supported.
    // JOIN alone → INNER JOIN. LEFT JOIN → LEFT OUTER JOIN. USING → ON.
    // ============================================================================

    #region P-JOIN: JOIN syntax variations

    [TestMethod]
    public void P_JOIN_01_CrossJoin_NotSupported()
    {
        // Arrange — CROSS JOIN (should suggest CROSS APPLY)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name 
FROM #A.Entities() a 
CROSS JOIN #B.Entities() b";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error, ideally suggesting CROSS APPLY
        AssertHasOneOfErrorCodes(result, "CROSS JOIN should suggest CROSS APPLY",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2007_InvalidJoinCondition);
    }

    [TestMethod]
    public void P_JOIN_02_NaturalJoin()
    {
        // Arrange — NATURAL JOIN is not supported
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name FROM #A.Entities() a NATURAL JOIN #B.Entities() b";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error
        AssertHasOneOfErrorCodes(result, "NATURAL JOIN not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_JOIN_03_FullOuterJoin()
    {
        // Arrange — FULL OUTER JOIN is not supported
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name 
FROM #A.Entities() a 
FULL OUTER JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error, explain FULL OUTER JOIN is not supported
        AssertHasOneOfErrorCodes(result, "FULL OUTER JOIN not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_JOIN_04_FullJoin_Shorthand()
    {
        // Arrange — FULL JOIN shorthand
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name 
FROM #A.Entities() a 
FULL JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error
        AssertHasOneOfErrorCodes(result, "FULL JOIN not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_JOIN_05_JoinWithoutInnerKeyword()
    {
        // Arrange — JOIN without INNER keyword is valid in Musoq (= INNER JOIN)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name 
FROM #A.Entities() a 
JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — bare JOIN is accepted as INNER JOIN in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_JOIN_06_LeftJoinWithoutOuterKeyword()
    {
        // Arrange — LEFT JOIN without OUTER is valid in Musoq (= LEFT OUTER JOIN)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name 
FROM #A.Entities() a 
LEFT JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — LEFT JOIN is accepted as LEFT OUTER JOIN in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_JOIN_07_RightJoinWithoutOuterKeyword()
    {
        // Arrange — RIGHT JOIN without OUTER is valid in Musoq (= RIGHT OUTER JOIN)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name 
FROM #A.Entities() a 
RIGHT JOIN #B.Entities() b ON a.Name = b.Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — RIGHT JOIN is accepted as RIGHT OUTER JOIN in Musoq
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_JOIN_08_UsingClauseInsteadOfOn()
    {
        // Arrange — USING clause instead of ON
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name 
FROM #A.Entities() a 
INNER JOIN #B.Entities() b USING (Name)";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should suggest ON clause instead
        AssertHasOneOfErrorCodes(result, "USING should suggest ON",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    // ============================================================================
    // P-SUB: Subquery attempts
    // Subqueries are not supported; suggest CTE with appropriate restructuring pattern.
    // ============================================================================

    #region P-SUB: Subquery attempts

    [TestMethod]
    public void P_SUB_01_SubqueryInWhere_In()
    {
        // Arrange — Subquery in WHERE with IN
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
WHERE Name IN (SELECT Name FROM #B.Entities())";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain subqueries aren't supported, suggest CTE
        AssertHasOneOfErrorCodes(result, "subquery in WHERE IN should suggest CTE",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SUB_02_SubqueryInSelect()
    {
        // Arrange — Subquery in SELECT (scalar subquery)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name, (SELECT Count(1) FROM #B.Entities()) AS Total
FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain subqueries aren't supported, suggest CTE
        AssertHasOneOfErrorCodes(result, "scalar subquery in SELECT should suggest CTE",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SUB_03_SubqueryInFrom()
    {
        // Arrange — Subquery in FROM (derived table)
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM (SELECT Name FROM #A.Entities()) sub";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain subqueries aren't supported, suggest CTE
        AssertHasOneOfErrorCodes(result, "derived table should suggest CTE",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SUB_04_ExistsSubquery()
    {
        // Arrange — EXISTS subquery
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities() a
WHERE EXISTS (SELECT 1 FROM #B.Entities() b WHERE b.Name = a.Name)";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain subqueries aren't supported, suggest CTE
        AssertHasOneOfErrorCodes(result, "EXISTS subquery should suggest CTE + JOIN",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SUB_05_NotExistsSubquery()
    {
        // Arrange — NOT EXISTS subquery
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities() a
WHERE NOT EXISTS (SELECT 1 FROM #B.Entities() b WHERE b.Name = a.Name)";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain subqueries aren't supported
        AssertHasOneOfErrorCodes(result, "NOT EXISTS subquery should suggest CTE + LEFT JOIN",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SUB_06_ScalarSubqueryComparison()
    {
        // Arrange — Scalar subquery in comparison
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
WHERE Population > (SELECT Population FROM #B.Entities())";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain subqueries aren't supported
        AssertHasOneOfErrorCodes(result, "scalar subquery comparison should suggest CTE",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SUB_07_AnySubquery()
    {
        // Arrange — ANY subquery
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
WHERE Population > ANY (SELECT Population FROM #B.Entities())";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain subqueries aren't supported
        AssertHasOneOfErrorCodes(result, "ANY subquery not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SUB_08_AllSubquery()
    {
        // Arrange — ALL subquery
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
WHERE Population > ALL (SELECT Population FROM #B.Entities())";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain subqueries aren't supported
        AssertHasOneOfErrorCodes(result, "ALL subquery not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2024_InvalidSubquery,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    // ============================================================================
    // P-SET: Set operations without column specification
    // Musoq requires explicit column list: UNION ALL (ColumnName) SELECT ...
    // ============================================================================

    #region P-SET: Set operations without column specification

    [TestMethod]
    public void P_SET_01_UnionAllWithoutColumnList()
    {
        // Arrange — UNION ALL without explicit column list (standard SQL style)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
UNION ALL
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest explicit column list: UNION ALL (Name)
        AssertHasOneOfErrorCodes(result, "UNION ALL without column list should explain Musoq syntax",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3031_SetOperatorMissingKeys,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SET_02_UnionWithoutAllAndColumnList()
    {
        // Arrange — UNION (no ALL) without column list
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
UNION
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest UNION ALL (ColumnName) syntax
        AssertHasOneOfErrorCodes(result, "UNION without ALL and column list",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3031_SetOperatorMissingKeys,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SET_03_ExceptWithoutColumnList()
    {
        // Arrange — EXCEPT without column list
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
EXCEPT
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest EXCEPT (ColumnName) syntax
        AssertHasOneOfErrorCodes(result, "EXCEPT without column list",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3031_SetOperatorMissingKeys,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_SET_04_IntersectWithoutColumnList()
    {
        // Arrange — INTERSECT without column list
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name FROM #A.Entities()
INTERSECT
SELECT Name FROM #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest INTERSECT (ColumnName) syntax
        AssertHasOneOfErrorCodes(result, "INTERSECT without column list",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3031_SetOperatorMissingKeys,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    // ============================================================================
    // P-WIN: Window functions (not supported)
    // Expected hint: Window functions (OVER clause) are not supported.
    // Suggest GROUP BY for aggregation or CTE-based alternatives.
    // ============================================================================

    #region P-WIN: Window functions (not supported)

    [TestMethod]
    public void P_WIN_01_RowNumber()
    {
        // Arrange — ROW_NUMBER() OVER (ORDER BY ...)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, ROW_NUMBER() OVER (ORDER BY Name) AS RowNum FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain OVER clause / window functions are not supported
        AssertHasOneOfErrorCodes(result, "ROW_NUMBER() OVER not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_WIN_02_Rank()
    {
        // Arrange — RANK() OVER (ORDER BY ...)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, RANK() OVER (ORDER BY Name) AS Rank FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain window functions are not supported
        AssertHasOneOfErrorCodes(result, "RANK() OVER not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_WIN_03_Lag()
    {
        // Arrange — LAG() OVER (ORDER BY ...)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, LAG(Name, 1) OVER (ORDER BY Name) AS PrevName FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain window functions are not supported
        AssertHasOneOfErrorCodes(result, "LAG() OVER not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_WIN_04_Lead()
    {
        // Arrange — LEAD() OVER (ORDER BY ...)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, LEAD(Name, 1) OVER (ORDER BY Name) AS NextName FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain window functions are not supported
        AssertHasOneOfErrorCodes(result, "LEAD() OVER not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_WIN_05_SumOver_RunningTotal()
    {
        // Arrange — SUM() OVER (ORDER BY ...) for running total
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Sum(Population) OVER (ORDER BY Name) AS RunningTotal FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain window functions are not supported
        AssertHasOneOfErrorCodes(result, "SUM() OVER not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_WIN_06_PartitionBy()
    {
        // Arrange — COUNT() OVER (PARTITION BY ...)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(1) OVER (PARTITION BY City) AS GroupCount FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain window functions are not supported
        AssertHasOneOfErrorCodes(result, "PARTITION BY not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_WIN_07_Ntile()
    {
        // Arrange — NTILE() OVER (ORDER BY ...)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, NTILE(4) OVER (ORDER BY Name) AS Quartile FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain window functions are not supported
        AssertHasOneOfErrorCodes(result, "NTILE() OVER not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_WIN_08_DenseRank()
    {
        // Arrange — DENSE_RANK() OVER (ORDER BY ...)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, DENSE_RANK() OVER (ORDER BY Name) AS DenseRank FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain window functions are not supported
        AssertHasOneOfErrorCodes(result, "DENSE_RANK() OVER not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion

    // ============================================================================
    // P-MISC: Miscellaneous SQL dialect confusion
    // Tests for operators, casting, and functions from other dialects.
    // ============================================================================

    #region P-MISC: Miscellaneous SQL dialect confusion

    [TestMethod]
    public void P_MISC_01_NotEquals_ExclamationEquals()
    {
        // Arrange — != instead of <> (many dialects)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name != 'Warsaw'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Musoq may or may not support !=. If not, should suggest <>
        // Document behavior:
        // Assert  != operator: either supported or should suggest <>`n        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_MISC_02_DoubleEquals()
    {
        // Arrange — == instead of = (C#/JS habit)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name == 'Warsaw'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error and suggest = instead of ==
        AssertHasOneOfErrorCodes(result, "== should suggest =",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_MISC_03_PipePipeConcatenation()
    {
        // Arrange — || for concatenation (PostgreSQL/SQLite)
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'hello' || ' ' || 'world' FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest Concat() function
        AssertHasOneOfErrorCodes(result, "|| should suggest Concat()",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_MISC_04_ILike_PostgresStyle()
    {
        // Arrange — ILIKE (PostgreSQL case-insensitive LIKE)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name ILIKE '%war%'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error and suggest LIKE with appropriate workaround
        AssertHasOneOfErrorCodes(result, "ILIKE not supported, suggest LIKE",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_MISC_05_DoubleColonCasting_PostgresStyle()
    {
        // Arrange — :: for casting (PostgreSQL)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population::text FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should error and suggest ToString() or appropriate Cast function
        AssertHasOneOfErrorCodes(result, ":: casting should suggest ToString() etc.",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_MISC_06_CastExpression()
    {
        // Arrange — CAST(x AS type) is standard SQL
        var analyzer = CreateAnalyzer();
        var query = "SELECT CAST(Population AS varchar) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest ToInt32()/ToString()/etc.
        AssertHasOneOfErrorCodes(result, "CAST should suggest Musoq conversion functions",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_MISC_07_ConvertFunction_SqlServerStyle()
    {
        // Arrange — CONVERT function (SQL Server)
        var analyzer = CreateAnalyzer();
        var query = "SELECT CONVERT(varchar, Population) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest Musoq conversion functions.
        // CONVERT(varchar, Population) parses as a function call CONVERT with args,
        // but 'varchar' is treated as a column reference (unknown) and
        // the method CONVERT cannot be resolved.
        AssertHasOneOfErrorCodes(result, "CONVERT should suggest Musoq conversion functions",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ3001_UnknownColumn,
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_MISC_08_Coalesce()
    {
        // Arrange — COALESCE (standard SQL, test if supported)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Coalesce(null, Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document behavior: Musoq may support Coalesce as a built-in function
        // Assert  Coalesce: may be supported as built-in function`n        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_MISC_09_IfNull_MySqlSqliteStyle()
    {
        // Arrange — IFNULL (MySQL/SQLite)
        var analyzer = CreateAnalyzer();
        var query = "SELECT IFNULL(null, Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest Coalesce or Musoq equivalent
        AssertHasOneOfErrorCodes(result, "IFNULL should suggest Coalesce or Musoq equivalent",
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_MISC_10_Nvl_OracleStyle()
    {
        // Arrange — NVL (Oracle)
        var analyzer = CreateAnalyzer();
        var query = "SELECT NVL(null, Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest Coalesce or Musoq equivalent
        AssertHasOneOfErrorCodes(result, "NVL should suggest Coalesce or Musoq equivalent",
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_MISC_11_IsNull_SqlServerStyle()
    {
        // Arrange — ISNULL function (SQL Server)
        var analyzer = CreateAnalyzer();
        var query = "SELECT ISNULL(null, Name) FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest Coalesce or Musoq equivalent
        AssertHasOneOfErrorCodes(result, "ISNULL should suggest Coalesce or Musoq equivalent",
            DiagnosticCode.MQ3004_UnknownFunction,
            DiagnosticCode.MQ3013_CannotResolveMethod,
            DiagnosticCode.MQ3029_UnresolvableMethod,
            DiagnosticCode.MQ9999_Unknown);
    }

    [TestMethod]
    public void P_MISC_12_BetweenOperator()
    {
        // Arrange — BETWEEN operator (test if supported)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Population BETWEEN 50 AND 150";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document: Musoq may support BETWEEN, if not suggest >= AND <=
        // Assert  BETWEEN: may be supported; if not, suggest >= AND <=`n        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_MISC_13_DoubleQuotedIdentifiers()
    {
        // Arrange — Double-quoted identifiers (ANSI/PostgreSQL)
        var analyzer = CreateAnalyzer();
        var query = "SELECT \"Name\" FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document: Musoq may or may not support double-quoted identifiers
        // Assert  Double-quoted identifiers: may parse as string or identifier`n        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_MISC_14_BacktickIdentifiers_MySqlStyle()
    {
        // Arrange — Backtick identifiers (MySQL)
        var analyzer = CreateAnalyzer();
        var query = "SELECT `Name` FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error if backticks aren't supported
        // Assert  Backtick identifiers: either supported or should error`n        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_MISC_15_StringAlias()
    {
        // Arrange — AS with string alias (some dialects allow this)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name AS 'MyColumn' FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document behavior
        // Assert  String alias: may be supported or should use identifier`n        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_MISC_16_StringConcatWithPlus_TypeMismatch()
    {
        // Arrange — String concatenation with + on numbers without casting
        var analyzer = CreateAnalyzer();
        var query = "SELECT 'Value is: ' + Population FROM #A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should suggest ToString() or Concat()
        AssertHasOneOfErrorCodes(result, "string + number should suggest ToString/Concat",
            DiagnosticCode.MQ3005_TypeMismatch,
            DiagnosticCode.MQ3007_InvalidOperandTypes,
            DiagnosticCode.MQ9999_Unknown);
    }

    #endregion
}
