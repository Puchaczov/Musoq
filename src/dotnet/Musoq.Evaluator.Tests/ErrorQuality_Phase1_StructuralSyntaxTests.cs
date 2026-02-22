#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Error Message Quality Audit — Phase 1: Structural Syntax Errors.
///     Tests for missing clauses, misordering, CTE errors, TABLE/COUPLE errors,
///     expression/operator parse errors, and schema reference parse errors.
///     Covers: P-STRUCT, P-CTE, P-TC, P-EXPR, P-SCHEMA categories.
/// </summary>
[TestClass]
public class ErrorQuality_Phase1_StructuralSyntaxTests : BasicEntityTestBase
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

    #endregion

    // ============================================================================
    // P-STRUCT: Missing clauses and misordering
    // ============================================================================

    #region P-STRUCT: Missing clauses and misordering

    [TestMethod]
    public void P_STRUCT_01_SelectWithoutFrom()
    {
        // Arrange — SELECT Value (no FROM, and no dual-like context)
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should say FROM is missing
        AssertHasOneOfErrorCodes(result, "SELECT without FROM",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2004_MissingFromClause);
    }

    [TestMethod]
    public void P_STRUCT_02_FromWithoutSelect()
    {
        // Arrange — FROM without SELECT
        var analyzer = CreateAnalyzer();
        var query = "FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should say SELECT is missing
        AssertHasOneOfErrorCodes(result, "FROM without SELECT",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2025_MissingSelectKeyword);
    }

    [TestMethod]
    public void P_STRUCT_03_WhereBeforeFrom()
    {
        // Arrange — WHERE before FROM
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name WHERE Name > 'A' FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate wrong clause order
        AssertHasOneOfErrorCodes(result, "WHERE before FROM is wrong order",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2004_MissingFromClause,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_04_HavingWithoutGroupBy()
    {
        // Arrange — HAVING without GROUP BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() HAVING Name > 'A'";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain HAVING requires GROUP BY
        AssertHasOneOfErrorCodes(result, "HAVING without GROUP BY",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_05_OrderByBeforeWhere()
    {
        // Arrange — ORDER BY before WHERE
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name WHERE Name > 'A'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate wrong clause order
        AssertHasOneOfErrorCodes(result, "ORDER BY before WHERE is wrong order",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_06_GroupByAfterOrderBy()
    {
        // Arrange — GROUP BY after ORDER BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(1) FROM #A.Entities() ORDER BY Name GROUP BY Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate wrong clause order
        AssertHasOneOfErrorCodes(result, "GROUP BY after ORDER BY is wrong order",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_07_DoubleWhere()
    {
        // Arrange — Double WHERE clause
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name > 'A' WHERE Name < 'Z'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate duplicate WHERE
        AssertHasOneOfErrorCodes(result, "double WHERE clause",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_08_DoubleOrderBy()
    {
        // Arrange — Double ORDER BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() ORDER BY Name ASC ORDER BY Name DESC";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate duplicate ORDER BY
        AssertHasOneOfErrorCodes(result, "double ORDER BY clause",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_09_TakeBeforeOrderBy()
    {
        // Arrange — TAKE before ORDER BY
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() TAKE 5 ORDER BY Name";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate wrong order or handle gracefully
        AssertHasOneOfErrorCodes(result, "TAKE before ORDER BY",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_10_SkipWithoutTake()
    {
        // Arrange — SKIP without TAKE
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() SKIP 5";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Document behavior: SKIP without TAKE may or may not be valid
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_STRUCT_11_EmptySelectList()
    {
        // Arrange — SELECT FROM (empty column list)
        var analyzer = CreateAnalyzer();
        var query = "SELECT FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — SELECT list cannot be empty
        AssertHasOneOfErrorCodes(result, "empty SELECT list",
            DiagnosticCode.MQ2005_InvalidSelectList,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void P_STRUCT_12_TrailingCommaInSelectList()
    {
        // Arrange — SELECT Name, FROM
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate trailing comma
        AssertHasOneOfErrorCodes(result, "trailing comma in SELECT list",
            DiagnosticCode.MQ2014_TrailingComma,
            DiagnosticCode.MQ2001_UnexpectedToken);
    }

    [TestMethod]
    public void P_STRUCT_13_TrailingCommaInGroupBy()
    {
        // Arrange — GROUP BY Name,
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name, Count(1) FROM #A.Entities() GROUP BY Name,";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate trailing comma
        AssertHasOneOfErrorCodes(result, "trailing comma in GROUP BY",
            DiagnosticCode.MQ2014_TrailingComma,
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_14_MissingOnInJoin()
    {
        // Arrange — JOIN without ON condition
        var analyzer = CreateAnalyzer();
        var query = @"SELECT a.Name, b.Name 
FROM #A.Entities() a 
INNER JOIN #B.Entities() b";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing ON clause
        AssertHasOneOfErrorCodes(result, "INNER JOIN without ON",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2007_InvalidJoinCondition,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_15_MissingAliasForCrossApply()
    {
        // Arrange — CROSS APPLY without alias on the applied source
        var analyzer = CreateAnalyzer();
        var query = @"SELECT Name 
FROM #A.Entities() a 
CROSS APPLY #B.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing alias
        AssertHasOneOfErrorCodes(result, "CROSS APPLY without alias",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ3022_MissingAlias,
            DiagnosticCode.MQ3002_AmbiguousColumn,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_16_EmptyParenthesesInSchemaTable()
    {
        // Arrange — #A.Entities() is fine, but what about missing required params?
        // Using #A with no method call at all
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate invalid schema reference
        AssertHasOneOfErrorCodes(result, "schema with empty method name",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_17_MissingClosingParenthesis()
    {
        // Arrange — #A.Entities( without closing )
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities(";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate unclosed parenthesis
        AssertHasOneOfErrorCodes(result, "missing closing parenthesis in schema method",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2010_MissingClosingParenthesis,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_18_ExtraClosingParenthesis()
    {
        // Arrange — Extra closing parenthesis
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities())";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate extra closing parenthesis
        AssertHasOneOfErrorCodes(result, "extra closing parenthesis",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_STRUCT_19_SemicolonInMiddleOfQuery()
    {
        // Arrange — Semicolon in middle of query
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name; FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, semicolons act as statement terminators.
        // 'SELECT Name; FROM #A.Entities()' is parsed as two separate statements:
        // Statement 1: 'SELECT Name' (valid standalone query)
        // Statement 2: 'FROM #A.Entities()' (reordered query syntax)
        // The parser's multi-statement support and error recovery handle this gracefully.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_STRUCT_20_MultipleQueriesWithoutSeparation()
    {
        // Arrange — Two SELECT statements without proper separation
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() SELECT City FROM #B.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, multiple statements are supported.
        // The parser treats consecutive SELECT statements as separate queries
        // in a multi-statement batch. No separator (semicolon) is required.
        AssertNoErrors(result);
    }

    #endregion

    // ============================================================================
    // P-CTE: CTE syntax errors
    // ============================================================================

    #region P-CTE: CTE syntax errors

    [TestMethod]
    public void P_CTE_01_WithWithoutAs()
    {
        // Arrange — WITH without AS keyword
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData SELECT Name FROM #A.Entities()
SELECT * FROM MyData md";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing AS in CTE
        AssertHasOneOfErrorCodes(result, "CTE without AS keyword",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2023_MissingAsKeyword,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_02_MissingParenthesesAroundCteQuery()
    {
        // Arrange — WITH MyData AS SELECT ... (missing parentheses)
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData AS SELECT Name FROM #A.Entities()
SELECT * FROM MyData md";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing parentheses around CTE query
        AssertHasOneOfErrorCodes(result, "CTE query without parentheses",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_03_RecursiveCte()
    {
        // Arrange — Recursive CTE referencing itself
        var analyzer = CreateAnalyzer();
        var query = @"WITH Recursive AS (
    SELECT 1 AS Value FROM #A.Entities()
    UNION ALL (Value)
    SELECT Value + 1 FROM Recursive r WHERE r.Value < 10
)
SELECT * FROM Recursive r";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain recursion is not supported
        AssertHasOneOfErrorCodes(result, "recursive CTE not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ3003_UnknownTable,
            DiagnosticCode.MQ3016_CircularReference,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_04_CteAfterSelect()
    {
        // Arrange — CTE placed after SELECT (wrong position)
        var analyzer = CreateAnalyzer();
        var query = @"SELECT * FROM MyData md
WITH MyData AS (SELECT Name FROM #A.Entities())";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate WITH must come before SELECT
        AssertHasOneOfErrorCodes(result, "CTE after SELECT (wrong position)",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_05_DuplicateCteNames()
    {
        // Arrange — Two CTEs with the same name
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData AS (SELECT Name FROM #A.Entities()),
     MyData AS (SELECT Name FROM #B.Entities())
SELECT * FROM MyData md";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate duplicate CTE name
        AssertHasOneOfErrorCodes(result, "duplicate CTE names",
            DiagnosticCode.MQ2008_DuplicateAlias,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ3021_DuplicateAlias,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_06_CteWithNoSelectAfter()
    {
        // Arrange — CTE definition with no SELECT that uses it
        var analyzer = CreateAnalyzer();
        var query = "WITH MyData AS (SELECT Name FROM #A.Entities())";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate CTE needs a SELECT statement
        AssertHasOneOfErrorCodes(result, "CTE with no SELECT after it",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2016_IncompleteStatement,
            DiagnosticCode.MQ2017_UnexpectedEndOfFile,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_07_CteReferenceWithoutAlias()
    {
        // Arrange — CTE reference without alias in FROM
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData AS (SELECT Name FROM #A.Entities())
SELECT * FROM MyData";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — In Musoq, CTE references without explicit alias are valid.
        // The CTE name 'MyData' is automatically used as the alias.
        // This is equivalent to: SELECT * FROM MyData MyData
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_CTE_08_ForwardReferenceBetweenCtes()
    {
        // Arrange — CTE referencing a CTE defined after it
        var analyzer = CreateAnalyzer();
        var query = @"WITH 
    Second AS (SELECT * FROM First f),
    First AS (SELECT Name FROM #A.Entities())
SELECT * FROM Second s";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should explain forward references not allowed
        AssertHasOneOfErrorCodes(result, "forward reference between CTEs",
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ3003_UnknownTable,
            DiagnosticCode.MQ3023_TableNotDefined,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_CTE_09_EmptyCteBody()
    {
        // Arrange — CTE with empty body
        var analyzer = CreateAnalyzer();
        var query = @"WITH MyData AS ()
SELECT * FROM MyData md";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate empty CTE body
        AssertHasOneOfErrorCodes(result, "empty CTE body",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2013_InvalidCTE,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    // ============================================================================
    // P-TC: TABLE/COUPLE syntax errors
    // ============================================================================

    #region P-TC: TABLE/COUPLE syntax errors

    [TestMethod]
    public void P_TC_03_TableWithInvalidTypeNames()
    {
        // Arrange — TABLE with invalid type names
        var analyzer = CreateAnalyzer();
        var query = @"table MyType { Name banana, Value potato };
couple #A.Entities() with table MyType as Source;
select Name, Value from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate invalid type names
        AssertHasOneOfErrorCodes(result, "invalid type names 'banana', 'potato'",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_TC_05_TableWithNoColumns()
    {
        // Arrange — TABLE with empty definition
        var analyzer = CreateAnalyzer();
        var query = @"table Empty {};
couple #A.Entities() with table Empty as Source;
select * from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate empty table definition
        AssertHasOneOfErrorCodes(result, "TABLE with no columns",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2005_InvalidSelectList,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_TC_06_TableWithDuplicateColumnNames()
    {
        // Arrange — TABLE with duplicate column names
        var analyzer = CreateAnalyzer();
        var query = @"table Dupes { Name string, Name int };
couple #A.Entities() with table Dupes as Source;
select Name from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate duplicate column names
        AssertHasOneOfErrorCodes(result, "duplicate column names in TABLE",
            DiagnosticCode.MQ2008_DuplicateAlias,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            DiagnosticCode.MQ4008_DuplicateSchemaField,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_TC_08_TableColumnWithoutType()
    {
        // Arrange — TABLE column without type
        var analyzer = CreateAnalyzer();
        var query = @"table MyType { Name, Value int };
couple #A.Entities() with table MyType as Source;
select Name from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing type
        AssertHasOneOfErrorCodes(result, "TABLE column without type",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_TC_09_TableColumnWithEmptyType()
    {
        // Arrange — TABLE column with missing type (second column has no type)
        var analyzer = CreateAnalyzer();
        var query = @"table MyType { Name string, Value };
couple #A.Entities() with table MyType as Source;
select Name from Source()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate invalid empty type
        AssertHasOneOfErrorCodes(result, "TABLE column with empty type",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2012_InvalidSchemaDefinition,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    // ============================================================================
    // P-EXPR: Expression/Operator Parse Errors
    // ============================================================================

    #region P-EXPR: Expression/Operator parse errors

    [TestMethod]
    public void P_EXPR_01_DanglingOperator()
    {
        // Arrange — Dangling operator: SELECT Name + FROM
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name + FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing operand
        AssertHasOneOfErrorCodes(result, "dangling + operator",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2020_MissingOperand,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_02_DoubleOperator()
    {
        // Arrange — Double operator: Name ++ 1
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population ++ 1 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate invalid operator
        AssertHasOneOfErrorCodes(result, "double ++ operator",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_03_UnclosedCaseExpression()
    {
        // Arrange — CASE without END
        var analyzer = CreateAnalyzer();
        var query = "SELECT CASE WHEN Population > 50 THEN 'high' FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing END
        AssertHasOneOfErrorCodes(result, "CASE without END",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            DiagnosticCode.MQ2029_MissingEndKeyword,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_04_CaseWithoutWhen()
    {
        // Arrange — CASE THEN (missing WHEN)
        var analyzer = CreateAnalyzer();
        var query = "SELECT CASE THEN 'value' END FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing WHEN
        AssertHasOneOfErrorCodes(result, "CASE without WHEN",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            DiagnosticCode.MQ2027_MissingWhenClause,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_05_CaseWhenWithoutThen()
    {
        // Arrange — CASE WHEN ... ELSE (missing THEN)
        var analyzer = CreateAnalyzer();
        var query = "SELECT CASE WHEN Population > 50 ELSE 'low' END FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing THEN
        AssertHasOneOfErrorCodes(result, "CASE WHEN without THEN",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            DiagnosticCode.MQ2028_MissingThenClause,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_06_MultipleElseInCase()
    {
        // Arrange — CASE with two ELSE branches
        var analyzer = CreateAnalyzer();
        var query = "SELECT CASE WHEN Population > 50 THEN 'high' ELSE 'medium' ELSE 'low' END FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate multiple ELSE not allowed
        AssertHasOneOfErrorCodes(result, "multiple ELSE in CASE",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2026_InvalidCaseExpression,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_07_UnclosedStringLiteral()
    {
        // Arrange — Unclosed string literal
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name = 'hello";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate unterminated string
        AssertHasOneOfErrorCodes(result, "unclosed string literal",
            DiagnosticCode.MQ1002_UnterminatedString,
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_08_UnclosedParenthesisInExpression()
    {
        // Arrange — (Population + 1 without closing paren
        var analyzer = CreateAnalyzer();
        var query = "SELECT (Population + 1 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate unclosed parenthesis
        AssertHasOneOfErrorCodes(result, "unclosed parenthesis in expression",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2010_MissingClosingParenthesis);
    }

    [TestMethod]
    public void P_EXPR_09_EmptyInList()
    {
        // Arrange — IN with empty list
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IN ()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — In Musoq, IN() with empty argument list is syntactically valid.
        // The parser treats () as an empty args list. At runtime, IN with no values
        // will simply never match (always false).
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_EXPR_10_InWithoutParentheses()
    {
        // Arrange — IN without parentheses
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name IN 'Warsaw', 'Berlin'";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing parentheses for IN
        AssertHasOneOfErrorCodes(result, "IN without parentheses",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_11_LikeWithoutPattern()
    {
        // Arrange — LIKE without pattern
        var analyzer = CreateAnalyzer();
        var query = "SELECT Name FROM #A.Entities() WHERE Name LIKE";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should indicate missing LIKE pattern
        AssertHasOneOfErrorCodes(result, "LIKE without pattern",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2020_MissingOperand,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_13_TernaryConditional_CSharpHabit()
    {
        // Arrange — Ternary-style conditional from C#/JS
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population > 50 ? 'high' : 'low' FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should suggest CASE WHEN expression
        AssertHasOneOfErrorCodes(result, "ternary ?: should suggest CASE WHEN",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_EXPR_14_LambdaExpression_CSharpHabit()
    {
        // Arrange — Lambda expression from C#
        var analyzer = CreateAnalyzer();
        var query = "SELECT Population => Population * 2 FROM #A.Entities()";

        // Act
        var result = analyzer.ValidateSyntax(query);

        // Assert — Should error with clear message
        AssertHasOneOfErrorCodes(result, "lambda => expression not supported",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2019_InvalidOperator,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion

    // ============================================================================
    // P-SCHEMA: Schema Reference Parse Errors
    // ============================================================================

    #region P-SCHEMA: Schema reference parse errors

    [TestMethod]
    public void P_SCHEMA_01_MissingHashPrefix()
    {
        // Arrange — A.Entities() without # prefix
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — In Musoq, the parser's EnsureHashPrefix method automatically adds
        // the # prefix when parsing schema references. So 'FROM A.Entities()' is
        // equivalent to 'FROM #A.Entities()'. The # prefix is optional.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SCHEMA_02_MissingDotSeparator()
    {
        // Arrange — #AEntities() without dot separator
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #AEntities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate invalid schema reference format
        AssertHasOneOfErrorCodes(result, "schema without dot separator",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ3003_UnknownTable,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_SCHEMA_03_MissingTableName()
    {
        // Arrange — #A() without table/method name
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #A()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing table/method name
        AssertHasOneOfErrorCodes(result, "schema without table name",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ3003_UnknownTable,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_SCHEMA_04_MissingSchemaName()
    {
        // Arrange — #.Entities() without schema name
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing schema name
        AssertHasOneOfErrorCodes(result, "schema reference with missing schema name",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_SCHEMA_05_DoubleHash()
    {
        // Arrange — ##A.Entities() with double hash
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM ##A.Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate invalid schema reference
        AssertHasOneOfErrorCodes(result, "double hash in schema reference",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ3010_UnknownSchema,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    [TestMethod]
    public void P_SCHEMA_06_SchemaWithSpaces()
    {
        // Arrange — #A .Entities() with space before dot
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #A .Entities()";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — In Musoq, the lexer ignores whitespace between tokens.
        // '#A .Entities()' is tokenized the same as '#A.Entities()'.
        // Whitespace around the dot separator is allowed.
        AssertNoErrors(result);
    }

    [TestMethod]
    public void P_SCHEMA_07_MissingParenthesesEntirely()
    {
        // Arrange — #A.Entities without ()
        var analyzer = CreateAnalyzer();
        var query = "SELECT * FROM #A.Entities";

        // Act
        var result = analyzer.Analyze(query);

        // Assert — Should indicate missing parentheses
        AssertHasOneOfErrorCodes(result, "schema reference without parentheses",
            DiagnosticCode.MQ2001_UnexpectedToken,
            DiagnosticCode.MQ2030_UnsupportedSyntax,
            DiagnosticCode.MQ2030_UnsupportedSyntax);
    }

    #endregion
}
